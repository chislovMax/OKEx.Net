using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexLoginRequest
	{
		public OkexLoginRequest(string apiKey, string password, string timestamp, string sign)
		{
			ApiKey = apiKey;
			Password = password;
			Timestamp = timestamp;
			Sign = sign;
		}

		[JsonProperty("apiKey")]
		public string ApiKey { get; set; }
		[JsonProperty("passphrase")]
		public string Password { get; set; }
		[JsonProperty("timestamp")]
		public string Timestamp { get; set; }
		[JsonProperty("sign")]
		public string Sign { get; set; }
	}
}
