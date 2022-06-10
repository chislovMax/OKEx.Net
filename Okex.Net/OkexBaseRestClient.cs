using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Clients;
using Okex.Net.CoreObjects;
using Okex.Net.Helpers;

namespace Okex.Net
{
	public class OkexBaseRestClient : BaseRestClient, IDisposable
	{
		public OkexBaseRestClient(BaseRestClientOptions options, OkexRestApiClientOptions okexRestApiOptions) : base("OKEX", options)
		{
			Common = AddApiClient(new OkexRestApiClient(this, options, okexRestApiOptions));
			_isTest = okexRestApiOptions.IsTest;
		}

		private readonly bool _isTest;

		internal async Task<WebCallResult<T>> SendRequestAsync<T>(
			RestApiClient apiClient,
			Uri uri,
			HttpMethod method,
			CancellationToken cancellationToken,
			Dictionary<string, object>? parameters = null,
			bool signed = false,
			HttpMethodParameterPosition? parameterPosition = null,
			ArrayParametersSerialization? arraySerialization = null,
			int requestWeight = 1,
			JsonSerializer? deserializer = null,
			Dictionary<string, string>? additionalHeaders = null,
			bool ignoreRatelimit = false,
			bool useProd = false
		) where T : class
		{
			var requestId = NextId();

			if (signed && apiClient.AuthenticationProvider == null)
			{
				log.Write(LogLevel.Warning,
					$"[{requestId}] Request {uri.AbsolutePath} failed because no ApiCredentials were provided");
				return new WebCallResult<T>(new NoApiCredentialsError());
			}

			var paramsPosition = parameterPosition ?? apiClient.ParameterPositions[method];
			var request = ConstructRequest(apiClient, uri, method, parameters, signed, paramsPosition,
				arraySerialization ?? apiClient.arraySerialization, requestId, additionalHeaders);
			if (_isTest && !useProd)
			{
				request.AddHeader("x-simulated-trading", "1");
			}

			apiClient.TotalRequestsMade++;
			return await GetResponseAsync<T>(apiClient, request, deserializer, cancellationToken, false).ConfigureAwait(false);
		}

		protected override IRequest ConstructRequest(RestApiClient apiClient, Uri uri, HttpMethod method, Dictionary<string, object>? parameters, bool signed,
			HttpMethodParameterPosition parameterPosition, ArrayParametersSerialization arraySerialization, int requestId,
			Dictionary<string, string>? additionalHeaders)
		{
			parameters ??= new Dictionary<string, object>();

			if (parameterPosition == HttpMethodParameterPosition.InUri)
			{
				foreach (var parameter in parameters)
					uri = uri.AddQueryParmeter(parameter.Key, parameter.Value.ToString());
			}

			var headers = new Dictionary<string, string>();
			//Из-за сортировки параметров неправильно работала подпись
			var uriParameters = parameterPosition == HttpMethodParameterPosition.InUri ? new SortedDictionary<string, object>(parameters) : new SortedDictionary<string, object>();
			var bodyParameters = parameterPosition == HttpMethodParameterPosition.InBody ? new SortedDictionary<string, object>(parameters) : new SortedDictionary<string, object>();
			if (apiClient.AuthenticationProvider != null)
				apiClient.AuthenticationProvider.AuthenticateRequest(
					 apiClient,
					 uri,
					 method,
					 parameters,
					 signed,
					 arraySerialization,
					 parameterPosition,
					 out uriParameters,
					 out bodyParameters,
					 out headers);

			var request = RequestFactory.Create(method, uri, requestId);
			request.Accept = Constants.JsonContentHeader;

			foreach (var header in headers)
				request.AddHeader(header.Key, header.Value);

			if (additionalHeaders != null)
			{
				foreach (var header in additionalHeaders)
					request.AddHeader(header.Key, header.Value);
			}

			if (StandardRequestHeaders != null)
			{
				foreach (var header in StandardRequestHeaders)
					// Only add it if it isn't overwritten
					if (additionalHeaders?.ContainsKey(header.Key) != true)
						request.AddHeader(header.Key, header.Value);
			}

			if (parameterPosition == HttpMethodParameterPosition.InBody)
			{
				var contentType = apiClient.requestBodyFormat == RequestBodyFormat.Json ? Constants.JsonContentHeader : Constants.FormContentHeader;
				if (parameters.Any())
					WriteParamToBody(apiClient, request, parameters, contentType);
				else
					request.SetContent(apiClient.requestBodyEmptyContent, contentType);
			}

			return request;
		}

		protected void WriteParamToBody(BaseApiClient apiClient, IRequest request, Dictionary<string, object> parameters, string contentType)
		{
			if (apiClient.requestBodyFormat == RequestBodyFormat.Json)
			{
				// Write the parameters as json in the body
				var stringData = JsonConvert.SerializeObject(parameters);
				request.SetContent(stringData, contentType);
			}
			else if (apiClient.requestBodyFormat == RequestBodyFormat.FormData)
			{
				// Write the parameters as form data in the body
				var stringData = parameters.ToFormData();
				request.SetContent(stringData, contentType);
			}
		}

		public OkexRestApiClient Common { get; }

		protected override Error ParseErrorResponse(JToken error)
		{
			if (error["code"] == null || error["msg"] == null)
				return new ServerError(error.ToString());

			return new ServerError((int)error["code"]!, (string)error["msg"]!);
		}

		public new void Dispose()
		{
			base.Dispose();
			Common.Dispose();
		}
	}
}
