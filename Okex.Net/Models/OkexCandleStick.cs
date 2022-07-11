using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexCandleStick
	{
		[JsonProperty("candle")] 
		public OkexCandleStickEntry Candle { get; set; } = new OkexCandleStickEntry();
		[JsonProperty("instId")]
		public string InstrumentName { get; set; } = string.Empty;
		[JsonProperty("timeFrame")]
		public string TimeFrame { get; set; } = string.Empty;
	}
}