using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexApiResponse <T>	where T : AbstractOkexModel 
	{
		[JsonProperty("code")]
		public string Code { get; set; }
		[JsonProperty("data")]
		public T[] Data { get; set; }
		[JsonProperty("msg")]
		public string Message { get; set; }
	}
}
