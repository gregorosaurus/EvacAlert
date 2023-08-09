using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using GeoJSON.Text.Feature;

namespace EvacAlert.Data.Maps
{
	public class GeoCodingBatchResponse
	{
		[JsonPropertyName("batchItems")]
		public List<FeatureCollection> BatchItems { get; set; }

		[JsonPropertyName("nextLink")]
		public string NextLink { get; set; }
	}
}

