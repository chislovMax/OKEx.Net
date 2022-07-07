using Newtonsoft.Json;
using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexTradeFee : AbstractOkexModel
	{
		[JsonProperty("category")]
		public OkexFeeCategoryEnum Category { get; set; }
		[JsonProperty("taker")]
		public decimal Taker { get; set; }
		[JsonProperty("maker")]
		public decimal Maker { get; set; }
		[JsonProperty("delivery")]
		public decimal? Delivery { get; set; }
		[JsonProperty("exercise")]
		public decimal? Exercise { get; set; }
		[JsonProperty("level")]
		public string Level { get; set; }
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum InstrumentType { get; set; }
		[JsonProperty("ts")]
		public decimal TimeStamp { get; set; }
	}
}
