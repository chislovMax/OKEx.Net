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
			AddChannelHandler(OkexChannelTypeEnum.OrderBook, ProcessOrderBook);
			AddChannelHandler(OkexChannelTypeEnum.Ticker, ProcessTicker);
			AddChannelHandler(OkexChannelTypeEnum.MarkPrice, ProcessMarkPrice);
			AddChannelHandler(OkexChannelTypeEnum.LimitPrice, ProcessLimitPrice);
			AddChannelHandler(OkexChannelTypeEnum.FundingRate, ProcessFundingRate);
		}

		public event Action<OkexOrderBook> BookPriceUpdate = bookPrice => { };
		public event Action<OkexTicker> TickerUpdate = ticker => { };
		public event Action<OkexMarkPrice> MarkPriceUpdate = markPrice => { };
		public event Action<OkexLimitPrice> LimitPriceUpdate = limitPrice => { };
		public event Action<OkexFundingRate> FundingRateUpdate = fundingRate => { };

		protected override Dictionary<string, OkexChannelTypeEnum> ChannelTypes { get; set; } = new Dictionary<string, OkexChannelTypeEnum>
		{
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

		#endregion

		#region Generate channel strings

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
			const string channelName = "funding-rate";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", channelName }, { "instId", instrumentName } };
			return new OkexChannel(channelName, channelArgs);
		}

		#endregion
	}
}
