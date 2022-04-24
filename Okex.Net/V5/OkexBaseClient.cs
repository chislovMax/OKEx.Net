using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json.Linq;
using Okex.Net.CoreObjects;
using Okex.Net.V5.Clients;

namespace Okex.Net.V5
{
	public class OkexBaseClient : BaseRestClient
	{
		public OkexBaseClient(BaseRestClientOptions options, OkexRestApiClientOptions okexRestApiOptions) : base("OKEX", options)
		{
			Common = AddApiClient(new OkexClientV5(this, options, okexRestApiOptions));
		}

		internal Task<WebCallResult<T>> SendRequestInternal<T>(RestApiClient apiClient, Uri uri, HttpMethod method, CancellationToken cancellationToken,
			Dictionary<string, object>? parameters = null, bool signed = false, HttpMethodParameterPosition? postPosition = null,
			ArrayParametersSerialization? arraySerialization = null, int weight = 1, bool ignoreRateLimit = false) where T : class
		{
			return base.SendRequestAsync<T>(apiClient, uri, method, cancellationToken, parameters, signed, postPosition, arraySerialization, requestWeight: weight, ignoreRatelimit: ignoreRateLimit);
		}

		protected override Task<ServerError?> TryParseErrorAsync(JToken error)
		{
			if (error["code"] == null || error["msg"] == null)
				return Task.FromResult(new ServerError(error.ToString()));

			return Task.FromResult(new ServerError((int)error["code"]!, (string)error["msg"]!));
		}

		public OkexClientV5 Common { get; }

		public void DisposeClient()
		{
			Common.DisposeClient();
			Dispose();
		}
	}
}
