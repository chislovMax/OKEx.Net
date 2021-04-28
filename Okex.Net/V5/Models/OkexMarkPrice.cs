using Newtonsoft.Json;
using Okex.Net.V5.Enums;

namespace Okex.Net.V5.Models
{
	public class OkexMarkPrice
	{
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum InstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("markPx")]
		public decimal? MarkPrice { get; set; }
		[JsonProperty("ts")]
		public string TimeStamp { get; set; }
	}
}
