using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace Okex.Net.Helpers
{
	public static class Encryptor
	{
		public static string HmacSHA256(string infoStr, string secret)
		{
			var sha256Data = Encoding.UTF8.GetBytes(infoStr);
			var secretData = Encoding.UTF8.GetBytes(secret);
			using var hmacsha256 = new HMACSHA256(secretData);
			var buffer = hmacsha256.ComputeHash(sha256Data);

			return Convert.ToBase64String(buffer);
		}

		public static string MakeSign(string apiKey, string secret, string phrase)
		{
			var timeStamp = (DateTime.UtcNow - new DateTime(1970, 1, 1)).Ticks / 10000000;
			var sign = HmacSHA256($"{timeStamp}GET/users/self/verify", secret);
			var info = new
			{
				op = "login",
				args = new List<Dictionary<string, string>>()
				{
					new Dictionary<string, string>()
					{
						{ "apiKey",apiKey},{"passphrase",phrase},{"timestamp",timeStamp.ToString() },{"sign",sign}
					}
				}
			};
			return JsonConvert.SerializeObject(info);
		}
	}
}
