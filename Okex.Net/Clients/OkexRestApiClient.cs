using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using CryptoExchange.Net.Objects;
using Okex.Net.CoreObjects;
using Okex.Net.Enums;
using Okex.Net.Models;

namespace Okex.Net.Clients
{
	public class OkexRestApiClient : RestApiClient, IDisposable
	{
		public OkexRestApiClient(OkexBaseRestClient baseClient, BaseRestClientOptions options, OkexRestApiClientOptions apiOptions) : base(options, apiOptions)
		{
			_baseClient = baseClient;
			_apiOptions = apiOptions;
		}

		private readonly OkexBaseRestClient _baseClient;
		private readonly OkexRestApiClientOptions _apiOptions;
		private readonly TimeSyncInfo _timeSyncInfo = new TimeSyncInfo(new Log("OKEXRestApiClient"), false, TimeSpan.Zero, new TimeSyncState("OKEXRestApiClient"));

		private OkexAuthenticationProvider _provider;

		#region EndPoints

		private const string Endpoints_Currencies = "api/v5/asset/currencies";
		private const string Endpoints_Instruments = "api/v5/public/instruments";
		private const string Endpoints_OrderBooks = "api/v5/market/books";
		private const string Endpoints_PlaceOrder = "api/v5/trade/order";
		private const string Endpoints_OrderDetails = "api/v5/trade/order";
		private const string Endpoints_Ticker = "api/v5/market/ticker";
		private const string Endpoints_OrderList = "api/v5/trade/orders-pending";
		private const string Endpoints_OrderHistory = "api/v5/trade/orders-history";
		private const string Endpoints_Balances = "api/v5/account/balance";
		private const string Endpoints_AccountConfig = "api/v5/account/config";
		private const string Endpoints_CancelOrder = "api/v5/trade/cancel-order";
		private const string Endpoints_MaxSizeAmount = "api/v5/account/max-size";
		private const string Endpoints_AvailableMaxSizeAmount = "api/v5/account/max-avail-size";
		private const string Endpoints_Status = "api/v5/system/status";
		private const string Endpoints_BorrowInfo = "api/v5/asset/lending-rate-summary";
		private const string Endpoints_BorrowHistory = "api/v5/asset/lending-rate-history";
		private const string Endpoints_FundingRate = "api/v5/public/funding-rate";
		private const string Endpoints_FundingRateHistory = "api/v5/public/funding-rate-history";
		private const string Endpoints_CandleStickList = "api/v5/market/candles";
		private const string Endpoints_TradeFee = "api/v5/account/trade-fee";

		#endregion

		#region Override

		public override TimeSyncInfo GetTimeSyncInfo() => _timeSyncInfo;

		public override TimeSpan GetTimeOffset()
		{
			return TimeSpan.Zero;
		}

		protected override Task<WebCallResult<DateTime>> GetServerTimestampAsync()
		{
			return Task.FromResult(new WebCallResult<DateTime>(HttpStatusCode.OK, null, null, null, null, null, null, null, DateTime.Now, null));
		}

		protected override AuthenticationProvider CreateAuthenticationProvider(ApiCredentials credentials)
		{
			_provider = new OkexAuthenticationProvider(_apiOptions.ApiCredentials!, _apiOptions.PassPhrase, _apiOptions.SignPublicRequests);
			return _provider;
		}

		#endregion

		#region Rest

		public Task<WebCallResult<OkexApiResponse<OkexCurrency>>> GetCurrenciesAsync(CancellationToken ct = default)
		{
			return SendRequestAsync<OkexApiResponse<OkexCurrency>>(GetUrl(Endpoints_Currencies), HttpMethod.Get, ct, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexInstrument>>> GetInstrumentsAsync(OkexInstrumentTypeEnum okexInstrumentType, string underlying = "", string instId = "", CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object> { { "instType", okexInstrumentType.ToString() } };

			if (string.IsNullOrWhiteSpace(underlying) && okexInstrumentType == OkexInstrumentTypeEnum.OPTION)
				throw new ArgumentException("Underlying required for OPTION");

			if (!string.IsNullOrWhiteSpace(underlying) && okexInstrumentType == OkexInstrumentTypeEnum.SPOT)
				throw new ArgumentException("Underlying only applicable to FUTURES/SWAP/OPTION");

			if (!string.IsNullOrWhiteSpace(underlying))
				parameters.Add("uly", underlying);

			if (!string.IsNullOrWhiteSpace(instId))
				parameters.Add("instId", instId);

			return SendRequestAsync<OkexApiResponse<OkexInstrument>>(GetUrl(Endpoints_Instruments), HttpMethod.Get, ct, parameters);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderBook>>> GetOrderBookAsync(string instrumentName, string depth = "1", CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}

			var parameters = new Dictionary<string, object> { { "instId", instrumentName }, { "sz", depth } };

			return SendRequestAsync<OkexApiResponse<OkexOrderBook>>(GetUrl(Endpoints_OrderBooks), HttpMethod.Get, ct, parameters);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderInfo>>> PlaceOrderAsync(OkexOrderParams orderParams, CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object>();
			if (string.IsNullOrWhiteSpace(orderParams.InstrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}

			if (orderParams.Amount == 0m)
			{
				throw new ArgumentException("Amount must be more then 0");
			}

			if (orderParams.Price is null && orderParams.OrderType == OkexOrderTypeEnum.limit)
			{
				throw new ArgumentException("Price required for limit order");
			}

			parameters.Add("instId", orderParams.InstrumentName);
			parameters.Add("tdMode", orderParams.OkexTradeMode.ToString());
			parameters.Add("side", orderParams.Side.ToString());
			parameters.Add("ordType", orderParams.OrderType.ToString());
			if (orderParams.OrderType == OkexOrderTypeEnum.limit)
			{
				parameters.Add("px", orderParams.Price.Value.ToString());
			}
			if (!string.IsNullOrWhiteSpace(orderParams.Currency))
			{
				parameters.Add("ccy", orderParams.Currency);
			}
			if (!string.IsNullOrWhiteSpace(orderParams.ClientSuppliedOrderId))
			{
				parameters.Add("clOrdId", orderParams.ClientSuppliedOrderId);
			}
			if (!string.IsNullOrWhiteSpace(orderParams.Tag))
			{
				parameters.Add("tag", orderParams.Tag);
			}
			if (!string.IsNullOrWhiteSpace(orderParams.Tag))
			{
				parameters.Add("tag", orderParams.Tag);
			}
			if (orderParams.PositionSide != null)
			{
				parameters.Add("posSide", orderParams.PositionSide.ToString());
			}
			if (orderParams.ReduceOnly != null)
			{
				parameters.Add("reduceOnly", orderParams.ReduceOnly.Value);
			}

			if (orderParams.OrderType == OkexOrderTypeEnum.market && orderParams.InstrumentType == OkexInstrumentTypeEnum.SPOT)
			{
				parameters.Add("tgtCcy", orderParams.QuantityType.Value.ToString());
			}

			if (orderParams.QuantityType is QuantityType.base_ccy)
			{
				parameters.Add("sz", orderParams.Amount);
			}
			else
			{
				parameters.Add("sz", (int)orderParams.Amount);
			}

			return SendRequestAsync<OkexApiResponse<OkexOrderInfo>>(GetUrl(Endpoints_PlaceOrder), HttpMethod.Post, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderDetails>>> GetOrderDetailsAsync(string instrumentName, string orderId = "", string clientSuppliedId = "", CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}
			var parameters = new Dictionary<string, object>
			{
				{"instId", instrumentName}
			};

			if (!string.IsNullOrWhiteSpace(orderId))
			{
				parameters.Add("ordId", orderId);
			}

			if (!string.IsNullOrWhiteSpace(clientSuppliedId))
			{
				parameters.Add("clOrdId", clientSuppliedId);
			}

			return SendRequestAsync<OkexApiResponse<OkexOrderDetails>>(GetUrl(Endpoints_OrderDetails), HttpMethod.Get, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexTicker>>> GetTickerAsync(string instrumentName, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}

			var parameters = new Dictionary<string, object>
			{
				{"instId", instrumentName}
			};

			return SendRequestAsync<OkexApiResponse<OkexTicker>>(GetUrl(Endpoints_Ticker), HttpMethod.Get, ct, parameters, signed: false);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderDetails>>> GetOrderListAsync(
			OkexOrderListParams listParams, CancellationToken ct = default)
		{
			if (listParams.State.HasValue && (listParams.State != OkexOrderStateEnum.live || listParams.State != OkexOrderStateEnum.partially_filled))
			{
				throw new ArgumentException("State must be live or partially_filled");
			}

			var parameters = new Dictionary<string, object>();
			if (listParams.InstrumentType.HasValue)
			{
				parameters.Add("instType", listParams.InstrumentType.ToString());
			}

			if (!string.IsNullOrWhiteSpace(listParams.Underlying))
			{
				parameters.Add("uly", listParams.Underlying);
			}

			if (!string.IsNullOrWhiteSpace(listParams.InstrumentName))
			{
				parameters.Add("instId", listParams.InstrumentName);
			}

			if (listParams.OrderType.HasValue)
			{
				parameters.Add("ordType", listParams.OrderType.ToString());
			}

			if (listParams.State.HasValue)
			{
				parameters.Add("state", listParams.State.ToString());
			}

			if (!string.IsNullOrWhiteSpace(listParams.AfterOrderId))
			{
				parameters.Add("after", listParams.AfterOrderId);
			}

			if (!string.IsNullOrWhiteSpace(listParams.AfterOrderId))
			{
				parameters.Add("before", listParams.BeforeOrderId);
			}

			if (!string.IsNullOrWhiteSpace(listParams.Limit))
			{
				parameters.Add("limit", listParams.BeforeOrderId);
			}

			return SendRequestAsync<OkexApiResponse<OkexOrderDetails>>(GetUrl(Endpoints_OrderList), HttpMethod.Get, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderDetails>>> GetOrderHistoryAsync(
			OkexOrderListParams listParams, CancellationToken ct = default)
		{
			if (listParams.State.HasValue && (listParams.State != OkexOrderStateEnum.live || listParams.State != OkexOrderStateEnum.partially_filled))
			{
				throw new ArgumentException("State must be live or partially_filled");
			}

			var parameters = new Dictionary<string, object>();
			if (listParams.InstrumentType.HasValue)
			{
				parameters.Add("instType", listParams.InstrumentType.ToString());
			}

			if (!string.IsNullOrWhiteSpace(listParams.Underlying))
			{
				parameters.Add("uly", listParams.Underlying);
			}

			if (!string.IsNullOrWhiteSpace(listParams.InstrumentName))
			{
				parameters.Add("instId", listParams.InstrumentName);
			}

			if (listParams.OrderType.HasValue)
			{
				parameters.Add("ordType", listParams.OrderType.ToString());
			}

			if (listParams.State.HasValue)
			{
				parameters.Add("state", listParams.State.ToString());
			}

			if (!string.IsNullOrWhiteSpace(listParams.AfterOrderId))
			{
				parameters.Add("after", listParams.AfterOrderId);
			}

			if (!string.IsNullOrWhiteSpace(listParams.AfterOrderId))
			{
				parameters.Add("before", listParams.BeforeOrderId);
			}

			if (!string.IsNullOrWhiteSpace(listParams.Limit))
			{
				parameters.Add("limit", listParams.BeforeOrderId);
			}

			return SendRequestAsync<OkexApiResponse<OkexOrderDetails>>(GetUrl(Endpoints_OrderHistory), HttpMethod.Get, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexAccountDetails>>> GetBalancesAsync(string currency = "", CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(currency))
			{
				parameters.Add("ccy", currency);
			}

			return SendRequestAsync<OkexApiResponse<OkexAccountDetails>>(GetUrl(Endpoints_Balances), HttpMethod.Get, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexAccountConfig>>> GetAccountConfigAsync(CancellationToken ct = default)
		{
			return SendRequestAsync<OkexApiResponse<OkexAccountConfig>>(GetUrl(Endpoints_AccountConfig), HttpMethod.Get, ct, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexOrderInfo>>> CancelOrderAsync(string instrumentName, string orderId = "", string clientOrderId = "", CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				throw new ArgumentException("Instrument name must not be null or empty");

			var okexParams = new Dictionary<string, object> { { "instId", instrumentName } };

			if (string.IsNullOrWhiteSpace(orderId) && string.IsNullOrWhiteSpace(clientOrderId))
				throw new ArgumentException("Either ordId or clOrdId is required.");

			if (!string.IsNullOrWhiteSpace(orderId))
			{
				okexParams.Add("ordId", orderId);
			}

			if (!string.IsNullOrWhiteSpace(clientOrderId))
			{
				okexParams.Add("clOrdId", clientOrderId);
			}

			return SendRequestAsync<OkexApiResponse<OkexOrderInfo>>(GetUrl(Endpoints_CancelOrder), HttpMethod.Post, ct, okexParams,
				signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<AmountMaxSizeInfo>>> GetAmountMaxSizeAsync(string instrumentName,
			OkexTradeModeEnum tradeMode, string currency = "", decimal? price = null, decimal? leverage = null, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				throw new ArgumentException("Instrument name must not be null or empty");

			var okexParams = new Dictionary<string, object> { { "instId", instrumentName }, { "tdMode", tradeMode } };
			if (!string.IsNullOrWhiteSpace(currency))
			{
				okexParams.Add("ccy", currency);
			}

			if (price.HasValue)
			{
				okexParams.Add("px", price);
			}

			if (leverage.HasValue)
			{
				okexParams.Add("leverage", leverage);
			}

			return SendRequestAsync<OkexApiResponse<AmountMaxSizeInfo>>(GetUrl(Endpoints_MaxSizeAmount), HttpMethod.Get, ct, okexParams,
				signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<AvailableMaxSizeInfo>>> GetAvailableAmountMaxSizeAsync(string instrumentName,
			OkexTradeModeEnum tradeMode, string currency = "", decimal? price = null, decimal? leverage = null, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
				throw new ArgumentException("Instrument name must not be null or empty");

			var okexParams = new Dictionary<string, object> { { "instId", instrumentName }, { "tdMode", tradeMode } };
			if (!string.IsNullOrWhiteSpace(currency))
			{
				okexParams.Add("ccy", currency);
			}

			if (price.HasValue)
			{
				okexParams.Add("px", price);
			}

			if (leverage.HasValue)
			{
				okexParams.Add("leverage", leverage);
			}

			return SendRequestAsync<OkexApiResponse<AvailableMaxSizeInfo>>(GetUrl(Endpoints_AvailableMaxSizeAmount), HttpMethod.Get, ct, okexParams,
				signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexSystemStatus>>> GetSystemStatusAsync(OkexMaintenanceStateEnum? maintenanceState = null, CancellationToken ct = default)
		{
			var okexParams = new Dictionary<string, object>();
			if (maintenanceState.HasValue)
			{
				okexParams.Add("state", maintenanceState.ToString());
			}

			return SendRequestAsync<OkexApiResponse<OkexSystemStatus>>(GetUrl(Endpoints_Status), HttpMethod.Get, ct, okexParams);
		}

		public Task<WebCallResult<OkexApiResponse<OkexBorrowInfo>>> GetBorrowInfoAsync(string? currency, CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object?>();
			if (!string.IsNullOrWhiteSpace(currency))
			{
				parameters.Add("ccy", currency);
			}

			return SendRequestAsync<OkexApiResponse<OkexBorrowInfo>>(
				GetUrl(Endpoints_BorrowInfo),
				HttpMethod.Get,
				ct,
				parameters,
				useProd: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexBorrowHistory>>> GetBorrowHistoryAsync(string currency,
            long? since, long? until, int? limit, CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(currency))
			{
				parameters.Add("ccy", currency);
			}
			if (since.HasValue)
			{
				parameters.Add("before", since);
			}
			if (until.HasValue)
			{
				parameters.Add("after", until);
			}
			if (limit.HasValue)
			{
				parameters.Add("limit", limit);
			}

			return SendRequestAsync<OkexApiResponse<OkexBorrowHistory>>(
				GetUrl(Endpoints_BorrowHistory),
				HttpMethod.Get,
				ct,
				parameters,
				useProd: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexFundingRate>>> GetFundingRateAsync(string instId, CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object> { { "instId", instId } };

			return SendRequestAsync<OkexApiResponse<OkexFundingRate>>(GetUrl(Endpoints_FundingRate), HttpMethod.Get, ct, parameters, signed: true);
		}

		public Task<WebCallResult<OkexApiResponse<OkexFundingRateHistory>>> GetFundingRateHistoryAsync(
            string instrumentName,
            long? since, long? until, int? limit, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be null or empty");
			}
			var parameters = new Dictionary<string, object> { { "instId", instrumentName } };
			if (since.HasValue)
			{
				parameters.Add("before", since);
			}
			if (until.HasValue)
			{
				parameters.Add("after", until);
			}
			if (limit.HasValue)
			{
				parameters.Add("limit", limit);
			}

			return SendRequestAsync<OkexApiResponse<OkexFundingRateHistory>>(GetUrl(Endpoints_FundingRateHistory), HttpMethod.Get, ct, parameters);
		}

		public Task<WebCallResult<OkexApiResponse<OkexCandleStickEntry>>> GetCandleStickListAsync(string instrumentName, string? timeFrame, long? since = null, long? until = null,
			int? limit = null, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be null or empty");
			}
			var parameters = new Dictionary<string, object> { { "instId", instrumentName } };
			if (!string.IsNullOrWhiteSpace(timeFrame))
			{
				parameters.Add("bar", timeFrame);
			}
			if (since.HasValue)
			{
				parameters.Add("before", since);
			}
			if (until.HasValue)
			{
				parameters.Add("after", until);
			}
			if (limit.HasValue)
			{
				parameters.Add("limit", limit);
			}

			return SendRequestAsync<OkexApiResponse<OkexCandleStickEntry>>(GetUrl(Endpoints_CandleStickList), HttpMethod.Get, ct, parameters);
		}


		public Task<WebCallResult<OkexApiResponse<OkexTradeFee>>> GetTradeFeeAsync(OkexInstrumentTypeEnum instrumentType,
			string? instrumentId = null, string? uly = null, CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object> { { "instType", instrumentType } };

			if (!string.IsNullOrWhiteSpace(instrumentId))
			{
				parameters.Add("instId", instrumentId!);
			}

			if (!string.IsNullOrWhiteSpace(uly))
			{
				parameters.Add("uly", uly!);
			}

			return SendRequestAsync<OkexApiResponse<OkexTradeFee>>(GetUrl(Endpoints_TradeFee), HttpMethod.Get, ct, parameters, true);
		}

		#endregion

		private Uri GetUrl(string endpoint, string param = "")
		{
			var x = endpoint.IndexOf('<');
			var y = endpoint.IndexOf('>');
			if (x > -1 && y > -1) endpoint = endpoint.Replace(endpoint.Substring(x, y - x + 1), param);

			return new Uri($"{BaseAddress.TrimEnd('/')}/{endpoint}");
		}

		private Task<WebCallResult<T>> SendRequestAsync<T>(Uri uri, HttpMethod method, CancellationToken cancellationToken,
			Dictionary<string, object>? parameters = null, bool signed = false, bool useProd = false) where T : class
		{
			return _baseClient.SendRequestAsync<T>(this, uri, method, cancellationToken, parameters, signed, useProd: useProd);
		}

		public new void Dispose()
		{
			base.Dispose();
			_timeSyncInfo.TimeSyncState.Semaphore.Dispose();
			_apiOptions.PassPhrase.Dispose();
		}
	}
}
