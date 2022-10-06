using CryptoExchange.Net;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security;
using Microsoft.Extensions.Logging;
using Okex.Net.Helpers;

namespace Okex.Net.CoreObjects
{
	public class OkexAuthenticationProvider : AuthenticationProvider
	{
		private const string AccessKeyHeaderName = "OK-ACCESS-KEY";
		private const string AccessSignHeaderName = "OK-ACCESS-SIGN";
		private const string AccessTimestampHeaderName = "OK-ACCESS-TIMESTAMP";
		private const string AccessPassPhraseHeaderName = "OK-ACCESS-PASSPHRASE";

		private ILogger? _logger;
		private readonly bool _signPublicRequests;
		private readonly SecureString? _passPhrase;

		public OkexAuthenticationProvider(ApiCredentials credentials, SecureString passPhrase, bool signPublicRequests, bool isSsl, ILogger? logger = null) : base(credentials)
		{
			if (credentials?.Secret == null)
				throw new ArgumentException("No valid API credentials provided. Key/Secret needed.");

			_logger = logger;
			_passPhrase = passPhrase;
			_signPublicRequests = signPublicRequests;
			_replacementBaseUrl = isSsl ? "https://www.okx.com" : "http://www.okx.com";
		}

		private readonly string BodyParameterKey = "<BODY>";
		private readonly string _replacementBaseUrl;

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

			var uriStringReplacement = uriString
				.Replace("http://www.okx.com", "")
				.Replace("https://www.okx.com", "")
				.Trim('?');

			var time = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now).ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
			var signtext = time + method.Method.ToUpper() + uriStringReplacement;

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

			//_logger?.LogTrace($"\nuriString: {uriString}\nuriStringReplacement: {uriStringReplacement}\nsigntext: {signtext}");

			var signature = Encryptor.HmacSHA256(signtext, Credentials.Secret.GetString());

			headers.Add(AccessKeyHeaderName, Credentials.Key.GetString());
			headers.Add(AccessSignHeaderName, signature);
			headers.Add(AccessTimestampHeaderName, time);
			headers.Add(AccessPassPhraseHeaderName, _passPhrase.GetString());
		}

		public static string Base64Encode(byte[] plainBytes)
		{
			return System.Convert.ToBase64String(plainBytes);
		}
	}
}
