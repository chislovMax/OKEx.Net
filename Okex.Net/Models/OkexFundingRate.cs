using Newtonsoft.Json;

namespace Okex.Net.Models
{
	public class OkexFundingRate : AbstractOkexModel
	{
		[JsonProperty("instType")]
		public string InstType { get; set; } = string.Empty;
		[JsonProperty("instId")]
		public string InstId { get; set; } = string.Empty;
		[JsonProperty("fundingRate")]
		public decimal FundingRate { get; set; }
		[JsonProperty("nextFundingRate")]
		public decimal PredictedFundingRate { get; set; }
		[JsonProperty("fundingTime")]
		public long FundingTime { get; set; }
		[JsonProperty("nextFundingTime")]
		public long PredictedFundingTime { get; set; }
	}
}
