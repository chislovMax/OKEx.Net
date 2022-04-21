using CryptoExchange.Net.Objects;

namespace Okex.Net.CoreObjects
{
    /// <summary>
    /// Client Options
    /// </summary>
    public class OkexClientOptions : RestApiClientOptions
    {
        /// <summary>
        /// Whether public requests should be signed if ApiCredentials are provided. Needed for accurate rate limiting.
        /// </summary>
        public bool SignPublicRequests { get; set; } = false;

        /// <summary>
        /// ctor
        /// </summary>
        public OkexClientOptions(string url, bool isTest = false) : base(url)
        {
        }

        public OkexClientOptions(string url) : base(url)
        {
	        
        }
    }
}
