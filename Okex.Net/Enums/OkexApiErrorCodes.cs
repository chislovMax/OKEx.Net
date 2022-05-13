namespace Okex.Net.Enums
{
	public enum OkexApiErrorCodes
	{
		Unknown = -1,


		RequestTooFrequentHttpCode = 429,
		SystemUpgrading = 503,

		InvalidParams = 51000,

		RequestTooFrequent = 50011,
		InvalidAuthorization = 50114,
		OrderOverPricing = 51116,
		LowBalance = 51119,
		OrderDoesNotExist = 51603,
		SystemMaintenance = 50001,
		RequestTimeout = 50004,
		ApiOffline = 50005,
		AccountBlocked = 50007,
		AccountBlockedByLiquidation = 50009,
		AccountStatusInvalid = 50012,
		SystemIsBusy = 50013,
		SystemError = 50026,
		InvalidOKAccessTimestamp = 50112,
		InvalidInstrument = 51001,

		OrderAmountExceedsCurrentTierLimit = 51004,
		OrderAmountExceedsTheLimit = 51005,
		OrderPriceOutOfTheLimit = 51006,
		OrderMinAmount = 51007,
		OrderLowBalance = 51008,
		OrderPlacementBLocked = 51009,
		OperationIsNotSupported = 51010,
		NotMatchInstrumentType = 51015,
		OrderAmountMustBeGreaterAvailableBalance = 51020,
		InstrumentExpired = 51027,
		OrderQuantityLess = 51120,
		OrderCountMustBeInteger = 51121,
		OrderPriceHigherThenMinPrice = 51122,
		InsufficientBalance = 51131,
		MarketOrderSizeToLarge = 51201,
		OrderAmountExceedsMax = 51202,
		OrderAmountExceedsLimit = 51203,
		OrderAlreadyCompleted = 51402,
		CurrencyNotSupportedBySavingsAccount = 58003,
	}
}
