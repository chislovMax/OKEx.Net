using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexApiResponse <T>	where T : class 
	{
		[JsonProperty("code")]
		public string Code { get; set; }
		[JsonProperty("data")]
		public T[] Data { get; set; }
		[JsonProperty("msg")]
		public string Message { get; set; }
	}
}
