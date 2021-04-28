using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OrderBookRequest
	{
		public OrderBookRequest(string channel, string instrumentName)
		{
			Channel = channel;
			InstrumentName = instrumentName;
		}

		[JsonProperty("channel")]
		public string Channel { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
	}
}
