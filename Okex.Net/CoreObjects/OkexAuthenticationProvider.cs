using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using Okex.Net.Helpers;

namespace Okex.Net.CoreObjects
{
	public class OkexAuthenticationProvider : AuthenticationProvider
	{
		private readonly SecureString? _passPhrase;
		private readonly bool _signPublicRequests;

		public OkexAuthenticationProvider(ApiCredentials credentials, SecureString passPhrase, bool signPublicRequests) : base(credentials)
		{
			if (credentials?.Secret == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret needed.");

			_passPhrase = passPhrase;
			_signPublicRequests = signPublicRequests;
		}

		private readonly string BodyParameterKey = "<BODY>";

		public override void AuthenticateRequest(RestApiClient apiClient, Uri uri, HttpMethod method, Dictionary<string, object> parameters, bool signed,
			ArrayParametersSerialization arraySerialization, HttpMethodParameterPosition parameterPosition,
			out SortedDictionary<string, object> uriParameters, out SortedDictionary<string, object> bodyParameters, out Dictionary<string, string> headers)
		{
			uriParameters = parameterPosition == HttpMethodParameterPosition.InUri
				? new SortedDictionary<string, object>(parameters)
				: new SortedDictionary<string, object>();

			bodyParameters = parameterPosition == HttpMethodParameterPosition.InBody
				? new SortedDictionary<string, object>(parameters)
				: new SortedDictionary<string, object>();

			headers = new Dictionary<string, string>();

			if (!signed && !_signPublicRequests)
				return;

			if (Credentials?.Key == null || _passPhrase == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret/PassPhrase needed.");
			var uriString = uri.ToString();

			var time = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
			var signtext = time + method.Method.ToUpper() + uriString.Replace("https://www.okx.com", "").Trim('?');

			if (method == HttpMethod.Post)
			{
				if (parameters.Count == 1 && parameters.Keys.First() == BodyParameterKey)
				{
					var bodyString = JsonConvert.SerializeObject(parameters[BodyParameterKey]);
					signtext += bodyString;
				}
				else
				{
					var bodyString = JsonConvert.SerializeObject(parameters.ToDictionary(p => p.Key, p => p.Value));
					signtext += bodyString;
				}
			}

			var signature = Encryptor.HmacSHA256(signtext, Credentials.Secret.GetString());

			headers.Add("OK-ACCESS-KEY", Credentials.Key.GetString());
			headers.Add("OK-ACCESS-SIGN", signature);
			headers.Add("OK-ACCESS-TIMESTAMP", time);
			headers.Add("OK-ACCESS-PASSPHRASE", _passPhrase.GetString());
		}

		public static string Base64Encode(byte[] plainBytes)
		{
			return System.Convert.ToBase64String(plainBytes);
		}
	}
}
