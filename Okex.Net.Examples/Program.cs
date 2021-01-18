﻿using CryptoExchange.Net.Sockets;
using Okex.Net.Enums;
using Okex.Net.RestObjects;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Okex.Net.Examples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Okex Rest Api Client
            OkexClient api = new OkexClient();
            api.SetApiCredentials("XXXXXXXX-API-KEY-XXXXXXXX", "XXXXXXXX-API-SECRET-XXXXXXXX", "XXXXXXXX-API-PASSPHRASE-XXXXXXXX");

            /* System: Public Endpoints */
            var system_public_01 = api.SystemTime();
            var system_public_02 = api.SystemStatus();

            /* Funding: Private Endpoints */
            var funding_public_01 = api.Funding_GetAllBalances();
            var funding_public_02 = api.Funding_GetSubAccount("subaccountname");
            var funding_public_03 = api.Funding_GetAssetValuation(OkexFundingAccountType.TotalAccountAssets, "USD");
            var funding_public_04 = api.Funding_GetCurrencyBalance("BTC");
            var funding_public_05 = api.Funding_Transfer("ETH", 0.1m, OkexFundingTransferAccountType.FundingAccount, OkexFundingTransferAccountType.Spot);
            var funding_public_06 = api.Funding_Withdrawal("ETH", 1.1m, OkexFundingWithdrawalDestination.Others, "0x65b02db9b67b73f5f1e983ae10796f91ded57b64", "--fundpassword--", 0.01m);
            var funding_public_07 = api.Funding_GetWithdrawalHistory();
            var funding_public_08 = api.Funding_GetDepositHistoryByCurrency("BTC");
            var funding_public_09 = api.Funding_GetBills();
            var funding_public_10 = api.Funding_GetDepositAddress("BTC");
            var funding_public_11 = api.Funding_GetDepositHistory();
            var funding_public_12 = api.Funding_GetWithdrawalHistoryByCurrency("BTC");
            var funding_public_13 = api.Funding_GetAllCurrencies();
            var funding_public_14 = api.Funding_GetUserID();
            var funding_public_15 = api.Funding_GetWithdrawalFees();
            var funding_public_16 = api.Funding_GetWithdrawalFees("ETH");
            var funding_public_17 = api.Funding_PiggyBank("ETH", 0.1m, OkexFundingPiggyBankActionSide.Purchase);
            var funding_public_18 = api.Funding_PiggyBank("ETH", 0.1m, OkexFundingPiggyBankActionSide.Redempt);

            /* Spot: Public Endpoints */
            var spot_public_01 = api.Spot_GetTradingPairs();
            var spot_public_02 = api.Spot_GetOrderBook("BTC-USDT");
            var spot_public_03 = api.Spot_GetAllTickers();
            var spot_public_04 = api.Spot_GetSymbolTicker("BTC-USDT");
            var spot_public_05 = api.Spot_GetTrades("BTC-USDT");
            var spot_public_06 = api.Spot_GetCandles("BTC-USDT", OkexSpotPeriod.OneHour);
            var spot_public_07 = api.Spot_GetHistoricalCandles("BTC-USDT", OkexSpotPeriod.OneHour);

            /* Spot: Private Endpoints */
            var spot_place_order_01 = new OkexSpotPlaceOrder
            {
                Symbol = "ETH-BTC",
                ClientOrderId = "ClientOrderId",
                Type = OkexSpotOrderType.Limit,
                Side = OkexSpotOrderSide.Sell,
                TimeInForce = OkexSpotTimeInForce.NormalOrder,
                Price = "0.1",
                Size = "0.1",
            };
            var spot_place_order_02 = new OkexSpotPlaceOrder
            {
                Symbol = "ETH-BTC",
                ClientOrderId = "ClientOrderIx",
                Type = OkexSpotOrderType.Limit,
                Side = OkexSpotOrderSide.Sell,
                TimeInForce = OkexSpotTimeInForce.NormalOrder,
                Price = "0.2",
                Size = "0.2",
            };
            var spot_place_orders = new List<OkexSpotPlaceOrder>();
            spot_place_orders.Add(spot_place_order_01);
            spot_place_orders.Add(spot_place_order_02);

            var spot_cancel_order_01 = new OkexSpotCancelOrder
            {
                Symbol = "ETH-BTC",
                OrderIds = new List<string> { "1001", "1002", "1003", "1004", "1005" },
                ClientOrderIds = new List<string>()
            };
            var spot_cancel_order_02 = new OkexSpotCancelOrder
            {
                Symbol = "ETH-BTC",
                OrderIds = new List<string> { },
                ClientOrderIds = new List<string> { "coid001", "coid002", "coid003", "coid004", "coid005" }
            };
            var spot_cancel_orders = new List<OkexSpotCancelOrder>();
            spot_cancel_orders.Add(spot_cancel_order_01);
            spot_cancel_orders.Add(spot_cancel_order_02);

            var spot_private_01 = api.Spot_GetAllBalances();
            var spot_private_02 = api.Spot_GetSymbolBalance("BTC");
            var spot_private_03 = api.Spot_GetSymbolBalance("ETH");
            var spot_private_04 = api.Spot_GetSymbolBalance("eth");
            var spot_private_05 = api.Spot_GetSymbolBills("ETH");
            var spot_private_06 = api.Spot_PlaceOrder(spot_place_order_01);
            var spot_private_07 = api.Spot_PlaceOrder(spot_place_order_02);
            var spot_private_08 = api.Spot_PlaceOrder("ETH-BTC", OkexSpotOrderSide.Sell, OkexSpotOrderType.Limit, OkexSpotTimeInForce.NormalOrder, price: 0.1m, size: 0.11m);
            var spot_private_09 = api.Spot_PlaceOrder("ETH-BTC", OkexSpotOrderSide.Sell, OkexSpotOrderType.Limit, OkexSpotTimeInForce.NormalOrder, price: 0.1m, size: 0.11m, clientOrderId: "ClientOrderId");
            var spot_private_10 = api.Spot_BatchPlaceOrders(spot_place_orders);
            var spot_private_11 = api.Spot_CancelOrder("ETH-BTC", 4275473321519104);
            var spot_private_12 = api.Spot_CancelOrder("ETH-BTC", clientOrderId: "clientorderid"); // It works: Case Insensitive
            var spot_private_13 = api.Spot_CancelOrder("ETH-BTC", clientOrderId: "CLIENTORDERID"); // It works: Case Insensitive 
            var spot_private_14 = api.Spot_BatchCancelOrders(spot_cancel_orders);
            var spot_private_15 = api.Spot_ModifyOrder("ETH-BTC", orderId: 1001, newSize: 0.1m);
            var spot_private_16 = api.Spot_BatchModifyOrders(new List<OkexSpotModifyOrder> { });
            var spot_private_17 = api.Spot_GetAllOrders("ETH-BTC", OkexSpotOrderState.Canceled);
            var spot_private_18 = api.Spot_GetAllOrders("ETH-BTC", OkexSpotOrderState.Complete, 2, after: 1);
            var spot_private_19 = api.Spot_GetOpenOrders("ETH-BTC");
            var spot_private_20 = api.Spot_GetOrderDetails("ETH-BTC", clientOrderId: "clientorderid");
            var spot_private_21 = api.Spot_TradeFeeRates();
            var spot_private_22 = api.Spot_GetTransactionDetails("ETH-BTC");
            var spot_private_23 = api.Spot_AlgoPlaceOrder("ETH-BTC", OkexAlgoOrderType.TriggerOrder, OkexMarket.Spot, OkexSpotOrderSide.Buy, size: 0.1m, trigger_price: 0.0101m, trigger_algo_price: 0.0100m, trigger_algo_type: OkexAlgoPriceType.Limit);
            var spot_private_24 = api.Spot_AlgoCancelOrder("ETH-BTC", OkexAlgoOrderType.TriggerOrder, new List<string> { "1001" });
            var spot_private_25 = api.Spot_AlgoGetOrders("ETH-BTC", OkexAlgoOrderType.TriggerOrder);

            /* Margin: Public Endpoints */
            var margin_public_01 = api.Margin_GetMarkPrice("BTC-USDT");

            /* Margin: Private Endpoints */
            var margin_private_01 = api.Margin_GetAllBalances();
            var margin_private_02 = api.Margin_GetSymbolBalance("BTC-USDT");
            var margin_private_03 = api.Margin_GetSymbolBills("BTC-USDT");
            var margin_private_04 = api.Margin_GetAccountSettings();
            var margin_private_05 = api.Margin_GetAccountSettings("BTC-USDT");
            var margin_private_06 = api.Margin_GetLoanHistory(OkexMarginLoanStatus.Outstanding);
            var margin_private_07 = api.Margin_GetLoanHistory("BTC-USDT", OkexMarginLoanState.Complete);
            var margin_private_08 = api.Margin_Loan("BTC-USDT", "BTC", 0.1m);
            var margin_private_09 = api.Margin_Repayment("BTC-USDT", "BTC", 0.1m);
            var margin_private_10 = api.Margin_PlaceOrder("BTC-USDT", OkexSpotOrderSide.Buy, OkexSpotOrderType.Limit);
            var margin_private_11 = api.Margin_BatchPlaceOrders(new List<OkexSpotPlaceOrder> { });
            var margin_private_12 = api.Margin_CancelOrder("BTC-USDT", 1001);
            var margin_private_13 = api.Margin_BatchCancelOrders(new List<OkexSpotCancelOrder> { });
            var margin_private_14 = api.Margin_GetAllOrders("BTC-USDT", OkexSpotOrderState.Complete);
            var margin_private_15 = api.Margin_GetLeverage("BTC-USDT");
            var margin_private_16 = api.Margin_SetLeverage("BTC-USDT", 10);
            var margin_private_17 = api.Margin_GetOrderDetails("BTC-USDT", 1001);
            var margin_private_18 = api.Margin_GetOpenOrders("BTC-USDT");
            var margin_private_19 = api.Margin_GetTransactionDetails("BTC-USDT", 1001);
            var margin_private_20 = api.Margin_AlgoPlaceOrder("BTC-USDT", OkexAlgoOrderType.TriggerOrder, OkexMarket.Margin, OkexSpotOrderSide.Buy, size: 0.1m, trigger_price: 0.0101m, trigger_algo_price: 0.0100m, trigger_algo_type: OkexAlgoPriceType.Limit);
            var margin_private_21 = api.Margin_AlgoCancelOrder("BTC-USDT", OkexAlgoOrderType.TriggerOrder, new List<string> { "1001" });
            var margin_private_22 = api.Margin_AlgoGetOrders("BTC-USDT", OkexAlgoOrderType.TriggerOrder);

            /* Futures: Public Endpoints */
            var futures_public_01 = api.Futures_GetTradingContracts();
            var futures_public_02 = api.Futures_GetOrderBook("BTC-USDT-201225", 20);
            var futures_public_03 = api.Futures_GetAllTickers();
            var futures_public_04 = api.Futures_GetSymbolTicker("BTC-USDT-201225");
            var futures_public_05 = api.Futures_GetTrades("BTC-USDT-201225");
            var futures_public_06 = api.Futures_GetCandles("BTC-USDT-201225", OkexSpotPeriod.OneHour);
            var futures_public_07 = api.Futures_GetIndices("BTC-USDT-201225");
            var futures_public_08 = api.Futures_GetFiatExchangeRates();
            var futures_public_09 = api.Futures_GetEstimatedPrice("BTC-USDT-201225");
            var futures_public_10 = api.Futures_GetOpenInterests("BTC-USDT-201225");
            var futures_public_11 = api.Futures_GetPriceLimit("BTC-USDT-201225");
            var futures_public_12 = api.Futures_GetMarkPrice("BTC-USDT-201225");
            var futures_public_13 = api.Futures_GetLiquidatedOrders("BTC-USDT-201225", OkexFuturesLiquidationStatus.FilledOrdersInTheRecent7Days);
            var futures_public_14 = api.Futures_GetSettlementHistory("BTC-USDT-201225");
            var futures_public_15 = api.Futures_GetHistoricalMarketData("BTC-USDT-201225", OkexSpotPeriod.OneHour);

            /* Futures: Private Endpoints */
            var futures_private_01 = api.Futures_GetPositions();
            var futures_private_02 = api.Futures_GetPositions("BTC-USDT-201225");
            var futures_private_03 = api.Futures_GetBalances();
            var futures_private_04 = api.Futures_GetBalances("BTC-USDT");
            var futures_private_05 = api.Futures_GetLeverage("BTC-USDT");
            var futures_private_06 = api.Futures_SetLeverage(OkexFuturesMarginMode.Crossed, "BTC-USDT", 10);
            var futures_private_07 = api.Futures_GetSymbolBills("ETH-USDT");
            var futures_private_08 = api.Futures_PlaceOrder("ETH-USDT", OkexFuturesOrderType.OpenLong, 0.1m, OkexFuturesTimeInForce.NormalOrder);
            var futures_private_09 = api.Futures_BatchPlaceOrders("ETH-USDT", new List<OkexFuturesPlaceOrder> { });
            var futures_private_10 = api.Futures_ModifyOrder("ETH-USDT", orderId: 1001, newSize: 0.1m);
            var futures_private_11 = api.Futures_BatchModifyOrders("ETH-USDT", new List<OkexFuturesModifyOrder> { });
            var futures_private_12 = api.Futures_CancelOrder("ETH-USDT", 1001);
            var futures_private_13 = api.Futures_BatchCancelOrders("ETH-USDT", new List<string> { }, new List<string> { });
            var futures_private_14 = api.Futures_GetAllOrders("ETH-USDT", OkexFuturesOrderState.Complete, 2, after: 1);
            var futures_private_15 = api.Futures_GetOrderDetails("ETH-USDT", clientOrderId: "clientorderid");
            var futures_private_16 = api.Futures_GetTransactionDetails("ETH-USDT", orderId: 1001);
            var futures_private_17 = api.Futures_SetAccountMode("ETH-USDT", OkexFuturesMarginMode.Crossed);
            var futures_private_18 = api.Futures_GetTradeFeeRates("ETH-USDT");
            var futures_private_19 = api.Futures_MarketCloseAll("ETH-USDT", OkexFuturesDirection.Long);
            var futures_private_20 = api.Futures_CancelAll("ETH-USDT", OkexFuturesDirection.Long);
            var futures_private_21 = api.Futures_GetHoldAmount("ETH-USDT");
            var futures_private_22 = api.Futures_AlgoPlaceOrder("BTC-USDT", OkexFuturesOrderType.OpenLong, OkexAlgoOrderType.TriggerOrder, size: 0.1m, trigger_price: 0.0101m, trigger_algo_price: 0.0100m, trigger_algo_type: OkexAlgoPriceType.Limit);
            var futures_private_23 = api.Margin_AlgoCancelOrder("BTC-USDT", OkexAlgoOrderType.TriggerOrder, new List<string> { "1001" });
            var futures_private_24 = api.Margin_AlgoGetOrders("BTC-USDT", OkexAlgoOrderType.TriggerOrder);

            /* Perpetual Swap: Public Endpoints */
            var swap_public_01 = api.Swap_GetTradingContracts();
            var swap_public_02 = api.Swap_GetOrderBook("BTC-USDT-SWAP");
            var swap_public_03 = api.Swap_GetAllTickers();
            var swap_public_04 = api.Swap_GetSymbolTicker("BTC-USDT-SWAP");
            var swap_public_05 = api.Swap_GetTrades("BTC-USDT-SWAP");
            var swap_public_06 = api.Swap_GetCandles("BTC-USDT-SWAP", OkexSpotPeriod.OneHour);
            var swap_public_07 = api.Swap_GetIndices("BTC-USDT-SWAP");
            var swap_public_08 = api.Swap_GetFiatExchangeRates();
            var swap_public_09 = api.Swap_GetOpenInterests("BTC-USDT-SWAP");
            var swap_public_10 = api.Swap_GetPriceLimit("BTC-USDT-SWAP");
            var swap_public_11 = api.Swap_GetLiquidatedOrders("BTC-USDT-SWAP", OkexSwapLiquidationStatus.FilledOrdersInTheRecent7Days);
            var swap_public_12 = api.Swap_GetNextSettlementTime("BTC-USDT-SWAP");
            var swap_public_13 = api.Swap_GetMarkPrice("BTC-USDT-SWAP");
            var swap_public_14 = api.Swap_GetFundingRateHistory("BTC-USDT-SWAP");
            var swap_public_15 = api.Swap_GetHistoricalMarketData("BTC-USDT-SWAP", OkexSpotPeriod.OneHour);

            /* Perpetual Swap: Private Endpoints */
            var swap_private_01 = api.Swap_GetPositions();
            var swap_private_02 = api.Swap_GetPositions("BTC-USDT-SWAP");
            var swap_private_03 = api.Swap_GetBalances();
            var swap_private_04 = api.Swap_GetBalances("BTC-USDT-SWAP");
            var swap_private_05 = api.Swap_GetLeverage("BTC-USDT-SWAP");
            var swap_private_06 = api.Swap_SetLeverage("BTC-USDT-SWAP", OkexSwapLeverageSide.CrossedMargin, 17);
            var swap_private_07 = api.Swap_GetBills("BTC-USDT-SWAP");
            var swap_private_08 = api.Swap_PlaceOrder("BTC-USDT-SWAP", OkexSwapOrderType.OpenLong, 0.1m);
            var swap_private_09 = api.Swap_BatchPlaceOrders("BTC-USDT-SWAP", new List<OkexSwapPlaceOrder> { });
            var swap_private_10 = api.Swap_CancelOrder("BTC-USDT-SWAP", orderId: 1001);
            var swap_private_11 = api.Swap_BatchCancelOrders("BTC-USDT-SWAP", new List<string> { }, new List<string> { });
            var swap_private_12 = api.Swap_ModifyOrder("BTC-USDT-SWAP", orderId: 1001, newSize: 0.1m);
            var swap_private_13 = api.Swap_BatchModifyOrders("BTC-USDT-SWAP", new List<OkexSwapModifyOrder> { });
            var swap_private_14 = api.Swap_GetAllOrders("BTC-USDT-SWAP", OkexSwapOrderState.Complete);
            var swap_private_15 = api.Swap_GetOrderDetails("BTC-USDT-SWAP", clientOrderId: "clientorderid");
            var swap_private_16 = api.Swap_GetTransactionDetails("BTC-USDT-SWAP", orderId: 1001);
            var swap_private_17 = api.Swap_GetHoldAmount("BTC-USDT-SWAP");
            var swap_private_18 = api.Swap_GetTradeFeeRates("BTC-USDT-SWAP");
            var swap_private_19 = api.Swap_MarketCloseAll("BTC-USDT-SWAP", OkexSwapDirection.Long);
            var swap_private_20 = api.Swap_CancelAll("BTC-USDT-SWAP", OkexSwapDirection.Long);
            var swap_private_21 = api.Swap_AlgoPlaceOrder("BTC-USDT", OkexSwapOrderType.OpenLong, OkexAlgoOrderType.TriggerOrder, size: 0.1m, trigger_price: 0.0101m, trigger_algo_price: 0.0100m, trigger_algo_type: OkexAlgoPriceType.Limit);
            var swap_private_22 = api.Swap_AlgoCancelOrder("BTC-USDT", OkexAlgoOrderType.TriggerOrder, new List<string> { "1001" });
            var swap_private_23 = api.Swap_AlgoGetOrders("BTC-USDT", OkexAlgoOrderType.TriggerOrder);

            /* Contract: Public Endpoints */
            var contract_public_01 = api.Contract_GetLongShortRatio("BTC", OkexContractPeriod.FiveMinutes);
            var contract_public_02 = api.Contract_GetVolume("BTC", OkexContractPeriod.FiveMinutes);
            var contract_public_03 = api.Contract_GetTakerVolume("BTC", OkexContractPeriod.FiveMinutes);
            var contract_public_04 = api.Contract_GetSentiment("BTC", OkexContractPeriod.FiveMinutes);
            var contract_public_05 = api.Contract_GetMargin("BTC", OkexContractPeriod.FiveMinutes);

            /* Options: Public Endpoints */
            var options_public_01 = api.Options_GetUnderlyingList();
            var options_public_02 = api.Options_GetInstruments("BTC-USD");
            var options_public_03 = api.Options_GetMarketData("BTC-USD");
            var options_public_04 = api.Options_GetMarketData("BTC-USD", "BTC-USD-201218-16250-C");
            var options_public_05 = api.Options_GetOrderBook("BTC-USD-201218-16250-C");
            var options_public_06 = api.Options_GetTrades("BTC-USD-201218-16250-C");
            var options_public_07 = api.Options_GetTicker("BTC-USD-201218-16250-P");
            var options_public_08 = api.Options_GetCandles("BTC-USD-201218-16250-P", OkexSpotPeriod.OneHour);
            var options_public_09 = api.Options_GetSettlementHistory("BTC-USD");

            /* Options: Private Endpoints */
            var options_private_01 = api.Options_GetPositions("BTC-USD");
            var options_private_02 = api.Options_GetBalances("BTC-USD");
            var options_private_03 = api.Options_PlaceOrder("BTC-USD-201218-16250-C", OkexOptionsOrderSide.Buy, 20000m, 0.1m, OkexOptionsTimeInForce.NormalOrder);
            var options_private_04 = api.Options_BatchPlaceOrders("BTC-USD", new List<OkexOptionsPlaceOrder> { });
            var options_private_05 = api.Options_CancelOrder("BTC-USD", 1001);
            var options_private_06 = api.Options_BatchCancelOrders("BTC-USD", new List<string> { }, new List<string> { });
            var options_private_07 = api.Options_CancelAllOrders("BTC-USD");
            var options_private_08 = api.Options_ModifyOrder("BTC-USD", orderId: 1001, newSize: 0.1m);
            var options_private_09 = api.Options_BatchModifyOrders("BTC-USD", new List<OkexOptionsModifyOrder> { });
            var options_private_10 = api.Options_GetOrderDetails("BTC-USD", 1001);
            var options_private_11 = api.Options_GetAllOrders("BTC-USD", OkexOptionsOrderState.Complete);
            var options_private_12 = api.Options_GetTransactionDetails("BTC-USD");
            var options_private_13 = api.Options_GetBills("BTC-USD");
            var options_private_14 = api.Options_GetTradeFeeRates("BTC-USD");

            // Console.ReadLine();
            // return;

            /* Sample Pairs */
            var spot_pairs = new List<string> { "BTC-USDT", "LTC-USDT", "ETH-USDT", "XRP-USDT", "BCH-USDT", "EOS-USDT", "OKB-USDT", "ETC-USDT", "TRX-USDT", "BSV-USDT", "DASH-USDT", "NEO-USDT", "QTUM-USDT", "XLM-USDT", "ADA-USDT", "AE-USDT", "BLOC-USDT", "EGT-USDT", "IOTA-USDT", "SC-USDT", "WXT-USDT", "ZEC-USDT", };
            var futures_pairs = new List<string> { "BTC-USD-210625", "BTC-USD-210326", "BTC-USD-210101", "BTC-USD-201225", "LTC-USD-210625", "LTC-USD-210326", "LTC-USD-210101", "LTC-USD-201225", "ETH-USD-210326", "ETH-USD-210101", "ETH-USD-210625", "ETH-USD-201225", };
            var swap_pairs = new List<string> { "BTC-USD-SWAP", "LTC-USD-SWAP", "ETH-USD-SWAP", };

            /* Okex Socket Client Object */
            var ws = new OkexSocketClient();

            /* WS Subscriptions */
            var subs = new List<UpdateSubscription>();

            /* 00. Core - Public */
            ws.SetApiCredentials("XXXXXXXX-API-KEY-XXXXXXXX", "XXXXXXXX-API-SECRET-XXXXXXXX", "XXXXXXXX-API-PASSPHRASE-XXXXXXXX"); // OR
            var ws_core_public_01 = ws.Auth_Login("XXXXXXXX-API-KEY-XXXXXXXX", "XXXXXXXX-API-SECRET-XXXXXXXX", "XXXXXXXX-API-PASSPHRASE-XXXXXXXX");

            /* 01. System - Public */
            var ws_system_public_01 = ws.Ping();

            /* 03. Spot - Public */

            // Ticker
            foreach (var pair in spot_pairs)
            {
                var subscription = ws.Spot_SubscribeToTicker(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Ticker >> {data.Symbol} >> LP:{data.LastPrice} LQ:{data.LastQuantity} Bid:{data.BestBidPrice} BS:{data.BestBidSize} Ask:{data.BestAskPrice} AS:{data.BestAskSize} 24O:{data.Open24H} 24H:{data.High24H} 24L:{data.Low24H} 24BV:{data.BaseVolume24H} 24QV:{data.QuoteVolume24H} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Candlesticks
            foreach (var pair in spot_pairs)
            {
                var subscription = ws.Spot_SubscribeToCandlesticks(pair, OkexSpotPeriod.FiveMinutes, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Candle >> {data.Symbol} >> ST:{data.StartTime} O:{data.Open} H:{data.High} L:{data.Low} C:{data.Close} V:{data.Volume}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Trades
            foreach (var pair in spot_pairs)
            {
                var subscription = ws.Spot_SubscribeToTrades(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Trades >> {data.Symbol} >> ID:{data.TradeId} P:{data.Price} A:{data.Size} S:{data.Side} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Order Book
            foreach (var pair in spot_pairs)
            {
                var subscription = ws.Spot_SubscribeToOrderBook(pair, OkexOrderBookDepth.Depth400, (data) =>
                {
                    if (data != null && data.Asks != null && data.Asks.Count() > 0 && data.Bids != null && data.Bids.Count() > 0)
                    {
                        Console.WriteLine($"Depth >> {data.Symbol} >> Ask P:{data.Asks.First().Price} Q:{data.Asks.First().Quantity} C:{data.Asks.First().OrdersCount} Bid P:{data.Bids.First().Price} Q:{data.Bids.First().Quantity} C:{data.Bids.First().OrdersCount} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Unsubscribe
            foreach (var sub in subs)
            {
                _ = ws.Unsubscribe(sub);
            }

            /* 03. Spot - Private */

            // Balance
            ws.Spot_SubscribeToBalance("ETH", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Balance Update >> {data.Currency} >> Balance:{data.Balance} Available:{data.Available} Frozen:{data.Frozen}");
                }
            });

            // Orders
            ws.Spot_SubscribeToOrders("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.State}");
                }
            });

            // Algo Orders
            ws.Spot_SubscribeToAlgoOrders("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.Status}");
                }
            });

            /* 04. Margin - Private */

            // Balance
            ws.Margin_SubscribeToBalance("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Balance Update >> {data.ProductId} >> MarginRatio:{data.MarginRatio} Liq:{data.LiquidationPrice}");
                }
            });

            /* 05. Futures - Public */

            // Contracts
            ws.Futures_SubscribeToContracts((data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Contract >> {data.Symbol} >> BC:{data.BaseCurrency} QC:{data.QuoteCurrency}");
                }
            });

            // Ticker
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToTicker(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Ticker >> {data.Symbol} >> LP:{data.LastPrice} LQ:{data.LastQuantity} Bid:{data.BestBidPrice} BS:{data.BestBidSize} Ask:{data.BestAskPrice} AS:{data.BestAskSize} 24H:{data.High24H} 24L:{data.Low24H} 24BV:{data.BaseVolume24H} 24QV:{data.QuoteVolume24H} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Candlesticks
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToCandlesticks(pair, OkexSpotPeriod.FiveMinutes, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Candle >> {data.Symbol} >> ST:{data.StartTime} O:{data.Open} H:{data.High} L:{data.Low} C:{data.Close} V:{data.BaseVolume}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Trades
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToTrades(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Trades >> {pair} >> ID:{data.TradeId} P:{data.Price} Q:{data.Quantity} S:{data.Side} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Price Range
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToPriceRange(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Price Range >> {pair} >> H:{data.Highest} L:{data.Lowest} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Estimated Price
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToEstimatedPrice(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Estimated Price >> {pair} >> R:{data.Rate} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Order Book
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToOrderBook(pair, OkexOrderBookDepth.Depth400, (data) =>
                {
                    if (data != null && data.Asks != null && data.Asks.Count() > 0 && data.Bids != null && data.Bids.Count() > 0)
                    {
                        Console.WriteLine($"Depth >> {data.Symbol} >> Ask P:{data.Asks.First().Price} Q:{data.Asks.First().Quantity} C:{data.Asks.First().OrdersCount} Bid P:{data.Bids.First().Price} Q:{data.Bids.First().Quantity} C:{data.Bids.First().OrdersCount} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Mark Price
            foreach (var pair in futures_pairs)
            {
                var subscription = ws.Futures_SubscribeToMarkPrice(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Trades >> {pair} >> P:{data.MarkPrice} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            /* 05. Futures - Private */

            // Positions
            ws.Futures_SubscribeToPositions("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Positions Update >> {data.Symbol} >> RPNL:{data.RealisedPnl} LUPNL:{data.LongUnrealisedPnl} SUPNL:{data.ShortUnrealisedPnl}");
                }
            });

            // Balance
            ws.Futures_SubscribeToBalance("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Balance Update >> {data.Currency} >> Balance:{data.TotalAvailableBalance} Available:{data.MarginMode} Frozen:{data.MarginFrozen}");
                }
            });

            // Orders
            ws.Futures_SubscribeToOrders("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.State}");
                }
            });

            // Algo Orders
            ws.Futures_SubscribeToAlgoOrders("ETH-USDT", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.Status}");
                }
            });

            /* 06. Swap - Public */

            // Ticker
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToTicker(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Ticker >> {data.Symbol} >> LP:{data.LastPrice} LQ:{data.LastQuantity} Bid:{data.BestBidPrice} BS:{data.BestBidSize} Ask:{data.BestAskPrice} AS:{data.BestAskSize} 24H:{data.High24H} 24L:{data.Low24H} 24BV:{data.BaseVolume24H} 24QV:{data.QuoteVolume24H} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Candlesticks
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToCandlesticks(pair, OkexSpotPeriod.FiveMinutes, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Candle >> {data.Symbol} >> ST:{data.StartTime} O:{data.Open} H:{data.High} L:{data.Low} C:{data.Close} V:{data.BaseVolume}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Trades
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToTrades(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Trades >> {pair} >> ID:{data.TradeId} P:{data.Price} Q:{data.Quantity} S:{data.Side} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Funding Rate
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToFundingRate(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Price Range >> {pair} >> RR:{data.RealizedRate} FR:{data.FundingRate} IR:{data.InterestRate}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Price Range
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToPriceRange(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Price Range >> {pair} >> H:{data.Highest} L:{data.Lowest} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Order Book
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToOrderBook(pair, OkexOrderBookDepth.Depth400, (data) =>
                {
                    if (data != null && data.Asks != null && data.Asks.Count() > 0 && data.Bids != null && data.Bids.Count() > 0)
                    {
                        Console.WriteLine($"Depth >> {data.Symbol} >> Ask P:{data.Asks.First().Price} Q:{data.Asks.First().Quantity} C:{data.Asks.First().OrdersCount} Bid P:{data.Bids.First().Price} Q:{data.Bids.First().Quantity} C:{data.Bids.First().OrdersCount} ");
                    }
                });
                subs.Add(subscription.Data);
            }

            // Mark Price
            foreach (var pair in swap_pairs)
            {
                var subscription = ws.Swap_SubscribeToMarkPrice(pair, (data) =>
                {
                    if (data != null)
                    {
                        Console.WriteLine($"Trades >> {pair} >> P:{data.MarkPrice} T:{data.Timestamp}");
                    }
                });
                subs.Add(subscription.Data);
            }

            /* 06. Swap - Private */

            // Positions
            ws.Swap_SubscribeToPositions("BTC-USD-SWAP", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Positions Update >> BTC-USD-SWAP >> MM:{data.MarginMode} T:{data.Timestamp}");
                }
            });

            // Balance
            ws.Swap_SubscribeToBalance("BTC-USD-SWAP", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Balance Update >> BTC-USD-SWAP >> Balance:{data.TotalAvailableBalance} Available:{data.MarginMode} Frozen:{data.MarginFrozen}");
                }
            });

            // Orders
            ws.Swap_SubscribeToOrders("BTC-USD-SWAP", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.State}");
                }
            });

            // Algo Orders
            ws.Swap_SubscribeToAlgoOrders("BTC-USD-SWAP", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Symbol} >> Id:{data.OrderId} State:{data.Status}");
                }
            });

            /* 07. Options - Public */

            // Contracts
            ws.Options_SubscribeToContracts("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Contract >> {data.Instrument} >> C:{data.Category} U:{data.Underlying}");
                }
            });

            // MarketData
            ws.Options_SubscribeToMarketData("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Ticker >> {data.Instrument} >> LP:{data.Last} Bid:{data.BestBid} BS:{data.BidVolume} Ask:{data.BestAsk} AS:{data.AskVolume} 24H:{data.High24H} 24L:{data.Low24H}");
                }
            });

            // Candlesticks
            ws.Options_SubscribeToCandlesticks("BTC-USD-201218-16250-C", OkexSpotPeriod.FiveMinutes, (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Candle >> {data.Symbol} >> ST:{data.StartTime} O:{data.Open} H:{data.High} L:{data.Low} C:{data.Close} V:{data.BaseVolume}");
                }
            });

            // Trades
            ws.Options_SubscribeToTrades("BTC-USD-201218-16250-C", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Trades >> ID:{data.TradeId} P:{data.Price} Q:{data.Quantity} S:{data.Side} T:{data.Timestamp}");
                }
            });

            // Ticker
            ws.Options_SubscribeToTicker("BTC-USD-201218-16250-C", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Ticker >> {data.Symbol} >> LP:{data.LastPrice} LQ:{data.LastQuantity} Bid:{data.BestBidPrice} BS:{data.BestBidSize} Ask:{data.BestAskPrice} AS:{data.BestAskSize} 24H:{data.High24H} 24L:{data.Low24H}");
                }
            });

            // Order Book
            ws.Options_SubscribeToOrderBook("BTC-USD-201218-16250-C", OkexOrderBookDepth.Depth400, (data) =>
            {
                if (data != null && data.Asks != null && data.Asks.Count() > 0 && data.Bids != null && data.Bids.Count() > 0)
                {
                    Console.WriteLine($"Depth >> {data.Symbol} >> Ask P:{data.Asks.First().Price} Q:{data.Asks.First().Quantity} C:{data.Asks.First().OrdersCount} Bid P:{data.Bids.First().Price} Q:{data.Bids.First().Quantity} C:{data.Bids.First().OrdersCount} ");
                }
            });

            /* 07. Options - Private */

            // Positions
            ws.Options_SubscribeToPositions("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Positions Update >> {data.Instrument} >> RPNL:{data.RealizedPnl} UPNL:{data.UnrealizedPnl}");
                }
            });

            // Balance
            ws.Options_SubscribeToBalance("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Balance Update >> {data.Currency} >> Balance:{data.TotalAvailableBalance} Status:{data.AccountStatus}");
                }
            });

            // Orders
            ws.Options_SubscribeToOrders("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Order Update >> {data.Instrument} >> Id:{data.OrderId} State:{data.State}");
                }
            });

            /* 09. Index - Public */

            // Ticker
            ws.Index_SubscribeToTicker("BTC-USD", (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Ticker >> {data.Symbol} >> 24O:{data.Open24H} 24H:{data.High24H} 24L:{data.Low24H}");
                }
            });

            // Candlesticks
            ws.Index_SubscribeToCandlesticks("BTC-USD", OkexSpotPeriod.FiveMinutes, (data) =>
            {
                if (data != null)
                {
                    Console.WriteLine($"Candle >> {data.Symbol} >> ST:{data.StartTime} O:{data.Open} H:{data.High} L:{data.Low} C:{data.Close} V:{data.BaseVolume}");
                }
            });

            Console.ReadLine();
            return;
        }
    }
}
