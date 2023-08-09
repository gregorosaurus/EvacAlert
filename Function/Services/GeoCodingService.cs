using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvacAlert.Data;

namespace EvacAlert.Services
{
    public interface IGeoCodingService
    {
        /// <summary>
        /// geocodes a given set of addresses
        /// </summary>
        /// <param name="address">the address to geocode</param>
        /// <returns>is null if nothing is found</returns>
        public Task<List<GeocodedData>> GeocodeAddressAsync(IEnumerable<AddressData> addresses);
    }
}

