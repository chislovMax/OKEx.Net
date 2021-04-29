namespace Okex.Net.V5.Configs
{
	public class SocketClientConfig
	{
		public bool IsTestNet { get; set; } = false;
		public int SocketReconnectionTimeMs { get; set; } = 100;
		public string Url { get; set; } = "wss://ws.okex.com:8443/ws/v5/public";
		public string DemoUrl { get; set; } = "wss://wspap.okex.com:8443/ws/v5/public";
	}
}
