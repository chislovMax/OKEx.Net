using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexBorrowHistory : AbstractOkexModel
	{
		[JsonProperty("ccy")]
		public string Currency { get; set; } = string.Empty;
		[JsonProperty("amt")]
		public decimal Amount { get; set; }
		[JsonProperty("rate")]
		public decimal Rate { get; set; }
		[JsonProperty("ts")]
		public long Timestamp { get; set; }
	}
}
