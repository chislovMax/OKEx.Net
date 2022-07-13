namespace Okex.Net.Enums
{
	public enum OkexChannelTypeEnum	
	{
		OrderBook,
		Ticker,
		MarkPrice,
		Order,
		Account,
		LimitPrice,
		FundingRate,

		#region Candles

		Candle1m,
		Candle3m,
		Candle5m,
		Candle15m,
		Candle30m,
		Candle1H,
		Candle2H,
		Candle4H,
		Candle6H,
		Candle12H,
		Candle1D,
		Candle2D,
		Candle3D,
		Candle5D,
		Candle1W,
		Candle1M,
		Candle3M,
		Candle6M,
		Candle1Y,

		#endregion
	}
}
