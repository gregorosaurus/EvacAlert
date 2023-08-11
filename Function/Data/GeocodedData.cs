using System;
namespace EvacAlert.Data
{
    public class GeocodedData
    {
        public string Identifier { get; set; }
        public string Group { get; set; }
        public Coordinate Coordinate { get; set; }
        /// <summary>
        /// used to determine if the address is new
        /// </summary>
        public string AddressHash { get; set; }
    }
}

