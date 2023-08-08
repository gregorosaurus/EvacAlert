using System;
using System.Threading.Tasks;
using EvacAlert.Data;

namespace EvacAlert.Services
{
    public interface IGeoCodingService
    {
        /// <summary>
        /// geocodes a given address query
        /// </summary>
        /// <param name="address">the address to geocode</param>
        /// <returns>is null if nothing is found</returns>
        public Task<GeocodedData> GeocodeAddressAsync(string name, string group, string address);
    }
}

