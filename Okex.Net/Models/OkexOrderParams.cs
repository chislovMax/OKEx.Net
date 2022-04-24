using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexOrderParams
	{
		public string InstrumentName { get; set; }
		public OkexTradeModeEnum OkexTradeMode { get; set; }
		public OkexDirectionEnum Side { get; set; }
		public string Currency { get; set; }
		public OkexPositionSideEnum? PositionSide { get; set; }
		public string ClientSuppliedOrderId { get; set; }
		public string Tag { get; set; }
		public OkexOrderTypeEnum OrderType { get; set; }
		public decimal Amount { get; set; }
		public decimal? Price { get; set; }
		public bool? ReduceOnly { get; set; }
		public QuantityType? QuantityType { get; set; }
		public OkexInstrumentTypeEnum InstrumentType { get; set; }	
	}
}
