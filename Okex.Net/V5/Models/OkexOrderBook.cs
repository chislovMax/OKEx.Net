using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexOrderBook
	{
		[JsonProperty("asks")]
		public OkexOrderBookEntry[] Asks { get; set; }
		[JsonProperty("bids")]
		public OkexOrderBookEntry[] Bids { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("ts")]
		public string TimeStamp { get; set; }
	}
}
