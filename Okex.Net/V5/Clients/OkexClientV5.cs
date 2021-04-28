using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.CoreObjects;
using Okex.Net.Interfaces;
using Okex.Net.V5.Enums;
using Okex.Net.V5.Models;

namespace Okex.Net.V5.Clients
{
	public class OkexClientV5 : RestClient, IOkexClient
	{
		public OkexClientV5(string clientName, RestClientOptions exchangeOptions, AuthenticationProvider? authenticationProvider) : base(clientName, exchangeOptions, authenticationProvider)
		{
			manualParseError = true;
		}

		public OkexClientV5(OkexClientOptions options) : base("Okex", options, options.ApiCredentials == null ? null : new OkexAuthenticationProvider(options.ApiCredentials, "", options.SignPublicRequests, ArrayParametersSerialization.Array))
		{
			SignPublicRequests = options.SignPublicRequests;
		}

		public bool SignPublicRequests { get; }

		private static readonly string BodyParameterKey = "<BODY>";

		#region V5 EndPoints
		private const string Endpoints_Currencies = "api/v5/asset/currencies";
		private const string Endpoints_Instruments = "api/v5/public/instruments";
		private const string Endpoints_OrderBooks = "api/v5/market/books";
		private const string Endpoints_PlaceOrder = "api/v5/trade/order";
		private const string Endpoints_OrderDetails = "api/v5/trade/order";
		private const string Endpoints_Ticker = "api/v5/market/ticker";
		private const string Endpoints_OrderList = "api/v5/trade/orders-pending";
		private const string Endpoints_Balances = "api/v5/account/balance";
		private const string Endpoints_AccountConfig = "api/v5/account/config";
		
		#endregion


		public void SetApiCredentials(string apiKey, string apiSecret, string passPhrase)
		{
			SetAuthenticationProvider(new OkexAuthenticationProvider(new ApiCredentials(apiKey, apiSecret), passPhrase, SignPublicRequests, ArrayParametersSerialization.Array));
		}

		public async Task<WebCallResult<OkexApiResponse<OkexCurrency>>> GetCurrenciesAsync(CancellationToken ct = default)
		{
			return await SendRequest<OkexApiResponse<OkexCurrency>>(GetUrl(Endpoints_Currencies), HttpMethod.Get, ct, signed: true).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexInstrument>>> GetInstrumentsAsync(OkexInstrumentTypeEnum okexInstrumentType, string underlying = "", string instId = "", CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object> { { "instType", okexInstrumentType.ToString() } };

			if (string.IsNullOrWhiteSpace(underlying) && okexInstrumentType == OkexInstrumentTypeEnum.OPTION)
			{
				throw new ArgumentException("Underlying required for OPTION");
			}

			if (!string.IsNullOrWhiteSpace(underlying) && okexInstrumentType == OkexInstrumentTypeEnum.SPOT)
			{
				throw new ArgumentException("Underlying only applicable to FUTURES/SWAP/OPTION");
			}

			if (!string.IsNullOrWhiteSpace(underlying))
			{
				parameters.Add("uly", underlying);
			}

			if (!string.IsNullOrWhiteSpace(instId))
			{
				parameters.Add("instId", instId);
			}

			return await SendRequest<OkexApiResponse<OkexInstrument>>(GetUrl(Endpoints_Instruments), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexOrderBook>>> GetOrderBookAsync(string instrumentName, string depth = "1", CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}

			var parameters = new Dictionary<string, object> { { "instId", instrumentName }, { "sz", depth } };

			return await SendRequest<OkexApiResponse<OkexOrderBook>>(GetUrl(Endpoints_OrderBooks), HttpMethod.Get, ct, parameters).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexOrderInfo>>> PlaceOrderAsync(OkexOrderParams orderParams, CancellationToken ct = default)
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
			parameters.Add("sz", orderParams.Amount.ToString(CultureInfo.InvariantCulture));
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

			return await SendRequest<OkexApiResponse<OkexOrderInfo>>(GetUrl(Endpoints_PlaceOrder), HttpMethod.Post, ct, parameters, signed: true).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexOrderDetails>>> GetOrderDetailsAsync(string instrumentName, string orderId = "", string clientSuppliedId = "", CancellationToken ct = default)
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

			return await SendRequest<OkexApiResponse<OkexOrderDetails>>(GetUrl(Endpoints_OrderDetails), HttpMethod.Get, ct, parameters, signed: true).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexTicker>>> GetTickerAsync(string instrumentName, CancellationToken ct = default)
		{
			if (string.IsNullOrWhiteSpace(instrumentName))
			{
				throw new ArgumentException("Instrument name must not be empty or null");
			}

			var parameters = new Dictionary<string, object>
			{
				{"instId", instrumentName}
			};

			return await SendRequest<OkexApiResponse<OkexTicker>>(GetUrl(Endpoints_Ticker), HttpMethod.Get, ct, parameters, signed: false).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexOrderDetails>>> GetOrderListAsync(
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

			return await SendRequest<OkexApiResponse<OkexOrderDetails>>(GetUrl(Endpoints_OrderList), HttpMethod.Get, ct, parameters, signed: true).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexBalances>>> GetBalancesAsync(string currency = "", CancellationToken ct = default)
		{
			var parameters = new Dictionary<string, object>();
			if (!string.IsNullOrWhiteSpace(currency))
			{
				parameters.Add("ccy", currency);
			}

			return await SendRequest<OkexApiResponse<OkexBalances>>(GetUrl(Endpoints_Balances), HttpMethod.Get, ct, parameters, signed: true).ConfigureAwait(false);
		}

		public async Task<WebCallResult<OkexApiResponse<OkexAccountConfig>>> GetAccountConfigAsync(CancellationToken ct = default)
		{
			return await SendRequest<OkexApiResponse<OkexAccountConfig>>(GetUrl(Endpoints_AccountConfig), HttpMethod.Get, ct, signed: true).ConfigureAwait(false);
		}

		protected virtual Uri GetUrl(string endpoint, string param = "")
		{
			var x = endpoint.IndexOf('<');
			var y = endpoint.IndexOf('>');
			if (x > -1 && y > -1) endpoint = endpoint.Replace(endpoint.Substring(x, y - x + 1), param);

			return new Uri($"{BaseAddress.TrimEnd('/')}/{endpoint}");
		}

		protected override IRequest ConstructRequest(Uri uri, HttpMethod method, Dictionary<string, object>? parameters, bool signed, PostParameters postPosition, ArrayParametersSerialization arraySerialization, int requestId)
		{
			return this.OkexConstructRequest(uri, method, parameters, signed, postPosition, arraySerialization, requestId);
		}
		protected virtual IRequest OkexConstructRequest(Uri uri, HttpMethod method, Dictionary<string, object>? parameters, bool signed, PostParameters postPosition, ArrayParametersSerialization arraySerialization, int requestId)
		{
			if (parameters == null)
				parameters = new Dictionary<string, object>();

			var uriString = uri.ToString();
			if (authProvider != null)
				parameters = authProvider.AddAuthenticationToParameters(uriString, method, parameters, signed, postPosition, arraySerialization);

			if ((method == HttpMethod.Get || method == HttpMethod.Delete || postParametersPosition == PostParameters.InUri) && parameters?.Any() == true)
				uriString += "?" + parameters.CreateParamString(true, arraySerialization);

			if (method == HttpMethod.Post && signed)
			{
				var uriParamNames = new[] { "AccessKeyId", "SignatureMethod", "SignatureVersion", "Timestamp", "Signature" };
				var uriParams = parameters.Where(p => uriParamNames.Contains(p.Key)).ToDictionary(k => k.Key, k => k.Value);
				uriString += "?" + uriParams.CreateParamString(true, ArrayParametersSerialization.MultipleValues);
				parameters = parameters.Where(p => !uriParamNames.Contains(p.Key)).ToDictionary(k => k.Key, k => k.Value);
			}

			var contentType = requestBodyFormat == RequestBodyFormat.Json ? Constants.JsonContentHeader : Constants.FormContentHeader;
			var request = RequestFactory.Create(method, uriString, requestId);
			request.Accept = Constants.JsonContentHeader;

			var headers = new Dictionary<string, string>();
			if (authProvider != null)
				headers = authProvider.AddAuthenticationToHeaders(uriString, method, parameters!, signed, postPosition, arraySerialization);

			foreach (var header in headers)
				request.AddHeader(header.Key, header.Value);

			if ((method == HttpMethod.Post || method == HttpMethod.Put) && postParametersPosition != PostParameters.InUri)
			{
				if (parameters?.Any() == true)
					WriteParamBody(request, parameters, contentType);
				else
					request.SetContent(requestBodyEmptyContent, contentType);
			}

			return request;
		}

		protected override void WriteParamBody(IRequest request, Dictionary<string, object> parameters, string contentType)
		{
			this.OkexWriteParamBody(request, parameters, contentType);
		}

		protected virtual void OkexWriteParamBody(IRequest request, Dictionary<string, object> parameters, string contentType)
		{
			if (requestBodyFormat == RequestBodyFormat.Json)
			{
				if (parameters.Count == 1 && parameters.Keys.First() == BodyParameterKey)
				{
					var stringData = JsonConvert.SerializeObject(parameters[BodyParameterKey]);
					request.SetContent(stringData, contentType);
				}
				else
				{
					var stringData = JsonConvert.SerializeObject(parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
					request.SetContent(stringData, contentType);
				}
			}
			else if (requestBodyFormat == RequestBodyFormat.FormData)
			{
				var formData = HttpUtility.ParseQueryString(string.Empty);
				foreach (var kvp in parameters.OrderBy(p => p.Key))
				{
					if (kvp.Value.GetType().IsArray)
					{
						var array = (Array)kvp.Value;
						foreach (var value in array)
							formData.Add(kvp.Key, value.ToString());
					}
					else
						formData.Add(kvp.Key, kvp.Value.ToString());
				}
				var stringData = formData.ToString();
				request.SetContent(stringData, contentType);
			}
		}

		protected override Error ParseErrorResponse(JToken error)
		{
			return this.OkexParseErrorResponse(error);
		}

		protected virtual Error OkexParseErrorResponse(JToken error)
		{
			if (error["code"] == null || error["msg"] == null)
				return new ServerError(error.ToString());

			return new ServerError((int)error["code"]!, (string)error["msg"]!);
		}
	}
}
