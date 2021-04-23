
using Newtonsoft.Json;

namespace Okex.Net.V5.Models
{
	public class OkexInstrument
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
		public string ContractValue { get; set; }
		[JsonProperty("ctMult")]
		public string ContractMultiplier { get; set; }
		[JsonProperty("ctValCcy")]
		public string ContractValueCurrency { get; set; }
		[JsonProperty("optType")]
		public string OptionType { get; set; }
		[JsonProperty("stk")]
		public string StrikePrice { get; set; }
		[JsonProperty("listTime")]
		public string ListingTime { get; set; }
		[JsonProperty("expTime")]
		public string ExpiryTime { get; set; }
		[JsonProperty("lever")]
		public string Leverage { get; set; }
		[JsonProperty("tickSz")]
		public string TickSize { get; set; }
		[JsonProperty("lotSz")]
		public string LotSize { get; set; }
		[JsonProperty("minSz")]
		public string MinimumOrderSize { get; set; }
		[JsonProperty("ctType")]
		public string ContractType { get; set; }
		[JsonProperty("alias")]
		public string Alias { get; set; }
		[JsonProperty("state")]
		public string State { get; set; }
	}
}
