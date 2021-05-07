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
		private readonly SecureString? PassPhrase;
		private readonly HMACSHA256 encryptor;
		private readonly bool signPublicRequests;
		private readonly ArrayParametersSerialization arraySerialization;
		private readonly bool _isTest;

		public OkexAuthenticationProvider(ApiCredentials credentials, string passPhrase, bool signPublicRequests, ArrayParametersSerialization arraySerialization, bool isTest = false) : base(credentials)
		{
			_isTest = isTest;

			if (credentials == null || credentials.Secret == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret needed.");

			PassPhrase = passPhrase.ToSecureString();
			encryptor = new HMACSHA256(Encoding.ASCII.GetBytes(credentials.Secret.GetString()));
			this.signPublicRequests = signPublicRequests;
			this.arraySerialization = arraySerialization;
		}

		public override Dictionary<string, string> AddAuthenticationToHeaders(string uri, HttpMethod method, Dictionary<string, object> parameters, bool signed, PostParameters postParameterPosition, ArrayParametersSerialization arraySerialization)
		{
			if (!signed && !signPublicRequests)
				return new Dictionary<string, string>();

			if (Credentials == null || Credentials.Key == null || PassPhrase == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret/PassPhrase needed.");

			var time = (DateTime.UtcNow.ToUnixTimeMilliSeconds() / 1000.0m).ToString(CultureInfo.InvariantCulture);
			var signtext = time + method.Method.ToUpper() + uri.Replace("https://www.okex.com", "").Trim('?');

			if (method == HttpMethod.Post)
			{
				if (parameters.Count == 1 && parameters.Keys.First() == OkexClient.BodyParameterKey)
				{
					var bodyString = JsonConvert.SerializeObject(parameters[OkexClient.BodyParameterKey]);
					signtext = signtext + bodyString;
				}
				else
				{
					var bodyString = JsonConvert.SerializeObject(parameters.OrderBy(p => p.Key).ToDictionary(p => p.Key, p => p.Value));
					signtext = signtext + bodyString;
				}
			}

			var signature = HmacSHA256(signtext, Credentials.Secret?.GetString());

			var authParams = new Dictionary<string, string> {
					 { "OK-ACCESS-KEY", Credentials.Key.GetString() },
					 { "OK-ACCESS-SIGN", signature },
					 { "OK-ACCESS-TIMESTAMP", time },
					 { "OK-ACCESS-PASSPHRASE", PassPhrase.GetString() },
				};

			if (_isTest)
			{
				authParams.Add("x-simulated-trading", "1");
			}

			return authParams;
		}
		
		public static string Base64Encode(byte[] plainBytes)
		{
			return System.Convert.ToBase64String(plainBytes);
		}

		public static string Base64Encode(string plainText)
		{
			var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
			return System.Convert.ToBase64String(plainTextBytes);
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

		public static string Base64Decode(string base64EncodedData)
		{
			var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
			return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
		}
	}
}
