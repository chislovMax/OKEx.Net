using Newtonsoft.Json;
using Okex.Net.Enums;

namespace Okex.Net.Models
{
	public class OkexSystemStatus : AbstractOkexModel
	{
		[JsonProperty("title")]
		public string Title { get; set; }
		[JsonProperty("state")]
		public OkexMaintenanceStateEnum State { get; set; }
		[JsonProperty("begin")]
		public long Begin { get; set; }
		[JsonProperty("end")]
		public long End { get; set; }
		[JsonProperty("href")]
		public string Href { get; set; }
		[JsonProperty("serviceType")]
		public OkexServiceTypeEnum ServiceType { get; set; }
		[JsonProperty("system")]
		public OkexAccountTypeEnum System { get; set; }
		[JsonProperty("scheDesc")]
		public string RescheduledDescription { get; set; }
	}
}
