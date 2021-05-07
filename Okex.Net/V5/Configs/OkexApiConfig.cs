namespace Okex.Net.V5.Configs
{
	public class OkexApiConfig
	{
		public bool IsTestNet { get; set; }
		public int SocketReconnectionTimeMs { get; set; } = 100;
		public string UrlPublic { get; set; } 
		public string UrlPrivate { get; set; }
	}
}
