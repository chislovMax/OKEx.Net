namespace Okex.Net.Models
{
	public abstract class AbstractOkexModel
	{
		public virtual string Code { get; set; } = "0";
		public virtual string Message { get; set; } = "";
	}
}
