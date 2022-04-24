using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Sockets;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Configs;
using Okex.Net.Enums;
using Okex.Net.Helpers;
using Okex.Net.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OkexSocketRequest = Okex.Net.Models.OkexSocketRequest;
using OkexSocketResponse = Okex.Net.Models.OkexSocketResponse;

namespace Okex.Net.Clients
{
	public class OkexSocketClientPublic : IDisposable
	{
		public OkexSocketClientPublic(ILogger logger, OkexApiConfig clientConfig)
		{
			_logger = logger;
			_clientConfig = clientConfig;
			_baseUrl = _clientConfig.WSUrlPublic;

			InitProcessors();
			CreateSocket();
		}

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "Unnamed";
		public bool SocketConnected => _ws.IsOpen;
		public DateTime LastMessageDate { get; private set; } = DateTime.Now;

		public event Action ConnectionBroken = () => { };
		public event Action ConnectionClosed = () => { };
		public event Action<OkexOrderBook> BookPriceUpdate = bookPrice => { };
		public event Action<OkexTicker> TickerUpdate = ticker => { };
		public event Action<OkexMarkPrice> MarkPriceUpdate = markPrice => { };
		public event Action<OkexLimitPrice> LimitPriceUpdate = limitPrice => { };
		public event Action<ErrorMessage> ErrorReceived = error => { };

		private const int ChunkSize = 50;

		private CryptoExchangeWebSocketClient _ws;
		private bool _onKilled;

		private readonly ILogger _logger;
		private readonly OkexApiConfig _clientConfig;

		private int _reconnectTime => _clientConfig.SocketReconnectionTimeMs;
		private readonly Dictionary<string, OkexChannel> _subscribedChannels = new Dictionary<string, OkexChannel>();
		private readonly Dictionary<string, OkexChannelTypeEnum> _channelTypes = new Dictionary<string, OkexChannelTypeEnum>
		{
			{"books5", OkexChannelTypeEnum.OrderBook},
			{"tickers", OkexChannelTypeEnum.Ticker},
			{"mark-price", OkexChannelTypeEnum.MarkPrice},
			{"price-limit", OkexChannelTypeEnum.LimitPrice}
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

				_logger.LogTrace($"Socket ({Name}) {Id} connecting... (IsOpen: {_ws.IsOpen})");
				var isConnect = await _ws.ConnectAsync().ConfigureAwait(false);
				if (!isConnect)
				{
					throw new Exception("Internal socket error");
				}
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

		private async Task DisconnectAsync()
		{
			if (_ws.IsClosed)
			{
				return;
			}

			_logger.LogTrace($"Socket ({Name}) {Id} disconnecting... (IsOpen: {_ws.IsOpen})");
			await _ws.CloseAsync().ConfigureAwait(false);
			_logger.LogTrace($"Socket ({Name}) {Id} disconnected... (IsOpen: {_ws.IsOpen})");
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
			SendSubscribeToChannels(_subscribedChannels.Values.ToArray());
		}

		private void OnSocketClosed()
		{
			_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (IsOpen: {_ws.IsOpen})");
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

		private void SocketOnError(Exception exception)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} (IsOpen: {_ws.IsOpen}) recieve error: {exception.GetFullTextWithInner()}");
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
			_ws = new CryptoExchangeWebSocketClient(new Log(nameof(OkexSocketClientPublic)), _baseUrl);

			_ws.OnError += SocketOnError;
			_ws.OnOpen += OnSocketOpened;
			_ws.OnClose += OnSocketClosed;
			_ws.OnMessage += OnSocketGetMessage;
		}

		#endregion

		public void SubscribeToTickers(params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetTickerChannel(name));
			}

			SubscribeToChannels(okexChannels);
		}

		public void SubscribeToOrderBooks(string orderBookType = "books5", params string[] instrumentNames)
		{
			if (string.IsNullOrWhiteSpace(orderBookType))
				throw new ArgumentException("Order book type must not be null or empty", orderBookType);

			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetOrderBookChannel(name, orderBookType));
			}

			SubscribeToChannels(okexChannels);
		}

		public void SubscribeToMarkPrice(string instrumentName)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				throw new ArgumentException("Instrument name must not be null or empty", instrumentName);

			var channel = GetMarkPriceChannel(instrumentName);
			CashChannels(channel);
			SendSubscribeToChannels(channel);
		}

		public void SubscribeToMarkPrices(params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);
				okexChannels.Add(GetMarkPriceChannel(name));
			}

			SubscribeToChannels(okexChannels);
		}

		public void SubscribeToLimitPrices(params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetLimitPriceChannel(name));
			}

			SubscribeToChannels(okexChannels);
		}

		public void UnsubscribeBookPriceChannel(string instrumentName, string orderBookType)
		{
			var orderBookChannel = GetOrderBookChannel(instrumentName, orderBookType);
			UnsubscribeChannel(orderBookChannel);
		}

		public void UnsubscribeTickerChannel(string instrumentName)
		{
			var tickerChannel = GetTickerChannel(instrumentName);
			UnsubscribeChannel(tickerChannel);
		}

		public void UnsubscribeMarkPriceChannel(string instrumentName)
		{
			var markPriceChannel = GetMarkPriceChannel(instrumentName);
			UnsubscribeChannel(markPriceChannel);
		}

		public void UnsubscribeLimitPriceChannel(string instrumentName)
		{
			var limitPriceChannel = GetLimitPriceChannel(instrumentName);
			UnsubscribeChannel(limitPriceChannel);
		}

		#region Generate channel strings

		private OkexChannel GetOrderBookChannel(string instrumentName, string orderBookType)
		{
			var channelName = $"{orderBookType}{instrumentName}";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", orderBookType }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetTickerChannel(string instrumentName)
		{
			var channelName = $"tickers{instrumentName}";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "tickers" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetMarkPriceChannel(string instrument)
		{
			var channelName = $"mark-price{instrument}";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "mark-price" }, { "instId", instrument } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetLimitPriceChannel(string instrumentName)
		{
			var channelName = $"price-limit{instrumentName}";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "price-limit" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		#endregion

		#region Subscribe/unsubscribe

		private void SubscribeToChannels(List<OkexChannel> channels)
		{
			var okexChannelsArray = channels.ToArray();
			CashChannels(okexChannelsArray);
			SendSubscribeToChannels(okexChannelsArray);
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
				{"unsubscribe", ProcessUnsubscribe}
			};
			_channelProcessorActions = new Dictionary<OkexChannelTypeEnum, Action<OkexSocketResponse>>
			{
				{OkexChannelTypeEnum.OrderBook, ProcessOrderBook},
				{OkexChannelTypeEnum.Ticker, ProcessTicker},
				{OkexChannelTypeEnum.MarkPrice, ProcessMarkPrice},
				{OkexChannelTypeEnum.LimitPrice, ProcessLimitPrice}
			};
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

		private void ProcessOrderBook(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var bookPrice = data?.ToObject<OkexOrderBook>();
			var instrument = response.Argument["instId"]?.Value<string>();
			if (bookPrice is null || string.IsNullOrWhiteSpace(instrument))
			{
				return;
			}

			bookPrice.InstrumentName = instrument;
			BookPriceUpdate.Invoke(bookPrice);
		}

		private void ProcessTicker(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var ticker = data?.ToObject<OkexTicker>();
			var instrument = response.Argument["instId"]?.Value<string>();
			if (ticker is null || string.IsNullOrWhiteSpace(instrument))
			{
				return;
			}

			ticker.InstrumentName = instrument;
			TickerUpdate.Invoke(ticker);
		}

		private void ProcessMarkPrice(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var markPrice = data?.ToObject<OkexMarkPrice>();
			var instrument = response.Argument["instId"]?.Value<string>();
			if (markPrice is null || string.IsNullOrWhiteSpace(instrument))
			{
				return;
			}

			markPrice.InstrumentName = instrument;
			MarkPriceUpdate.Invoke(markPrice);
		}

		private void ProcessLimitPrice(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var limitPrice = data?.ToObject<OkexLimitPrice>();
			if (limitPrice is null)
			{
				return;
			}

			LimitPriceUpdate.Invoke(limitPrice);
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
