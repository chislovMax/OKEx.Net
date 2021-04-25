using System.Collections.Generic;
using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexOrderBook
	{
		[JsonProperty("asks")]
		public ICollection<OkexOrderBookEntry> Asks { get; set; }
		[JsonProperty("bids")]
		public ICollection<OkexOrderBookEntry> Bids { get; set; }
		[JsonProperty("ts")]
		public string TimeStamp { get; set; }
	}
}
