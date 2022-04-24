using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CryptoExchange.Net;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json.Linq;
using Okex.Net.Clients;
using Okex.Net.CoreObjects;

namespace Okex.Net
{
	public class OkexBaseRestClient : BaseRestClient, IDisposable
	{
		public OkexBaseRestClient(BaseRestClientOptions options, OkexRestApiClientOptions okexRestApiOptions) : base("OKEX", options)
		{
			Common = AddApiClient(new OkexRestApiClientClient(this, options, okexRestApiOptions));
		}

		internal Task<WebCallResult<T>> SendRequestInternal<T>(RestApiClient apiClient, Uri uri, HttpMethod method, CancellationToken cancellationToken,
			Dictionary<string, object>? parameters = null, bool signed = false, HttpMethodParameterPosition? postPosition = null,
			ArrayParametersSerialization? arraySerialization = null, int weight = 1, bool ignoreRateLimit = false) where T : class
		{
			return base.SendRequestAsync<T>(apiClient, uri, method, cancellationToken, parameters, signed, postPosition, arraySerialization, requestWeight: weight, ignoreRatelimit: ignoreRateLimit);
		}

		public OkexRestApiClientClient Common { get; }

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
