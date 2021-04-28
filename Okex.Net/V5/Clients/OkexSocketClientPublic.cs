using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using Okex.Net.SocketObjects.Structure;
using Okex.Net.V5.Enums;
using Okex.Net.V5.Models;
using WebSocket4Net;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OkexSocketRequest = Okex.Net.V5.Models.OkexSocketRequest;
using OkexSocketResponse = Okex.Net.V5.Models.OkexSocketResponse;

namespace Okex.Net.V5.Clients
{
	public class OkexSocketClientPublic
	{
		public OkexSocketClientPublic()
		{
			InitProcessors();

			CreateSocket();
		}

		public Guid Id { get; } = Guid.NewGuid();
		public string Name { get; set; } = "Unnamed";
		public bool SocketConnected => _ws.State == WebSocketState.Open;
		public DateTime LastMessageDate { get; private set; } = DateTime.MinValue;

		internal event Action ConnectionBroken = () => { };
		internal event Action<OkexOrderDetails> OrderUpdated = order => { };
		internal event Action<OkexOrderBook> BookPriceUpdate = bookPrice => { };
		internal event Action<OkexTicker> TickerUpdate = ticker => { };
		internal event Action<ErrorMessage> ErrorReceived = error => { };
		//internal event Action<IndexPrice> IndexPriceUpdate = price => { };
		//internal event Action<UserChanges> UserChangesUpdate = changes => { };

		private bool _onKilled;
		private int _privateApiId;
		private string _accessToken;

		private WebSocket _ws;
		private readonly int _reconnectTime;
		private readonly Logger _logger = LogManager.GetCurrentClassLogger();
		private readonly List<OkexChannel> _channels = new List<OkexChannel>();
		private readonly ChannelTypeEnum[] _userChannelTypes = { ChannelTypeEnum.BookPrice, };
		private readonly Dictionary<string, ChannelTypeEnum> _channelSubscribes = new Dictionary<string, ChannelTypeEnum>();
		private readonly Dictionary<string, ChannelTypeEnum> _channelTypes = new Dictionary<string, ChannelTypeEnum>
		{
			{"books5", ChannelTypeEnum.BookPrice}
		};

		private const string BaseUrl = "wss://www.deribit.com/ws/api/v2";
		private const string TestBaseUrl = "wss://wsaws.okex.com:8443/ws/v5/public";

		#region Connection

		public void Connect()
		{
			try
			{
				if (_onKilled)
				{
					return;
				}

				//_logger.LogTrace($"Socket ({Name}) {Id} connecting... (state: {_ws.State})");
				_ws.Open();
				//_logger.LogTrace($"Socket ({Name}) {Id} connected... (state: {_ws.State})");
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
				//_logger.LogTrace($"Socket ({Name}) {Id}  connect failed {e.GetType().Name} (state: {_ws.State}): {e.Message}");
				throw;
			}
		}

		public void Disconnect()
		{
			if (_ws.State == WebSocketState.Closed || _ws.State == WebSocketState.Closing)
			{
				return;
			}

			//_logger.LogTrace($"Socket ({Name}) {Id} disconnecting... (state: {_ws.State})");
			_ws.Close();
			//_logger.LogTrace($"Socket ({Name}) {Id} disconnected... (state: {_ws.State})");
		}

		public void Reconnect()
		{
			var ws = _ws;
			//_ws.Error -= SocketOnError;
			_ws.Opened -= OnSocketOpened;
			_ws.Closed -= OnSocketClosed;
			_ws.MessageReceived -= OnSocketGetMessage;

			CreateSocket();
			Connect();

			ws.Dispose();
		}

		public void Kill()
		{
			//_logger.LogTrace("Socket killing...");

			_onKilled = true;
			TryDisconnect();
			_ws.Dispose();
		}

		private void OnSocketOpened(object sender, EventArgs e)
		{
			//_logger.LogTrace($"Socket ({Name}) {Id} is open (state: {_ws.State}): resubscribing...");
			//Auth();
			//SendSubscribeToPublicChannels();
			//SendSetHeartbeat();
		}

		private void OnSocketClosed(object sender, EventArgs e)
		{
			//_logger.LogTrace($"Socket ({Name}) {Id} OnSocketClosed... (state: {_ws.State})");
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

				//_logger.LogTrace($"Try connect in reconnecting ({Name}) {Id} failed (state: {_ws.State})");
				Connect();
			}
			catch (Exception e)
			{
				var errorMessage = e.Message;
				//var errorMessage = e.GetFullTextWithInner();
				if (errorMessage.Contains("you needn't connect again!")
					 || errorMessage.Contains("cannot connect again!"))
				{
					return;
				}

				//_logger.LogTrace($"Reconnect ({Name}) {Id} failed (state: {_ws.State}): {errorMessage}");
				Task.Run(ReconnectingSocket);
			}
		}

		private void SocketOnError(object sender, ErrorEventArgs e)
		{
			//_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) recieve error: {e.GetException().Message}");
			//_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) recieve error: {e.GetException().GetFullTextWithInner()}");
		}

		private void TryDisconnect()
		{
			try
			{
				Disconnect();
			}
			catch (Exception e)
			{
				//_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.Message}");
				//_logger.LogTrace($"Socket ({Name}) {Id} (state: {_ws.State}) error in disconnect: {e.GetFullTextWithInner()}");
			}
		}

		private void CreateSocket()
		{
			_ws = new WebSocket(/*_config.IsTestApi*/ true ? TestBaseUrl : BaseUrl, sslProtocols: SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls)
			{
				EnableAutoSendPing = true,
				AutoSendPingInterval = 10
			};

			//_ws.Error += SocketOnError;
			_ws.Opened += OnSocketOpened;
			_ws.Closed += OnSocketClosed;
			_ws.MessageReceived += OnSocketGetMessage;
		}

		#endregion

		#region Public channels

		private const string SetHeartBeatMethod = "public/set_heartbeat";
		private const string TestMethod = "public/test";

		//internal void SendSetHeartbeat()
		//{
		//	var param = new SetHeartbeatParams();
		//	var request = new RequestMessage(SetHeartBeatMethod, 100, param);
		//	Send(request);
		//}

		//internal void SendTest()
		//{
		//	var request = new RequestMessage(TestMethod, 100, new { });
		//	Send(request);
		//}

		//internal void SubscribeToTicker(string instrument)
		//{
		//	var channel = GetTickerChannel(instrument);
		//	AddChannel(channel, ChannelTypes.Ticker);
		//	SendSubscribeToPublicChannels();
		//}

		public void SubscribeToBookPrice(string instrumentName)
		{
			AddChannel(GetBookPriceChannel(instrumentName));
			SendSubscribeToChannels();
		}

		//internal void SubscribeToBookPrices(params string[] instruments)
		//{
		//	var channels = instruments.Select(GetBookPriceChannel).ToArray();
		//	AddChannels(channels, ChannelTypeEnum.BookPrice);
		//	//SendSubscribeToPublicChannels();
		//}

		//internal void SubscribeToIndexPrices()
		//{
		//	SubscribeToIndexPrice("btc");
		//	SubscribeToIndexPrice("eth");
		//}

		//private void SubscribeToIndexPrice(string currency)
		//{
		//	var channel = GetIndexPriceChannel(currency);
		//	AddChannel(channel, ChannelTypes.IndexPrice);
		//	SendSubscribeToPublicChannels();
		//}

		public void UnsubscribeBookPriceChannel(string instrumentName)
		{
			var channelInstrumentBook = GetBookPriceChannel(instrumentName);
			UnsubscribeChannel(channelInstrumentBook);
		}

		//internal void UnsubscribeToTicker(string instrument)
		//{
		//	var channel = GetTickerChannel(instrument);
		//	UnsubscribeToPublicChannel(channel);
		//}

		//#region Generate channel strings

		//private string GetIndexPriceChannel(string currency)
		//{
		//	return $"deribit_price_index.{currency.ToLower()}_usd";
		//}

		private OkexChannel GetBookPriceChannel(string instrument)
		{
			var channelName = $"books5{instrument}";
			var okexChannel = _channels.FirstOrDefault(x => x.ChannelName == channelName);
			return okexChannel
					 ?? new OkexChannel(channelName, new OrderBookRequest("books5", instrument));
		}

		//private string GetTickerChannel(string instrument)
		//{
		//	return $"ticker.{instrument}.100ms";
		//}

		#endregion

		#region Subscribe/unsubscribe

		private const string PublicMethodSubscribe = "public/subscribe";
		private const string PrivateMethodSubscribe = "private/subscribe";
		private const string PublicMethodUnsubscribe = "public/unsubscribe";
		private const string PrivateMethodUnsubscribe = "private/unsubscribe";

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
			//var channels = _channelSubscribes
			//	.Where(x => _userChannelTypes.Contains(x.Value))
			//	.Select(x => x.Key)
			//	.ToArray();

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
			var request = new OkexSocketRequest("unsubscribe", new[] { channel.Params });
			Send(request);
			_channels.Remove(channel);
		}

		#endregion

		#region ProcessMessage

		private Dictionary<string, Action<OkexSocketResponse>> _methodProcessorActions;
		private Dictionary<ChannelTypeEnum, Action<OkexSocketResponse>> _channelProcessorActions;

		private void InitProcessors()
		{
			_methodProcessorActions = new Dictionary<string, Action<OkexSocketResponse>>
			{
				{"subscribe", ProcessSubscription},
				{"error", ProcessError},
				{"unsubscribe", ProcessUnsubscribe}
			};
			_channelProcessorActions = new Dictionary<ChannelTypeEnum, Action<OkexSocketResponse>>
			{
				{ChannelTypeEnum.BookPrice, ProcessBookPrice},
				//{ChannelTypes.Ticker, ProcessTicker},
				//{ChannelTypes.IndexPrice, ProcessIndexPrice},
				//{ChannelTypes.UserChanges, ProcessUserChanges},
				//{ChannelTypes.Order, ProcessOrder}
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
				_logger.Trace($"{nameof(SocketClient)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.Message}.");
				//_logger.LogTrace($"{nameof(SocketClient)} ERROR ON PROCESS MESSAGE.\n {message} \n {exception.GetFullTextWithInner()}.");
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

			if (!_methodProcessorActions.TryGetValue(response.Event, out var action))
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

		private void ProcessError(OkexSocketResponse response)
		{
			ErrorReceived.Invoke(new ErrorMessage(response.Code, response.Message));
		}

		private void ProcessUnsubscribe(OkexSocketResponse socketResponse)
		{
			_logger.Trace($"UNSUBSCRIBE from a channel {JsonConvert.SerializeObject(socketResponse.Argument)}");
		}

		//private void ProcessHeartbeat(ResponseParameters param)
		//{
		//	if (param?.ParamType.IsNullOrWhiteSpace() ?? true)
		//	{
		//		return;
		//	}

		//	if (param.ParamType == "test_request")
		//	{
		//		SendTest();
		//	}
		//}

		//private void ProcessOrder(JToken data)
		//{
		//	var orders = data.Is<JArray>() ? data.ToObject<Order[]>() : new[] { data.ToObject<Order>() };
		//	foreach (var order in orders)
		//	{
		//		//TODO: async
		//		OrderUpdated.Invoke(order);
		//	}
		//}

		//private void ProcessUserChanges(JToken data)
		//{
		//	var userChanges = data.ToObject<UserChanges>();
		//	UserChangesUpdate.Invoke(userChanges);
		//}

		//private void ProcessIndexPrice(JToken data)
		//{
		//	var indexPrice = data.ToObject<IndexPrice>();
		//	IndexPriceUpdate.Invoke(indexPrice);
		//}

		private void ProcessBookPrice(OkexSocketResponse response)
		{
			var instrument = response.Argument["instId"]?.Value<string>();
			var data = response.Data?.FirstOrDefault();
			var bookPrice = data?.ToObject<OkexOrderBook>();
			if (bookPrice is null || string.IsNullOrWhiteSpace(instrument))
			{
				return;
			}

			bookPrice.InstrumentName = instrument;
			_logger.Trace(JsonConvert.SerializeObject(bookPrice));
			BookPriceUpdate.Invoke(bookPrice);
		}

		//private void ProcessTicker(JToken data)
		//{
		//	var ticker = data.ToObject<Ticker>();
		//	TickerUpdate.Invoke(ticker);
		//}

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
