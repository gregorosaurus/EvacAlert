using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EvacAlert.Data;

namespace EvacAlert.Services
{
    public interface IEvacuationService
    {
        Task<List<EvacuationArea>> GetCurrentEvacuationAreasAsync();
    }
}

