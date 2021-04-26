using Newtonsoft.Json;
using Okex.Net.V5.Enums;

namespace Okex.Net.V5.Models
{
	public class OkexOrderDetails
	{
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum OkexInstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("ccy")]
		public string Currency { get; set; }
		[JsonProperty("ordId")]
		public string OrderId { get; set; }
		[JsonProperty("clOrdId")]
		public string ClientSuppliedOrderId { get; set; }
		[JsonProperty("tag")]
		public string Tag { get; set; }
		[JsonProperty("px")]
		public decimal? Price { get; set; }
		[JsonProperty("sz")]
		public decimal Amount { get; set; }
		[JsonProperty("pnl")]
		public decimal? PnL { get; set; }
		[JsonProperty("ordType")]
		public OkexOrderTypeEnum OrderType { get; set; }
		[JsonProperty("side")]
		public OkexDirectionEnum Side { get; set; }
		[JsonProperty("posSide")]
		public OkexPositionSideEnum? PositionSide { get; set; }
		[JsonProperty("tdMode")]
		public TradeModeEnum TradeMode { get; set; }
		[JsonProperty("accFillSz")]
		public decimal? AccumulatedFillQuantity { get; set; }
		[JsonProperty("fillPx")]
		public decimal? LastFilledPrice { get; set; }
		[JsonProperty("tradeId")]
		public string LastTradeId { get; set; }
		[JsonProperty("fillSz")]
		public  decimal? LastFilledQuantity { get; set; }
		[JsonProperty("fillTime")]
		public string LastFilledTime { get; set; }
		[JsonProperty("avgPx")]
		public decimal? AveragePrice { get; set; }
		[JsonProperty("state")]
		public OkexOrderStateEnum State { get; set; }
		[JsonProperty("lever")]
		public decimal? Leverage { get; set; }
		[JsonProperty("tpTriggerPx")]
		public decimal? TakeProfitTriggerPrice { get; set; }
		[JsonProperty("tpOrdPx")]
		public decimal? TakeProfitOrderPrice { get; set; }
		[JsonProperty("slTriggerPx")]
		public decimal? StopLossTriggerPrice { get; set; }
		[JsonProperty("slOrdPx")]
		public decimal? StopLossOrderPrice { get; set; }
		[JsonProperty("feeCcy")]
		public string FeeCurrency { get; set; }
		[JsonProperty("fee")]
		public decimal? Fee { get; set; }
		[JsonProperty("rebateCcy")]
		public string RebateCurrency { get; set; }
		[JsonProperty("rebate")]
		public decimal? Rebate { get; set; }
		[JsonProperty("category")]
		public OkexOrderCategoryEnum Category { get; set; }
		[JsonProperty("uTime")]
		public string UpdateTime { get; set; }
		[JsonProperty("cTime")]
		public string CreationTime { get; set; }
	}
}
