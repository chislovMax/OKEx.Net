using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Okex.Net.Configs;
using Okex.Net.Enums;
using Okex.Net.Models;

namespace Okex.Net.Clients
{
	public class OkexSocketClientPrivate : OkexBaseSocketClient
	{
		public OkexSocketClientPrivate(ILogger logger, OkexCredential credential, OkexApiConfig clientConfig)
			: base(logger, clientConfig, clientConfig.WSUrlPrivate)
		{
			SetCredential(credential);

			AddChannelHandler(OkexChannelTypeEnum.Order, ProcessOrder);
			AddChannelHandler(OkexChannelTypeEnum.Account, ProcessAccount);
		}

		protected override Dictionary<string, OkexChannelTypeEnum> ChannelTypes { get; set; } = new Dictionary<string, OkexChannelTypeEnum>
		{
			{"orders", OkexChannelTypeEnum.Order},
			{"account", OkexChannelTypeEnum.Account}
		};

		public event Action<OkexOrderDetails> OrderUpdate = order => { };
		public event Action<OkexAccountDetails> AccountUpdate = order => { };

		#region Subscribe/unsubscribe

		public void SubscribeToChangeOrders(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			SubscribeToChannels(GetOrderChannel(instrumentType, underlying, instrumentName));
		}

		public void UnsubscribeChangeOrderChannel(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			var orderChannel = GetOrderChannel(instrumentType, underlying, instrumentName);
			UnsubscribeChannel(orderChannel);
		}

		public void SubscribeToChangeAccount(string currency = "")
		{
			SubscribeToChannels(GetAccountChannel(currency));
		}

		public void UnsubscribeToChangeAccountChannel(string currency = "")
		{
			var accountChannel = GetAccountChannel(currency);
			UnsubscribeChannel(accountChannel);
		}

		#endregion

		#region ProcessMessage

		private void ProcessOrder(OkexSocketResponse response)
		{
			var orders = response.Data?.ToObject<OkexOrderDetails[]>();
			if (orders is null || !orders.Any())
			{
				return;
			}

			foreach (var order in orders)
			{
				OrderUpdate.Invoke(order);
			}
		}

		private void ProcessAccount(OkexSocketResponse response)
		{
			var balances = response.Data?.ToObject<OkexAccountDetails[]>();
			if (balances is null || !balances.Any())
			{
				return;
			}

			foreach (var balance in balances)
			{
				AccountUpdate.Invoke(balance);
			}
		}

		#endregion

		#region Generate channel strings

		private OkexChannel GetOrderChannel(OrderInstrumentTypeEnum instrumentType, string underlying = "", string instrumentName = "")
		{
			var channelName = $"orders{instrumentType.ToString()}{underlying}{instrumentName}";
			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var channelArgs = new Dictionary<string, string> { { "channel", "orders" }, { "instType", instrumentType.ToString() } };
			if (!string.IsNullOrWhiteSpace(underlying)) channelArgs.Add("uly", underlying);
			if (!string.IsNullOrWhiteSpace(instrumentName)) channelArgs.Add("instId", instrumentName);

			return new OkexChannel(channelName, channelArgs);
		}

		private OkexChannel GetAccountChannel(string currency = "")
		{
			var channelName = $"account{currency}";

			if (SubscribedChannels.TryGetValue(channelName, out var channel))
			{
				return channel;
			}

			var args = new Dictionary<string, string> { { "channel", "account" } };
			if (!string.IsNullOrWhiteSpace(currency)) args.Add("ccy", currency);

			return new OkexChannel(channelName, args);
		}

		#endregion

	}
}
