using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexOrderBook : AbstractOkexModel
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
