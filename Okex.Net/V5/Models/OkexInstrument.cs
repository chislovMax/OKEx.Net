
using Newtonsoft.Json;
using Okex.Net.V5.Enums;

namespace Okex.Net.V5.Models
{
	public class OkexInstrument : AbstractOkexModel
	{
		[JsonProperty("instType")]
		public string InstrumentType { get; set; }
		[JsonProperty("instId")]
		public string InstrumentName { get; set; }
		[JsonProperty("uly")]
		public string Underlying { get; set; }
		[JsonProperty("category")]
		public string Category { get; set; }
		[JsonProperty("baseCcy")]
		public string BaseCurrency { get; set; }
		[JsonProperty("quoteCcy")]
		public string QuoteCurrency { get; set; }
		[JsonProperty("settleCcy")]
		public string SettlementCurrency { get; set; }
		[JsonProperty("ctVal")]
		public decimal? ContractValue { get; set; }
		[JsonProperty("ctMult")]
		public decimal? ContractMultiplier { get; set; }
		[JsonProperty("ctValCcy")]
		public string ContractValueCurrency { get; set; }
		[JsonProperty("optType")]
		public OkexOptionTypeEnum? OptionType { get; set; }
		[JsonProperty("stk")]
		public decimal? StrikePrice { get; set; }
		[JsonProperty("listTime")]
		public long? ListingTime { get; set; }
		[JsonProperty("expTime")]
		public long? ExpiryTime { get; set; }
		[JsonProperty("lever")]
		public decimal? Leverage { get; set; }
		[JsonProperty("tickSz")]
		public decimal? TickSize { get; set; }
		[JsonProperty("lotSz")]
		public decimal? LotSize { get; set; }
		[JsonProperty("minSz")]
		public decimal? MinimumOrderSize { get; set; }
		[JsonProperty("ctType")]
		public string ContractType { get; set; }
		[JsonProperty("alias")]
		public string Alias { get; set; }
		[JsonProperty("state")]
		public string State { get; set; }
	}
}
