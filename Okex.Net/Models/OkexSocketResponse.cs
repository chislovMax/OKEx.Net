using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Okex.Net.Models
{
	public class OkexSocketResponse
	{
		[JsonProperty("event")]
		public string Event { get; set; }
		[JsonProperty("code")]
		public string Code { get; set; }
		[JsonProperty("msg")]
		public string Message { get; set; }
		[JsonProperty("arg")]
		public JObject Argument { get; set; }
		[JsonProperty("data")]
		public JToken Data { get; set; }
		[JsonProperty("action")]
		public string Action { get; set; }
	}
}
