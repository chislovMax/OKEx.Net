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
		public OkexRestApiClientOptions(ApiCredentials credentials, SecureString passPhrase, string url, bool isTest = false, bool isSsl = true) : base(url)
		{
			IsSsl = isSsl;
			IsTest = isTest;
			ApiCredentials = credentials;
			PassPhrase = passPhrase;
		}

		public bool SignPublicRequests { get; set; } = false;

		public SecureString PassPhrase { get; }
		public readonly bool IsTest;
		public readonly bool IsSsl;
	}
}
