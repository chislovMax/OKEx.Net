using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexSocketRequest
	{
		public OkexSocketRequest(string option, object[] args)
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
