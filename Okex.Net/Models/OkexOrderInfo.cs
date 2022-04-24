using Newtonsoft.Json;

namespace Okex.Net.Models
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
		public override string Message { get; set; }
	}
}
