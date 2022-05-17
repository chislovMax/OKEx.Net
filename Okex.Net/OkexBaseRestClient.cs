using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Okex.Net.Clients;
using Okex.Net.CoreObjects;

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

			log.Write(LogLevel.Information, $"[{requestId}] Creating request for " + uri);
			var paramsPosition = parameterPosition ?? apiClient.ParameterPositions[method];
			var request = ConstructRequest(apiClient, uri, method, parameters, signed, paramsPosition,
				arraySerialization ?? apiClient.arraySerialization, requestId, additionalHeaders);
			if (_isTest && !useProd)
			{
				request.AddHeader("x-simulated-trading", "1");
			}

			string? paramString = "";
			if (paramsPosition == HttpMethodParameterPosition.InBody)
				paramString = $" with request body '{request.Content}'";

			var headers = request.GetHeaders();
			if (headers.Any())
				paramString += " with headers " +
									string.Join(", ", headers.Select(h => h.Key + $"=[{string.Join(",", h.Value)}]"));


			apiClient.TotalRequestsMade++;
			log.Write(LogLevel.Trace,
				$"[{requestId}] Sending {method}{(signed ? " signed" : "")} request to {request.Uri}{paramString ?? " "}{(ClientOptions.Proxy == null ? "" : $" via proxy {ClientOptions.Proxy.Host}")}");
			return await GetResponseAsync<T>(apiClient, request, deserializer, cancellationToken, false).ConfigureAwait(false);

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
