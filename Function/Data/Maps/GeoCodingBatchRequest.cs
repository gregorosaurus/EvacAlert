using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace EvacAlert.Data.Maps
{
	public class GeoCodingBatchRequest
	{
		[JsonPropertyName("batchItems")]
		public List<MapAddressRequest> BatchItems { get; set; } = new List<MapAddressRequest>();
	}
}

