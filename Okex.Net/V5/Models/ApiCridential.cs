namespace Okex.Net.V5.Models
{
	public class OkexCredential
	{
		public OkexCredential(string apiKey, string secretKey, string password)
		{
			ApiKey = apiKey;
			SecretKey = secretKey;
			Password = password;	
		}

		public string ApiKey { get; set; }
		public string SecretKey { get; set; }
		public string Password { get; set; }
	}
}
