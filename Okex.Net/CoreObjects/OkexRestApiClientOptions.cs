using CryptoExchange.Net.Objects;

namespace Okex.Net.CoreObjects
{
    /// <summary>
    /// Client Options
    /// </summary>
    public class OkexRestApiClientOptions : RestApiClientOptions
    {
        /// <summary>
        /// Whether public requests should be signed if ApiCredentials are provided. Needed for accurate rate limiting.
        /// </summary>
        public bool SignPublicRequests { get; set; } = false;

        /// <summary>
        /// ctor
        /// </summary>
        public OkexRestApiClientOptions(string url, bool isTest = false) : base(url)
        {
        }

        public OkexRestApiClientOptions(string url) : base(url)
        {
	        
        }
    }
}
