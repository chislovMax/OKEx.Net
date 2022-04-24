using Newtonsoft.Json;
using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexTicker : AbstractOkexModel
	{
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum OkexInstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("last")]
		public decimal? LastTradedPrice { get; set; }
		[JsonProperty("lastSz")]
		public decimal? LastTradedSize { get; set; }
		[JsonProperty("askPx")]
		public decimal? BestAskPrice { get; set; }
		[JsonProperty("askSz")]
		public decimal? BestAskSize{ get; set; }
		[JsonProperty("bidPx")]
		public decimal? BestBidPrice { get; set; }
		[JsonProperty("bidSz")]
		public decimal? BestBidSize { get; set; }
		[JsonProperty("open24h")]
		public decimal? OpenPrice { get; set; }
		[JsonProperty("high24h")]
		public decimal? HighestPrice { get; set; }
		[JsonProperty("low24h")]
		public decimal? LowestPrice { get; set; }
		[JsonProperty("volCcy24h")]
		public string TradingVolumeCurrency { get; set; }
		[JsonProperty("vol24h")]
		public string TradingVolume { get; set; }
		[JsonProperty("sodUtc0")]
		public string SodUtc0 { get; set; }
		[JsonProperty("sodUtc8")]
		public string SodUtc8 { get; set; }
		[JsonProperty("ts")]
		public string Timestamp { get; set; }
	}
}
