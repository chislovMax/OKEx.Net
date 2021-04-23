using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexCurrency
	{
		[JsonProperty("ccy")]
		public string ShortName { get; set; }
		[JsonProperty("chain")]
		public string Chain { get; set; }
		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("canDep")]
		public bool CanDeposit { get; set; }
		[JsonProperty("canWd")]
		public bool CanWithdraw { get; set; }
		[JsonProperty("canInternal")]
		public bool CanInternal { get; set; }
		[JsonProperty("minWd")]
		public decimal? MinWithdrawal { get; set; }
		[JsonProperty("maxFee")]
		public decimal? MinWithdrawalFee { get; set; }
		[JsonProperty("minFee")]
		public decimal? MaxWithdrawalFee { get; set; }
	}
}
