using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexSocketRequest
	{
		public OkexSocketRequest(string option, params object[] args)
		{
			Arguments = args;
			Options = option;
		}

		[JsonProperty("op")]
		public string Options { get; set; }
		[JsonProperty("args")]
		public object[] Arguments { get; set; }
	}
}
