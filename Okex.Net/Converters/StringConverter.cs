using System;
using System.Globalization;
using Newtonsoft.Json;

namespace Okex.Net.Converters
{
	public class StringConverter : JsonConverter<string>
	{
		public override string ReadJson(JsonReader reader, Type objectType, string existingValue, bool hasExistingValue,
			JsonSerializer serializer)
		{
			return existingValue;
		}

		public override void WriteJson(JsonWriter writer, string value, JsonSerializer serializer)
		{
			writer.WriteValue(Decimal.Parse(value, NumberStyles.Any, CultureInfo.InvariantCulture));
		}
	}
}
