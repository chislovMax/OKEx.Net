using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexAccountDetails : AbstractOkexModel
	{
		[JsonProperty("uTime")]
		public string UpdateTime { get; set; }
		[JsonProperty("totalEq")]
		public decimal? TotalEquity { get; set; }
		[JsonProperty("isoEq")]
		public decimal? IsolatedEquity { get; set; }
		[JsonProperty("adjEq")]
		public decimal? AdjustedEquity { get; set; }
		[JsonProperty("ordFroz")]
		public decimal? MarginFrozen { get; set; }
		[JsonProperty("imr")]
		public decimal? InitialMargin { get; set; }
		[JsonProperty("mmr")]
		public decimal? MaintenanceMargin { get; set; }
		[JsonProperty("mgnRatio")]
		public decimal? MarginRatio { get; set; }
		[JsonProperty("notionalUsd")]
		public decimal? NotionalUsd { get; set; }
		[JsonProperty("details")]
		public OkexBalanceDetails[] Details { get; set; }
	}
}
