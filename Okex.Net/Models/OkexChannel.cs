namespace Okex.Net.Models
{
	public class OkexChannel
	{
		public OkexChannel(string name, object channelParams)
		{
			ChannelName = name;
			Params = channelParams;
		}
		public string ChannelName { get; set; }
		public object Params { get; set; }
	}
}
