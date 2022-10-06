namespace Okex.Net.Configs
{
	public class OkexApiConfig
	{
		public bool IsSsl { get; set; }
		public bool IsTestNet { get; set; }
		public int SocketReconnectionTimeMs { get; set; } = 100;
		public string WSUrlPublic { get; set; } 
		public string WSUrlPrivate { get; set; }
		public string RestUrl { get; set; }
	}
}
