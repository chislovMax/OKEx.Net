using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Helpers;
using Okex.Net.V5.Configs;
using Okex.Net.V5.Enums;
using Okex.Net.V5.Models;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OkexSocketRequest = Okex.Net.V5.Models.OkexSocketRequest;
using OkexSocketResponse = Okex.Net.V5.Models.OkexSocketResponse;

namespace Okex.Net.V5.Clients
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
		public bool SocketConnected => _ws.State == WebSocketState.Open;
		public DateTime LastMessageDate { get; private set; } = DateTime.Now;

		public event Action ConnectionBroken = () => { };
		public event Action ConnectionClosed = () => { };
		public event Action<OkexOrderBook> BookPriceUpdate = bookPrice => { };
		public event Action<OkexTicker> TickerUpdate = ticker => { };
		public event Action<OkexMarkPrice> MarkPriceUpdate = markPrice => { };
		public event Action<OkexLimitPrice> LimitPriceUpdate = limitPrice => { };
		public event Action<OkexFundingRate> FundingRateUpdate = fundingRate => { };
		public event Action<ErrorMessage> ErrorReceived = error => { };

		private const int ChunkSize = 50;

		private WebSocket _ws;
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
			{"price-limit", OkexChannelTypeEnum.LimitPrice},
			{"funding-rate", OkexChannelTypeEnum.FundingRate}
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
				ConnectionBroken();
				throw;
			}
		}

		private void Disconnect()
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
			SendSubscribeToChannels(_subscribedChannels.Values.ToArray());
		}

		private void OnSocketClosed(object sender, EventArgs e)
		{
			_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (state: {_ws.State})");
			ConnectionClosed.Invoke();
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

		public void SubscribeToFundingRates(params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetFundingRateChannel(name));
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

		public void UnsubscribeFundingRateChannel(string instrumentName)
		{
			var fundingRateChannel = GetFundingRateChannel(instrumentName);
			UnsubscribeChannel(fundingRateChannel);
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

		private OkexChannel GetFundingRateChannel(string instrumentName)
		{
			const string channelName = "funding-rate";
			if (_subscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", channelName }, { "instId", instrumentName } };
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

			foreach (var chunk  in chunks)
			{
				Send(new OkexSocketRequest("subscribe", chunk.Select(x=> x.Params).ToArray()));
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
				{OkexChannelTypeEnum.LimitPrice, ProcessLimitPrice},
				{OkexChannelTypeEnum.FundingRate, ProcessFundingRate}
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

		private void ProcessFundingRate(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var fundingRate = data?.ToObject<OkexFundingRate>();
			if (fundingRate is null)
			{
				return;
			}

			FundingRateUpdate.Invoke(fundingRate);
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
			_ws.Error -= SocketOnError;
			_ws.Opened -= OnSocketOpened;
			_ws.Closed -= OnSocketClosed;
			_ws.MessageReceived -= OnSocketGetMessage;

			_ws.Dispose();
		}
	}
}
