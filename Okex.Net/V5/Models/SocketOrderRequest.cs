using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class SocketOrderRequest
	{
		public SocketOrderRequest(string channel, string instrumentType, string instrumentName)
		{
			Channel = channel;
			InstrumentType = instrumentType;
			InstrumentName = instrumentName;
		}

		[JsonProperty("channel")]
		public string Channel { get; set; }
		[JsonProperty("instType")]
		public string InstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
	}
}
