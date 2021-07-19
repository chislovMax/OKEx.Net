namespace Okex.Net.V5.Enums
{
	public enum OkexApiErrorCodes
	{
		Unknown = -1,

		InvalidParams = 51000,
		RequestTooFrequent = 50011,
		InvalidAuthorization = 50114,
		LowBalance = 51119,
		OrderDoesNotExist = 51603,
	}
}
