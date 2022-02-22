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
using Okex.Net.CoreObjects;
using Okex.Net.Helpers;
using Okex.Net.V5.Configs;
using Okex.Net.V5.Enums;
using Okex.Net.V5.Models;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

namespace Okex.Net.V5.Clients
{
	public class OkexSocketClientPrivate : IDisposable
	{
		public OkexSocketClientPrivate(ILogger logger, OkexCredential credential, OkexApiConfig clientConfig)
		{
			_logger = logger;
			_clientConfig = clientConfig;
			_credential = credential;
			_baseUrl = _clientConfig.WSUrlPrivate;

			InitProcessors();
			CreateSocket();
		}

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "Unnamed";
		public bool SocketConnected => _ws.State == WebSocketState.Open;
		public DateTime LastMessageDate { get; private set; } = DateTime.MinValue;

		public event Action ConnectionBroken = () => { };
		public event Action<OkexOrderDetails> OrderUpdate = order => { };
		public event Action<OkexAccountDetails> AccountUpdate = order => { };
		public event Action<ErrorMessage> ErrorReceived = error => { };

		private bool _onKilled;

		private readonly ILogger _logger;
		private readonly OkexApiConfig _clientConfig;

		private WebSocket _ws;
		private readonly OkexCredential _credential;
		private int _reconnectTime => _clientConfig.SocketReconnectionTimeMs;
		private readonly Dictionary<string, OkexChannel> _subscribedChannels = new Dictionary<string, OkexChannel>();
		private readonly Dictionary<string, OkexChannelTypeEnum> _channelTypes = new Dictionary<string, OkexChannelTypeEnum>
		{
			{"orders", OkexChannelTypeEnum.Order},
			{"account", OkexChannelTypeEnum.Account}
		};

		private readonly string _baseUrl;


		#region Connection

		public void Connect()
		{
			try
			{
				if (_onKilled)
				{
					return;
				}

				_logger.LogTrace($"Socket ({Name}) {Id} connecting... (state: {_ws.State})");
				_ws.Open();
				_logger.LogTrace($"Socket ({Name}) {Id} connected... (state: {_ws.State})");
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
				_logger.LogTrace($"Socket ({Name}) {Id}  connect failed {e.GetType().Name} (state: {_ws.State}): {e.Message}");
				throw;
			}
		}

		public void Disconnect()
		{
			if (_ws.State == WebSocketState.Closed || _ws.State == WebSocketState.Closing)
			{
				return;
			}

			_logger.LogTrace($"Socket ({Name}) {Id} disconnecting... (state: {_ws.State})");
			_ws.Close();
			_logger.LogTrace($"Socket ({Name}) {Id} disconnected... (state: {_ws.State})");
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
			_logger.LogTrace("Socket killing...");

			_onKilled = true;
			TryDisconnect();
			_ws.Dispose();
		}

		private void OnSocketOpened(object sender, EventArgs e)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} is open (state: {_ws.State}): resubscribing...");
			Auth();
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

			var request = new OkexSocketRequest("login", new OkexLoginRequest(_credential.ApiKey, _credential.Password, time, signature));
			Send(request);
		}

		private void OnSocketClosed(object sender, EventArgs e)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (state: {_ws.State})");
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

				_logger.LogTrace($"Try connect in reconnecting ({Name}) {Id} failed (state: {_ws.State})");
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

				_logger.LogTrace($"Reconnect ({Name}) {Id} failed (state: {_ws.State}): {errorMessage}");
				Task.Run(ReconnectingSocket);
			}
		}

		private void SocketOnError(object sender, ErrorEventArgs e)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) recieve error: {e.Exception.GetFullTextWithInner()}");
		}

		private void TryDisconnect()
		{
			try
			{
				Disconnect();
			}
			catch (Exception e)
			{
				_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.Message}");
				_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.GetFullTextWithInner()}");
			}
		}

		private void CreateSocket()
		{
			_ws = new WebSocket(_baseUrl, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls)
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

		public void SubscribeToChangeOrders(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			AddChannel(GetOrderChannel(instrumentType, underlying, instrumentName));
			SendSubscribeToChannels();
		}

		public void UnsubscribeChangeOrderChannel(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			var orderChannel = GetOrderChannel(instrumentType, underlying, instrumentName);
			UnsubscribeChannel(orderChannel);
		}

		public void SubscribeToChangeAccount(string currency = "")
		{
			AddChannel(GetAccountChannel(currency));
			SendSubscribeToChannels();
		}

		public void UnsubscribeToChangeAccountChannel(string currency = "")
		{
			var accountChannel = GetAccountChannel(currency);
			UnsubscribeChannel(accountChannel);
		}
		#region Generate channel strings

		private OkexChannel GetOrderChannel(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			var channelName = $"orders{instrumentType.ToString()}{underlying}{instrumentName}";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "orders" }, { "instType", instrumentType.ToString() } };
			if (!string.IsNullOrWhiteSpace(underlying)) channelArgs.Add("uly", underlying);
			if (!string.IsNullOrWhiteSpace(instrumentName)) channelArgs.Add("instId", instrumentName);

			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetAccountChannel(string currency = "")
		{
			var channelName = $"account{currency}";

			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var args = new Dictionary<string, string> { { "channel", "account" } };
			if (!string.IsNullOrWhiteSpace(currency)) args.Add("ccy", currency);

			return new OkexChannel(channelName, args);
		}

		#endregion

		#region Subscribe/unsubscribe

		private void AddChannel(OkexChannel okexChannel)
		{
			if (!_subscribedChannels.TryGetValue(okexChannel.ChannelName, out _))
			{
				_subscribedChannels.Add(okexChannel.ChannelName, okexChannel);
			}
		}

		internal void SendSubscribeToChannels()
		{
			var channelParams = _subscribedChannels
				.Select(x => x.Value.Params)
				.ToArray();

			if (!channelParams.Any())
			{
				return;
			}

			var request = new OkexSocketRequest("subscribe", channelParams);
			Send(request);
		}

		private void UnsubscribeChannel(OkexChannel channel)
		{
			var request = new OkexSocketRequest("unsubscribe", channel.Params);
			Send(request);
			_subscribedChannels.Remove(channel.ChannelName);
		}

		#endregion

		#region ProcessMessage

		private Dictionary<string, Action<OkexSocketResponse>> _eventProcessorActions;
		private Dictionary<OkexChannelTypeEnum, Action<OkexSocketResponse>> _channelProcessorActions;

		private void InitProcessors()
		{
			_eventProcessorActions = new Dictionary<string, Action<OkexSocketResponse>>
			{
				{"subscribe", ProcessSubscribe},
				{"error", ProcessError},
				{"unsubscribe", ProcessUnsubscribe},
				{"login", ProcessLogin}
			};
			_channelProcessorActions = new Dictionary<OkexChannelTypeEnum, Action<OkexSocketResponse>>
			{
				{OkexChannelTypeEnum.Order, ProcessOrder},
				{OkexChannelTypeEnum.Account, ProcessAccount},
			};
		}

		private void ProcessLogin(OkexSocketResponse response)
		{
			SendSubscribeToChannels();
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
				_logger.LogTrace($"{nameof(SocketClient)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.GetFullTextWithInner()}.");
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
				_logger.LogTrace($"Unhandled message: {message}");
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
			//_logger.LogTrace($"SUBSCRIBED to channels {JsonConvert.SerializeObject(response.Argument)}");
		}

		private void ProcessUnsubscribe(OkexSocketResponse socketResponse)
		{
			//_logger.LogTrace($"UNSUBSCRIBED from a channels {JsonConvert.SerializeObject(socketResponse.Argument)}");
		}

		private void ProcessError(OkexSocketResponse response)
		{
			_logger.LogTrace(JsonConvert.SerializeObject(response));
			ErrorReceived.Invoke(new ErrorMessage(response.Code, response.Message));
		}

		private void ProcessOrder(OkexSocketResponse response)
		{
			var orders = response.Data?.ToObject<OkexOrderDetails[]>();
			if (orders is null || !orders.Any())
			{
				return;
			}

			foreach (var order in orders)
			{
				OrderUpdate.Invoke(order);
			}
		}

		private void ProcessAccount(OkexSocketResponse response)
		{
			var balances = response.Data?.ToObject<OkexAccountDetails[]>();
			if (balances is null || !balances.Any())
			{
				return;
			}

			foreach (var balance in balances)
			{
				AccountUpdate.Invoke(balance);
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
			_ws.Send(text);
		}

		public void Dispose()
		{
			_ws.Error -= SocketOnError;
			_ws.Opened -= OnSocketOpened;
			_ws.Closed -= OnSocketClosed;
			_ws.MessageReceived -= OnSocketGetMessage;

			_ws.Dispose();
		}
	}
}
