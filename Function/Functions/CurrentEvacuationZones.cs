using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace EvacAlert.Functions
{
    public class CurrentEvacuationZones
    {
        private Services.IEvacuationService _evacService;
        public CurrentEvacuationZones(Services.IEvacuationService evacService)
        {
            _evacService = evacService;
        }

        [FunctionName("CurrentEvacZones")]
        public async Task<IActionResult> RetrieveCurrentEvacZones(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
            return new OkObjectResult(await _evacService.GetCurrentEvacuationAreasAsync());
        }
    }
}

