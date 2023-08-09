using System;
using System.Text.Json.Serialization;

namespace EvacAlert.Data.Maps
{
	public class MapAddressRequest
	{
		[JsonPropertyName("addressLine")]
		public string Address { get; set; }

		[JsonPropertyName("top")]
		public int Top { get; set; } = 1;
	}
}

