using CryptoExchange.Net.Converters;
using Newtonsoft.Json;

namespace Okex.Net.Models
{
	[JsonConverter(typeof(ArrayConverter))]
	public class OkexCandleStickEntry : AbstractOkexModel
	{
		[ArrayProperty(0)]
		public long Timestamp { get; set; }
		[ArrayProperty(1)]
		public decimal Open { get; set; }
		[ArrayProperty(2)]
		public decimal High { get; set; }
		[ArrayProperty(3)]
		public decimal Low { get; set; }
		[ArrayProperty(4)]
		public decimal Close { get; set; }
		[ArrayProperty(5)]
		public decimal Volume { get; set; }
		[ArrayProperty(6)]
		public decimal VolumeCurrency { get; set; }
	}
}