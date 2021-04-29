using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Okex.Net.CoreObjects;
using Okex.Net.Helpers;
using Okex.Net.V5.Configs;
using Okex.Net.V5.Enums;
using Okex.Net.V5.Models;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace Okex.Net.V5.Clients
{
	public class OkexSocketClientPrivate
	{
		public OkexSocketClientPrivate(OkexCredential credential) : this(credential, new SocketClientConfig())
		{
		}

		public OkexSocketClientPrivate(OkexCredential credential, SocketClientConfig clientConfig)
		{
			_clientConfig = clientConfig;
			_credential = credential;

			InitProcessors();
			CreateSocket();
		}

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "Unnamed";
		public bool SocketConnected => _ws.State == WebSocketState.Open;
		public DateTime LastMessageDate { get; private set; } = DateTime.MinValue;

		internal event Action ConnectionBroken = () => { };
		internal event Action<OkexOrderDetails> OrderUpdate = order => { };
		internal event Action<ErrorMessage> ErrorReceived = error => { };

		private bool _onKilled;

		private readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly SocketClientConfig _clientConfig;

		private WebSocket _ws;
		private readonly OkexCredential _credential;
		private int _reconnectTime => _clientConfig.SocketReconnectionTimeMs;
		private readonly List<OkexChannel> _channels = new List<OkexChannel>();
		private readonly Dictionary<string, ChannelTypeEnum> _channelTypes = new Dictionary<string, ChannelTypeEnum>
		{
			{"orders", ChannelTypeEnum.Order},
		};

		private string BaseUrl => _clientConfig.IsTestNet ? _clientConfig.DemoUrlPrivate : _clientConfig.UrlPrivate;


		#region Connection

		public void Connect()
		{
			try
			{
				if (_onKilled)
				{
					return;
				}

				_logger.Trace($"Socket ({Name}) {Id} connecting... (state: {_ws.State})");
				_ws.Open();
				_logger.Trace($"Socket ({Name}) {Id} connected... (state: {_ws.State})");
			}
			catch (PlatformNotSupportedException)
			{
				ConnectionBroken();
			}
			catch (IOException)
			{
				ConnectionBroken();
			}
			catch (Exception e)
			{
				_logger.Trace($"Socket ({Name}) {Id}  connect failed {e.GetType().Name} (state: {_ws.State}): {e.Message}");
				throw;
			}
		}

		public void Disconnect()
		{
			if (_ws.State == WebSocketState.Closed || _ws.State == WebSocketState.Closing)
			{
				return;
			}

			_logger.Trace($"Socket ({Name}) {Id} disconnecting... (state: {_ws.State})");
			_ws.Close();
			_logger.Trace($"Socket ({Name}) {Id} disconnected... (state: {_ws.State})");
		}

		public void Reconnect()
		{
			var ws = _ws;
			_ws.Error -= SocketOnError;
			_ws.Opened -= OnSocketOpened;
			_ws.Closed -= OnSocketClosed;
			_ws.MessageReceived -= OnSocketGetMessage;

			CreateSocket();
			Connect();

			ws.Dispose();
		}

		public void Kill()
		{
			_logger.Trace("Socket killing...");

			_onKilled = true;
			TryDisconnect();
			_ws.Dispose();
		}

		private void OnSocketOpened(object sender, EventArgs e)
		{
			_logger.Trace($"Socket ({Name}) {Id} is open (state: {_ws.State}): resubscribing...");
			Auth();
			SendSubscribeToChannels();
		}

		private void Auth()
		{
			if (_credential is null)
			{
				return;
			}

			var time = (DateTime.UtcNow.ToUnixTimeMilliSeconds() / 1000.0m).ToString(CultureInfo.InvariantCulture);
			var signtext = time + "GET" + "/users/self/verify";
			var hmacEncryptor = new HMACSHA256(Encoding.ASCII.GetBytes(_credential.SecretKey));
			var signature = OkexAuthenticationProvider.Base64Encode(hmacEncryptor.ComputeHash(Encoding.UTF8.GetBytes(signtext)));
			var loginRequest = new OkexLoginRequest(_credential.ApiKey, _credential.Password, time, signature);
			var request = new OkexSocketRequest("login", loginRequest);

			Send(request);
		}

		private void OnSocketClosed(object sender, EventArgs e)
		{
			_logger.Trace($"Socket ({Name}) {Id} OnSocketClosed... (state: {_ws.State})");
			ReconnectingSocket();
		}

		private void ReconnectingSocket()
		{
			try
			{
				if (_reconnectTime > 0)
				{
					Thread.Sleep(_reconnectTime);
				}

				_logger.Trace($"Try connect in reconnecting ({Name}) {Id} failed (state: {_ws.State})");
				Connect();
			}
			catch (Exception e)
			{
				var errorMessage = e.GetFullTextWithInner();
				if (errorMessage.Contains("you needn't connect again!")
					 || errorMessage.Contains("cannot connect again!"))
				{
					return;
				}

				_logger.Trace($"Reconnect ({Name}) {Id} failed (state: {_ws.State}): {errorMessage}");
				Task.Run(ReconnectingSocket);
			}
		}

		private void SocketOnError(object sender, ErrorEventArgs e)
		{
			_logger.Trace($"Socket ({Name}) {Id} (state: {_ws.State}) recieve error: {e.Exception.GetFullTextWithInner()}");
		}

		private void TryDisconnect()
		{
			try
			{
				Disconnect();
			}
			catch (Exception e)
			{
				_logger.Trace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.Message}");
				_logger.Trace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.GetFullTextWithInner()}");
			}
		}

		private void CreateSocket()
		{
			_ws = new WebSocket(BaseUrl, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls)
			{
				EnableAutoSendPing = true,
				AutoSendPingInterval = 10
			};

			_ws.Error += SocketOnError;
			_ws.Opened += OnSocketOpened;
			_ws.Closed += OnSocketClosed;
			_ws.MessageReceived += OnSocketGetMessage;
		}

		#endregion

		public void SubscribeOrders()
		{
			AddChannel(GetOrderChannel());
			SendSubscribeToChannels();
		}

		public void UnsubscribeBookPriceChannel(string instrumentName)
		{
			var orderBookChannel = GetBookPriceChannel(instrumentName);
			UnsubscribeChannel(orderBookChannel);
		}

		#region Generate channel strings

		private OkexChannel GetBookPriceChannel(string instrument)
		{
			var channelName = $"books5{instrument}";
			var okexChannel = _channels.FirstOrDefault(x => x.ChannelName == channelName);
			return okexChannel
					 ?? new OkexChannel(channelName, new SocketInstrumentRequest("books5", instrument));
		}

		private OkexChannel GetOrderChannel()
		{
			var channelName = $"ordersany";
			var okexChannel = _channels.FirstOrDefault(x => x.ChannelName == channelName);
			return okexChannel
					 ?? new OkexChannel(channelName, new SocketOrderRequest("orders", "FUTURES", "BTC-USD-210430"));
		}

		#endregion

		#region Subscribe/unsubscribe

		private void AddChannel(OkexChannel channel)
		{
			var channelParams = _channels.FirstOrDefault(x => x.ChannelName == channel.ChannelName);
			if (channelParams is null)
			{
				_channels.Add(channel);
			}
		}

		internal void SendSubscribeToChannels()
		{
			var channels = _channels
				.Select(x => x.Params)
				.ToArray();

			if (!channels.Any())
			{
				return;
			}

			var request = new OkexSocketRequest("subscribe", channels);
			Send(request);
		}

		private void UnsubscribeChannel(OkexChannel channel)
		{
			var request = new OkexSocketRequest("unsubscribe", channel.Params);
			Send(request);
			_channels.Remove(channel);
		}

		#endregion

		#region ProcessMessage

		private Dictionary<string, Action<OkexSocketResponse>> _eventProcessorActions;
		private Dictionary<ChannelTypeEnum, Action<OkexSocketResponse>> _channelProcessorActions;

		private void InitProcessors()
		{
			_eventProcessorActions = new Dictionary<string, Action<OkexSocketResponse>>
			{
				{"subscribe", ProcessSubscribe},
				{"error", ProcessError},
				{"unsubscribe", ProcessUnsubscribe}
			};
			_channelProcessorActions = new Dictionary<ChannelTypeEnum, Action<OkexSocketResponse>>
			{
				{ChannelTypeEnum.Order, ProcessOrder},
			};
		}

		private void OnSocketGetMessage(object sender, MessageReceivedEventArgs e)
		{
			Task.Run(() => TryProcessMessage(e.Message));
		}

		private void TryProcessMessage(string message)
		{
			try
			{
				ProcessMessage(message);
			}
			catch (Exception exception)
			{
				_logger.Trace($"{nameof(SocketClient)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.GetFullTextWithInner()}.");
			}
		}

		private void ProcessMessage(string message)
		{
			LastMessageDate = DateTime.Now;
			var response = JsonConvert.DeserializeObject<OkexSocketResponse>(message);
			if (string.IsNullOrWhiteSpace(response.Event))
			{
				ProcessSubscription(response);
				return;
			}

			if (!_eventProcessorActions.TryGetValue(response.Event, out var action))
			{
				_logger.Trace($"Unhandled message: {message}");
				return;
			}

			action(response);
		}

		private void ProcessSubscription(OkexSocketResponse response)
		{
			var channel = response.Argument["channel"]?.Value<string>();
			if (string.IsNullOrWhiteSpace(channel))
			{
				return;
			}

			if (string.IsNullOrWhiteSpace(channel)
				 || !_channelTypes.TryGetValue(channel, out var type)
				 || !_channelProcessorActions.TryGetValue(type, out var action))
			{
				return;
			}

			action(response);
		}

		private void ProcessSubscribe(OkexSocketResponse response)
		{
			_logger.Trace($"SUBSCRIBED to channels {JsonConvert.SerializeObject(response.Argument)}");
		}

		private void ProcessUnsubscribe(OkexSocketResponse socketResponse)
		{
			_logger.Trace($"UNSUBSCRIBED from a channels {JsonConvert.SerializeObject(socketResponse.Argument)}");
		}

		private void ProcessError(OkexSocketResponse response)
		{
			_logger.Trace(JsonConvert.SerializeObject(response));
			ErrorReceived.Invoke(new ErrorMessage(response.Code, response.Message));
		}

		private void ProcessOrder(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var orders = data?.ToObject<OkexOrderDetails[]>();
			if (orders is null || !orders.Any())
			{
				return;
			}

			foreach (var order in orders)
			{
				OrderUpdate.Invoke(order);
				_logger.Trace(JsonConvert.SerializeObject(order));
			}
		}

		#endregion

		private void Send(object request)
		{
			if (!SocketConnected)
			{
				return;
			}

			var text = JsonConvert.SerializeObject(request);
			// _logger.Trace($"Send {text}");
			_ws.Send(text);
		}
	}
}
