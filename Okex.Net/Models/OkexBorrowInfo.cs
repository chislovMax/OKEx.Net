using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexBorrowInfo : AbstractOkexModel
	{
		[JsonProperty("ccy")]
		public string Currency { get; set; } = string.Empty;
		[JsonProperty("avgAmt")]
		public decimal AvgAmount { get; set; }
		[JsonProperty("avgAmtUsd")]
		public decimal AvgAmtUsd { get; set; }
		[JsonProperty("avgRate")]
		public decimal AvgRate { get; set; }
		[JsonProperty("preRate")]
		public decimal PreRate { get; set; }
		[JsonProperty("estRate")]
		public decimal EstRate { get; set; }
	}
}
