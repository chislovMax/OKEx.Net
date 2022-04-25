using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexFundingRateHistory : AbstractOkexModel
	{
		[JsonProperty("instType")]
		public string InstrumentType { get; set; } = string.Empty;
		[JsonProperty("instId")]
		public string InstrumentName { get; set; } = string.Empty;
		[JsonProperty("fundingRate")]
		public decimal FundingRate { get; set; }
		[JsonProperty("realizedRate")]
		public decimal RealizedRate { get; set; }
		[JsonProperty("fundingTime")]
		public long FundingTime { get; set; }
	}
}
