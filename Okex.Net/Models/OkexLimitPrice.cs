using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexLimitPrice
	{
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("buyLmt")]
		public decimal? BuyLimitPrice { get; set; }
		[JsonProperty("sellLmt")]
		public decimal? SellLimitPrice { get; set; }
		[JsonProperty("ts")]
		public string Timestamp { get; set; }

	}
}
