namespace Okex.Net.Models
{
	public class ErrorMessage
	{
		public ErrorMessage(string code, string message)
		{
			Code = code;
			Message = message;
		}

		public string Code { get; set; }
		public string Message { get; set; }
	}
}
