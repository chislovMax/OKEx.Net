using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Configs;
using Okex.Net.CoreObjects;
using Okex.Net.Enums;
using Okex.Net.Helpers;
using Okex.Net.Models;

namespace Okex.Net.Clients
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
		public bool SocketConnected => _ws.IsOpen;
		public DateTime LastMessageDate { get; private set; } = DateTime.Now;

		public event Action ConnectionBroken = () => { };
		public event Action ConnectionClosed = () => { };
		public event Action<OkexOrderDetails> OrderUpdate = order => { };
		public event Action<OkexAccountDetails> AccountUpdate = order => { };
		public event Action<ErrorMessage> ErrorReceived = error => { };

		private const int ChunkSize = 50;

		private bool _onKilled;

		private readonly ILogger _logger;
		private readonly OkexApiConfig _clientConfig;

		private CryptoExchangeWebSocketClient _ws;
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

		public async Task ConnectAsync()
		{
			try
			{
				if (_onKilled)
				{
					return;
				}

				_logger.LogTrace($"Socket ({Name}) {Id} connecting... isOpen: {_ws.IsOpen}");

				var isConnect = await _ws.ConnectAsync().ConfigureAwait(false);
				if (!isConnect)
				{
					throw new Exception("Internal socket error");
				}
				_logger.LogTrace($"Socket ({Name}) {Id} connected... isOpen {_ws.IsOpen}");
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
				_logger.LogTrace($"Socket ({Name}) {Id}  connect failed {e.GetType().Name} (isOpen: {_ws.IsOpen} ): {e.Message}");
				ConnectionBroken();
				throw;
			}
		}

		public async Task DisconnectAsync()
		{
			if (_ws.IsClosed)
			{
				return;
			}

			_logger.LogTrace($"Socket ({Name}) {Id} disconnecting... (isOpen: {_ws.IsOpen})");
			await _ws.CloseAsync().ConfigureAwait(false);
			_logger.LogTrace($"Socket ({Name}) {Id} disconnected... (isOpen: {_ws.IsOpen})");
		}

		public async Task ReconnectAsync()
		{
			var ws = _ws;
			_ws.OnError -= SocketOnError;
			_ws.OnOpen -= OnSocketOpened;
			_ws.OnClose -= OnSocketClosed;
			_ws.OnMessage -= OnSocketGetMessage;
			
			CreateSocket();
			await ConnectAsync().ConfigureAwait(false);

			ws.Dispose();
		}

		public async Task KillAsync()
		{
			_logger.LogTrace("Socket killing...");

			_onKilled = true;
			await TryDisconnectAsync().ConfigureAwait(false);
			_ws.Dispose();
		}

		private void OnSocketOpened()
		{
			_logger.LogTrace($"Socket ({Name}) {Id} is open (IsOpen: {_ws.IsOpen}): resubscribing...");
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

		private void OnSocketClosed()
		{
			_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (isOpen: {_ws.IsOpen})");
			ConnectionClosed.Invoke();
			ReconnectingSocketAsync().Wait();
		}

		private async Task ReconnectingSocketAsync()
		{
			try
			{
				if (_reconnectTime > 0)
				{
					await Task.Delay(_reconnectTime).ConfigureAwait(false);
				}

				_logger.LogTrace($"Try connect in reconnecting ({Name}) {Id} failed (IsOpen: {_ws.IsOpen})");
				await ConnectAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				var errorMessage = e.GetFullTextWithInner();
				if (errorMessage.Contains("you needn't connect again!")
					 || errorMessage.Contains("cannot connect again!"))
				{
					return;
				}

				_logger.LogTrace($"Reconnect ({Name}) {Id} failed (IsOpen: {_ws.IsOpen}): {errorMessage}");
				_ = Task.Run(ReconnectingSocketAsync);
			}
		}

		private void SocketOnError(Exception error)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} (isOpen: {_ws.IsOpen}) recieve error: {error.GetFullTextWithInner()}");
		}

		private async Task TryDisconnectAsync()
		{
			try
			{
				await DisconnectAsync().ConfigureAwait(false);
			}
			catch (Exception e)
			{
				_logger.LogTrace($"Socket ({Name}) {Id} (IsOpen: {_ws.IsOpen}) error in disconnect: {e.Message}");
				_logger.LogTrace($"Socket ({Name}) {Id} (IsOpen: {_ws.IsOpen}) error in disconnect: {e.GetFullTextWithInner()}");
			}
		}

		private void CreateSocket()
		{
			_ws = new CryptoExchangeWebSocketClient(new Log(nameof(OkexSocketClientPrivate)), _baseUrl);

			_ws.OnError += SocketOnError;
			_ws.OnOpen += OnSocketOpened;
			_ws.OnClose += OnSocketClosed;
			_ws.OnMessage += OnSocketGetMessage;
		}

		#endregion

		public void SubscribeToChangeOrders(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			SubscribeToChannels(GetOrderChannel(instrumentType, underlying, instrumentName));
		}

		public void UnsubscribeChangeOrderChannel(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			var orderChannel = GetOrderChannel(instrumentType, underlying, instrumentName);
			UnsubscribeChannel(orderChannel);
		}

		public void SubscribeToChangeAccount(string currency = "")
		{
			SubscribeToChannels(GetAccountChannel(currency));
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

		private void SubscribeToChannels(params OkexChannel[] channels)
		{
			CashChannels(channels);
			SendSubscribeToChannels(channels);
		}

		private void CashChannels(params OkexChannel[] channels)
		{
			foreach (var channel in channels)
			{
				if (!_subscribedChannels.TryGetValue(channel.ChannelName, out _))
				{
					_subscribedChannels.Add(channel.ChannelName, channel);
				}
			}
		}

		private void SendSubscribeToChannels(params OkexChannel[] channels)
		{
			var chunks = channels.Chunk(ChunkSize).ToArray();
			if (!chunks.Any())
			{
				return;
			}

			foreach (var chunk in chunks)
			{
				Send(new OkexSocketRequest("subscribe", chunk.Select(x => x.Params).ToArray()));
			}
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
			SubscribeToChannels();
		}

		private void OnSocketGetMessage(string message)
		{
			Task.Run(() => TryProcessMessage(message));
		}

		private void TryProcessMessage(string message)
		{
			try
			{
				ProcessMessage(message);
			}
			catch (Exception exception)
			{
				_logger.LogTrace($"{nameof(OkexSocketClientPrivate)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.GetFullTextWithInner()}.");
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
			_ws.OnError -= SocketOnError;
			_ws.OnOpen -= OnSocketOpened;
			_ws.OnClose -= OnSocketClosed;
			_ws.OnMessage -= OnSocketGetMessage;

			_ws.Dispose();
		}
	}
}
