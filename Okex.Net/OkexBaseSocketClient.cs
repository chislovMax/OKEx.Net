using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Clients;
using Okex.Net.Configs;
using Okex.Net.CoreObjects;
using Okex.Net.Enums;
using Okex.Net.Helpers;
using Okex.Net.Models;

namespace Okex.Net
{
	public abstract class OkexBaseSocketClient : IDisposable
	{
		protected OkexBaseSocketClient(ILogger logger, OkexApiConfig clientConfig, string url)
		{
			_logger = logger;
			_clientConfig = clientConfig;
			_baseUrl = new Uri(url);

			InitProcessors();

			CreateSocket();
		}

		public event Action ConnectionBroken = () => { };
		public event Action ConnectionClosed = () => { };
		public event Action<ErrorMessage> ErrorReceived = error => { };

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "Unnamed";
		public bool SocketConnected => _ws.IsOpen;
		public DateTime LastMessageDate { get; private set; } = DateTime.Now;

		protected abstract Dictionary<string, OkexChannelTypeEnum> ChannelTypes { get; set; }
		protected readonly Dictionary<string, OkexChannel> SubscribedChannels = new Dictionary<string, OkexChannel>();

		private const int ChunkSize = 50;

		private readonly Uri _baseUrl;
		private readonly ILogger _logger;
		private OkexCredential? _credential;
		private readonly OkexApiConfig _clientConfig;
		private readonly SemaphoreSlim _semaphoreSlimReconnect = new SemaphoreSlim(1, 1);

		private CryptoExchangeWebSocketClient _ws;
		private DateTime _lastConnectTime = DateTime.MinValue;
		private int _reconnectTime => _clientConfig.SocketReconnectionTimeMs;
		private Dictionary<string, Action<OkexSocketResponse>> _eventProcessorActions;

		private readonly Dictionary<OkexChannelTypeEnum, Action<OkexSocketResponse>> _channelProcessorActions =
			new Dictionary<OkexChannelTypeEnum, Action<OkexSocketResponse>>();

		#region Init

		protected void SetCredential(OkexCredential credential)
		{
			_credential = credential;
		}

		private void InitProcessors()
		{
			_eventProcessorActions = new Dictionary<string, Action<OkexSocketResponse>>
			{
				{"subscribe", ProcessSubscribe},
				{"error", ProcessError},
				{"unsubscribe", ProcessUnsubscribe},
				{"login", ProcessLogin}
			};
		}

		private void CreateSocket()
		{
			_ws = new CryptoExchangeWebSocketClient(new Log(nameof(OkexSocketClientPublic)), _baseUrl);

			_ws.OnError += SocketOnError;
			_ws.OnOpen += OnSocketOpened;
			_ws.OnClose += OnSocketClosed;
			_ws.OnMessage += OnSocketGetMessage;
		}

		#endregion

		#region Connection

		public async Task ConnectAsync()
		{
			try
			{
				_logger.LogTrace($"Socket ({Name}) {Id} connecting... (IsOpen: {_ws.IsOpen})");
				var isConnect = await _ws.ConnectAsync().ConfigureAwait(false);
				if (!isConnect)
				{
					throw new Exception("Internal socket error");
				}

				_ = _ws.ProcessAsync();
				_logger.LogTrace($"Socket ({Name}) {Id} connected... (IsOpen: {_ws.IsOpen})");
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
				_logger.LogTrace($"Socket ({Name}) {Id}  connect failed {e.GetType().Name} (IsOpen: {_ws.IsOpen}): {e.Message}");
				ConnectionBroken();
				throw;
			}
		}

		public async Task ReconnectAsync()
		{
			var now = DateTime.Now;
			await _semaphoreSlimReconnect.WaitAsync().ConfigureAwait(false);
			try
			{
				if (_lastConnectTime > now)
				{
					return;
				}

				var ws = _ws;
				_ws.OnError -= SocketOnError;
				_ws.OnOpen -= OnSocketOpened;
				_ws.OnClose -= OnSocketClosed;
				_ws.OnMessage -= OnSocketGetMessage;

				CreateSocket();
				ws.Dispose();

				await ConnectAsync().ConfigureAwait(false);
			}
			finally
			{
				_semaphoreSlimReconnect.Release();
			}
		}

		private async Task TryReconnectAsync()
		{
			try
			{
				if (_reconnectTime > 0)
				{
					await Task.Delay(_reconnectTime).ConfigureAwait(false);
				}

				_logger.LogTrace($"Try connect in reconnecting ({Name}) {Id} failed (IsOpen: {_ws.IsOpen})");
				await ReconnectAsync().ConfigureAwait(false);
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
				_ = Task.Run(TryReconnectAsync);
			}
		}

		#endregion

		#region Subscribe/unsubscribe

		protected void AddChannelHandler(OkexChannelTypeEnum channelType, Action<OkexSocketResponse> action)
		{
			_channelProcessorActions.Add(channelType, action);
		}

		protected void SubscribeToChannels(params OkexChannel[] channels)
		{
			СacheChannels(channels);
			SendSubscribeToChannels(channels);
		}

		protected void СacheChannels(params OkexChannel[] channels)
		{
			foreach (var channel in channels)
			{
				if (!SubscribedChannels.TryGetValue(channel.ChannelName, out _))
				{
					SubscribedChannels.Add(channel.ChannelName, channel);
				}
			}
		}

		protected void SendSubscribeToChannels(params OkexChannel[] channels)
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

		protected void UnsubscribeChannel(OkexChannel channel)
		{
			var request = new OkexSocketRequest("unsubscribe", channel.Params);
			Send(request);
			SubscribedChannels.Remove(channel.ChannelName);
		}

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

		#endregion

		#region ProcessMessage

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
				_logger.LogTrace($"{nameof(OkexSocketClientPublic)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.GetFullTextWithInner()}.");
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
				 || !ChannelTypes.TryGetValue(channel, out var type)
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

		private void Auth()
		{
			var time = (DateTime.UtcNow.ToUnixTimeMilliSeconds() / 1000.0m).ToString(CultureInfo.InvariantCulture);
			var signtext = time + "GET" + "/users/self/verify";
			var hmacEncryptor = new HMACSHA256(Encoding.ASCII.GetBytes(_credential.SecretKey));
			var signature = OkexAuthenticationProvider.Base64Encode(hmacEncryptor.ComputeHash(Encoding.UTF8.GetBytes(signtext)));

			var request = new OkexSocketRequest("login", new OkexLoginRequest(_credential.ApiKey, _credential.Password, time, signature));
			Send(request);
		}

		private void ProcessLogin(OkexSocketResponse response)
		{
			SubscribeToChannels();
		}

		protected virtual void OnSocketOpened()
		{
			_logger.LogTrace($"Socket ({Name}) {Id} is open (IsOpen: {_ws.IsOpen})");
			if (_credential is null)
			{
				SendSubscribeToChannels(SubscribedChannels.Values.ToArray());
			}
			else
			{
				Auth();
			}

			_lastConnectTime = DateTime.Now;
		}

		private void OnSocketClosed()
		{
			_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (IsOpen: {_ws.IsOpen})");
			ConnectionClosed.Invoke();

			TryReconnectAsync().Wait();
		}

		private void SocketOnError(Exception exception)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} (IsOpen: {_ws.IsOpen}) recieve error: {exception.GetFullTextWithInner()}");
		}

		#endregion

		#region Dispose

		public void Dispose()
		{
			_ws.OnError -= SocketOnError;
			_ws.OnOpen -= OnSocketOpened;
			_ws.OnClose -= OnSocketClosed;
			_ws.OnMessage -= OnSocketGetMessage;

			_ws.Dispose();
			_semaphoreSlimReconnect.Dispose();
		}

		#endregion
	}
}
