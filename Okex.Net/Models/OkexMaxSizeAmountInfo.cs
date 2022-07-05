using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexMaxSizeAmountInfo : AbstractOkexModel
	{
		[JsonProperty("instId")]
		public string InstrumentId { get; set; }

		[JsonProperty("ccy")]
		public string Currency { get; set; }

		[JsonProperty("maxBuy")]
		public decimal MaxBuy { get; set; }

		[JsonProperty("maxSell")]
		public decimal MaxSell { get; set; }
	}
}
