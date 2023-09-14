using System;
namespace EvacAlert.Explore.Data
{
	public class Region
	{
        public string? RegionName { get; set; }
        public List<BoundingArea> BoundingAreas { get; set; } = new List<BoundingArea>();
    }
}

