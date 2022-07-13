using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Okex.Net.Configs;
using Okex.Net.Enums;
using Okex.Net.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger;
using OkexSocketResponse = Okex.Net.Models.OkexSocketResponse;

namespace Okex.Net.Clients
{
	public class OkexSocketClientPublic : OkexBaseSocketClient
	{
		public OkexSocketClientPublic(ILogger logger, OkexApiConfig clientConfig)
			: base(logger, clientConfig, clientConfig.WSUrlPublic)
		{
			#region Candles
			
			AddChannelHandler(OkexChannelTypeEnum.Candle1m, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle3m, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle5m, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle15m, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle30m, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle1H, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle2H, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle4H, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle6H, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle12H, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle1D, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle2D, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle3D, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle5D, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle1W, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle1M, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle3M, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle6M, ProcessCandle);
			AddChannelHandler(OkexChannelTypeEnum.Candle1Y, ProcessCandle);

			#endregion

			AddChannelHandler(OkexChannelTypeEnum.OrderBook, ProcessOrderBook);
			AddChannelHandler(OkexChannelTypeEnum.Ticker, ProcessTicker);
			AddChannelHandler(OkexChannelTypeEnum.MarkPrice, ProcessMarkPrice);
			AddChannelHandler(OkexChannelTypeEnum.LimitPrice, ProcessLimitPrice);
			AddChannelHandler(OkexChannelTypeEnum.FundingRate, ProcessFundingRate);
		}

		public event Action<OkexOrderBook> BookPriceUpdate = bookPrice => { };
		public event Action<OkexCandleStick> CandleUpdate = candle => { };
		public event Action<OkexTicker> TickerUpdate = ticker => { };
		public event Action<OkexMarkPrice> MarkPriceUpdate = markPrice => { };
		public event Action<OkexLimitPrice> LimitPriceUpdate = limitPrice => { };
		public event Action<OkexFundingRate> FundingRateUpdate = fundingRate => { };

		protected override Dictionary<string, OkexChannelTypeEnum> ChannelTypes { get; set; } = new Dictionary<string, OkexChannelTypeEnum>
		{
			#region Candles

			{"candle1m", OkexChannelTypeEnum.Candle1m},
			{"candle3m", OkexChannelTypeEnum.Candle3m},
			{"candle5m", OkexChannelTypeEnum.Candle5m},
			{"candle15m", OkexChannelTypeEnum.Candle15m},
			{"candle30m", OkexChannelTypeEnum.Candle30m},
			{"candle1H", OkexChannelTypeEnum.Candle1H},
			{"candle2H", OkexChannelTypeEnum.Candle2H},
			{"candle4H", OkexChannelTypeEnum.Candle4H},
			{"candle6H", OkexChannelTypeEnum.Candle6H},
			{"candle12H", OkexChannelTypeEnum.Candle12H},
			{"candle1D", OkexChannelTypeEnum.Candle1D},
			{"candle2D", OkexChannelTypeEnum.Candle2D},
			{"candle3D", OkexChannelTypeEnum.Candle3D},
			{"candle5D", OkexChannelTypeEnum.Candle5D},
			{"candle1W", OkexChannelTypeEnum.Candle1W},
			{"candle1M", OkexChannelTypeEnum.Candle1M},
			{"candle3M", OkexChannelTypeEnum.Candle3M},
			{"candle6M", OkexChannelTypeEnum.Candle6M},
			{"candle1Y", OkexChannelTypeEnum.Candle1Y},

			#endregion

			{"books5", OkexChannelTypeEnum.OrderBook},
			{"tickers", OkexChannelTypeEnum.Ticker},
			{"mark-price", OkexChannelTypeEnum.MarkPrice},
			{"price-limit", OkexChannelTypeEnum.LimitPrice},
			{"funding-rate", OkexChannelTypeEnum.FundingRate}
		};

		#region Subscribe/Unsubscribe

		public void SubscribeToTickers(params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetTickerChannel(name));
			}

			SubscribeToChannels(okexChannels.ToArray());
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

			SubscribeToChannels(okexChannels.ToArray());
		}

		public void SubscribeToMarkPrice(string instrumentName)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				throw new ArgumentException("Instrument name must not be null or empty", instrumentName);

			var channel = GetMarkPriceChannel(instrumentName);
			СacheChannels(channel);
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

			SubscribeToChannels(okexChannels.ToArray());
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

			SubscribeToChannels(okexChannels.ToArray());
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

			SubscribeToChannels(okexChannels.ToArray());
		}

		public void SubscribeToCandleSticks(string timeFrame, params string[] instrumentNames)
		{
			var okexChannels = new List<OkexChannel>(instrumentNames.Length);
			foreach (var name in instrumentNames)
			{
				if (string.IsNullOrWhiteSpace(name))
					throw new ArgumentException("Instrument name must not be null or empty", name);

				okexChannels.Add(GetCandleSticksChannel(name, timeFrame));
			}

			SubscribeToChannels(okexChannels.ToArray());
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

		public void UnsubscribeToCandleStickChannel(string timeFrame, string instrumentName)
		{
			var candleStickChannel = GetCandleSticksChannel(instrumentName, timeFrame);
			UnsubscribeChannel(candleStickChannel);
		}

		#endregion

		#region ProcessMessage

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

		private void ProcessCandle(OkexSocketResponse response)
		{
			var data = response.Data?.FirstOrDefault();
			var candleStick = data?.ToObject<OkexCandleStickEntry>();
			var instrument = response.Argument["instId"]?.Value<string>();
			var timeFrame = response.Argument["channel"]?.Value<string>()?.Replace("candle", "");
			if (candleStick is null || string.IsNullOrWhiteSpace(instrument) || string.IsNullOrWhiteSpace(timeFrame))
			{
				return;
			}

			CandleUpdate.Invoke(new OkexCandleStick
			{
				Candle = candleStick,
				InstrumentName = instrument,
				TimeFrame = timeFrame,
			});
		}

		#endregion

		#region Generate channel strings

		private OkexChannel GetCandleSticksChannel(string instrumentName, string timeFrame)
		{
			var channelName = $"candle{timeFrame}{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", $"candle{timeFrame}" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetOrderBookChannel(string instrumentName, string orderBookType)
		{
			var channelName = $"{orderBookType}{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", orderBookType }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetTickerChannel(string instrumentName)
		{
			var channelName = $"tickers{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "tickers" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetMarkPriceChannel(string instrument)
		{
			var channelName = $"mark-price{instrument}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "mark-price" }, { "instId", instrument } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetLimitPriceChannel(string instrumentName)
		{
			var channelName = $"price-limit{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "price-limit" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetFundingRateChannel(string instrumentName)
		{
			var channelName = $"funding-rate{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "funding-rate" }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		#endregion
	}
}
