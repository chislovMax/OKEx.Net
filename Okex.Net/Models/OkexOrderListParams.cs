using Newtonsoft.Json;
using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexOrderListParams
	{
		[JsonProperty("instType")]
		public OkexInstrumentTypeEnum? InstrumentType { get; set; }
		[JsonProperty("uly")]
		public string Underlying { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("ordType")]
		public OkexOrderTypeEnum? OrderType { get; set; }
		[JsonProperty("state")]
		public OkexOrderStateEnum? State { get; set; }
		[JsonProperty("after")]
		public string AfterOrderId { get; set; }
		[JsonProperty("before")]
		public string BeforeOrderId { get; set; }
		[JsonProperty("limit")]
		public string Limit { get; set; }
	}
}
