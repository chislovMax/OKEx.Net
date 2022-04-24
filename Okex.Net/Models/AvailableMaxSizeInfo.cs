using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class AvailableMaxSizeInfo : AbstractOkexModel
	{
		[JsonProperty("instId")]
		public string InstrumentId { get; set; }

		[JsonProperty("ccy")]
		public string Currency { get; set; }

		[JsonProperty("availBuy")]
		public decimal MaxBuy { get; set; }

		[JsonProperty("availSell")]
		public decimal MaxSell { get; set; }
	}
}
