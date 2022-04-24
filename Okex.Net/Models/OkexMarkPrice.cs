using Newtonsoft.Json;
using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexMarkPrice
	{
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum InstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("markPx")]
		public decimal? Price { get; set; }
		[JsonProperty("ts")]
		public string Timestamp { get; set; }
	}
}
