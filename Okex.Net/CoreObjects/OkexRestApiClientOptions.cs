using System.Security;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;

namespace Okex.Net.CoreObjects
{
	/// <summary>
	/// Client Options
	/// </summary>
	public class OkexRestApiClientOptions : RestApiClientOptions
	{

		public OkexRestApiClientOptions(ApiCredentials credentials, SecureString passPhrase, string url, bool isTest = false) : base(url)
		{
			IsTest = isTest;
			ApiCredentials = credentials;
			PassPhrase = passPhrase;
		}

		public bool SignPublicRequests { get; set; } = false;

		public SecureString PassPhrase { get; }
		public readonly bool IsTest;
	}
}
