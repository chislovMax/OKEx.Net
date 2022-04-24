using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using Okex.Net.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Security.Cryptography;
using System.Text;

namespace Okex.Net.CoreObjects
{
	public class OkexAuthenticationProvider : AuthenticationProvider
	{
		private readonly SecureString? _passPhrase;
		private readonly bool _signPublicRequests;
		private readonly bool _isTest;

		public OkexAuthenticationProvider(ApiCredentials credentials, SecureString passPhrase, bool signPublicRequests, ArrayParametersSerialization arraySerialization, bool isTest = false) : base(credentials)
		{
			_isTest = isTest;

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

			headers = new Dictionary<string, string>() ;
			if (_isTest)
			{
				headers.Add("x-simulated-trading", "1");
			}

			if (!signed && !_signPublicRequests)
				return;

			if (Credentials?.Key == null || _passPhrase == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret/PassPhrase needed.");
			var uriString = uri.ToString();
			var time = (DateTime.UtcNow.ToUnixTimeMilliSeconds() / 1000.0m).ToString(CultureInfo.InvariantCulture);
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
					var bodyString = JsonConvert.SerializeObject(parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
					signtext += bodyString;
				}
			}

			var signature = HmacSHA256(signtext, Credentials.Secret?.GetString());

			headers.Add("OK-ACCESS-KEY", Credentials.Key.GetString());
			headers.Add("OK-ACCESS-SIGN", signature);
			headers.Add("OK-ACCESS-TIMESTAMP", time);
			headers.Add("OK-ACCESS-PASSPHRASE", _passPhrase.GetString());
		}

		public static string Base64Encode(byte[] plainBytes)
		{
			return System.Convert.ToBase64String(plainBytes);
		}

		public string HmacSHA256(string infoStr, string secret)
		{
			byte[] sha256Data = Encoding.UTF8.GetBytes(infoStr);
			byte[] secretData = Encoding.UTF8.GetBytes(secret);
			using (var hmacsha256 = new HMACSHA256(secretData))
			{
				byte[] buffer = hmacsha256.ComputeHash(sha256Data);
				return Convert.ToBase64String(buffer);
			}
		}
	}
}
