using System;
using EvacAlert.Explore.Data;

namespace EvacAlert.Explore.Services
{
	public interface IStaticLocationInformationService
	{
        Task<List<Facility>> GetFacilitiesAsync();
        Task<List<Region>> GetRegionsAsync();
	}
}

