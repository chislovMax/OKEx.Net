using Okex.Net.V5.Enums;

namespace Okex.Net.V5.Models
{
	public class OkexOrderParams
	{
		public string InstrumentName { get; set; }
		public TradeModeEnum TradeMode { get; set; }
		public OkexDirectionEnum Side { get; set; }
		public string Currency { get; set; }
		public PositionSideEnum? PositionSide { get; set; }
		public string ClientSuppliedOrderId { get; set; }
		public string Tag { get; set; }
		public OkexOrderTypeEnum OrderType { get; set; }
		public decimal Amount { get; set; }
		public decimal? Price { get; set; }
		public bool? ReduceOnly { get; set; }
	}
}
