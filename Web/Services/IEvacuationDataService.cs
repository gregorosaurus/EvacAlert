using System;
using EvacAlert.Explore.Data;

namespace EvacAlert.Explore.Services
{
    public interface IEvacuationDataService
    {
        Task<List<EvacuationArea>> GetEvacuationAreasAsync();
    }
}

