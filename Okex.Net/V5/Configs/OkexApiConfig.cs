namespace Okex.Net.V5.Configs
{
	public class OkexApiConfig
	{
		public bool IsTestNet { get; set; }
		public int SocketReconnectionTimeMs { get; set; } = 100;
		public string WSUrlPublic { get; set; } 
		public string WSUrlPrivate { get; set; }
		public string RestUrl { get; set; }
	}
}
