using System;
using CryptoExchange.Net.Converters;
using Newtonsoft.Json;
using Okex.Net.Converters.Core;

namespace Okex.Net.V5.Models
{
	[JsonConverter(typeof(ArrayConverter))]
	public class OkexOrderBookEntry
	{
		[ArrayProperty(0)]
		[JsonConverter(typeof(StringConverter))]
		public decimal Price { get; set; }

		/// <summary>
		/// The contract size at the price
		/// </summary>
		[ArrayProperty(1)]
		public decimal Quantity { get; set; }

		/// <summary>
		/// The number of the liquidated orders at the price
		/// </summary>
		[ArrayProperty(2)]
		public decimal LiquidatedOrdersCount { get; set; }

		/// <summary>
		/// The number of orders at the price
		/// </summary>
		[ArrayProperty(3)]
		public decimal OrdersCount { get; set; }
	}
}
