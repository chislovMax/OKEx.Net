namespace Okex.Net.V5.Configs
{
	public class SocketClientConfig
	{
		public bool IsTestNet { get; set; } = true;
		public int SocketReconnectionTimeMs { get; set; } = 100;
		public string UrlPublic { get; set; } = "wss://ws.okex.com:8443/ws/v5/public";
		public string DemoUrlPublic { get; set; } = "wss://wspap.okex.com:8443/ws/v5/public";
		public string UrlPrivate { get; set; } = "wss://ws.okex.com:8443/ws/v5/private";
		public string DemoUrlPrivate { get; set; } = "wss://wspap.okex.com:8443/ws/v5/private";
	}
}
