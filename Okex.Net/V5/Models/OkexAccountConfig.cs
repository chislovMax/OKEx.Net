using Newtonsoft.Json;
using Okex.Net.V5.Enums;

namespace Okex.Net.V5.Models
{
	public class OkexAccountConfig
	{
		[JsonProperty("uid")]
		public string UId { get; set; }
		[JsonProperty("posMode")]
		public string PositionMode { get; set; }
		[JsonProperty("autoLoan")]
		public bool AutoLoan { get; set; }
		[JsonProperty("level")]
		public string Level { get; set; }
		[JsonProperty("greeksType")]
		public string GreeksType { get; set; }
		[JsonProperty("levelTmp")]
		public string LevelTemporary { get; set; }
		[JsonProperty("acctLv")]
		public OkexAccountLevelEnum OkexAccountLevel { get; set; }

	}
}
