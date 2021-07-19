using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexOrderInfo : AbstractOkexModel
	{
		[JsonProperty("ordId")]
		public string OrderId { get; set; }
		[JsonProperty("clOrdId")]
		public string ClientOrderId { get; set; }
		[JsonProperty("tag")]
		public string Tag { get; set; }
		[JsonProperty("sCode")]
		public override string Code { get; set; }
		[JsonProperty("sMsg")]
		public string Message { get; set; }
	}
}
