using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexBalanceDetails
	{
		[JsonProperty("ccy")]
		public string Currency { get; set; }
		[JsonProperty("eq")]
		public decimal? Equity { get; set; }
		[JsonProperty("cashBal")]
		public decimal? CashBalance { get; set; }
		[JsonProperty("uTime")]
		public string UpdateTime { get; set; }
		[JsonProperty("isoEq")]
		public decimal? IsolatedMarginEquity { get; set; }
		[JsonProperty("availEq")]
		public decimal? AvailableEquity { get; set; }
		[JsonProperty("disEq")]
		public decimal? DiscountEquity { get; set; }
		[JsonProperty("availBal")]
		public decimal? AvailableBalance { get; set; }
		[JsonProperty("frozenBal")]
		public decimal? FrozenBalanceCurrency { get; set; }
		[JsonProperty("ordFrozen")]
		public decimal? MarginFrozenOpenOrders { get; set; }
		[JsonProperty("liab")]
		public decimal? LiabilitiesCurrency { get; set; }
		[JsonProperty("upl")]
		public decimal? UnrealizedProfit { get; set; }
		[JsonProperty("uplLib")]
		public decimal? LiabilitiesDueToUnrealizedLoss { get; set; }
		[JsonProperty("crossLiab")]
		public decimal? CrossLiabilities { get; set; }
		[JsonProperty("isoLiab")]
		public decimal? IsolatedLiabilities { get; set; }
		[JsonProperty("mgnRatio")]
		public decimal? MarginRatio { get; set; }
		[JsonProperty("interest")]
		public decimal? Interest { get; set; }
		[JsonProperty("twap")]
		public string Twap { get; set; }

	}
}
