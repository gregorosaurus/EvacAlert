using System;
namespace EvacAlert.Data
{
    public class GeocodedData
    {
        public string Identifier { get; set; }
        public string Group { get; set; }
        public Coordinate Coordinate { get; set; }
    }
}

