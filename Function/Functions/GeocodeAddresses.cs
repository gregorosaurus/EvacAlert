using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using CsvHelper;
using System.Globalization;
using EvacAlert.Data;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Files.DataLake;

namespace EvacAlert.Functions
{
    public class GeocodeAddresses
    {
        private Services.IGeoCodingService _geocodingService;
        private GeocodeStorageOptions _geocodeStorageOptions;
        private ILogger<GeocodeAddresses> _logger;

        public class GeocodeStorageOptions
        {
            public string ConnectionString { get; set; }
            public string OutputFilePath { get; set; }
            public string Container { get; set; }
        }


        public GeocodeAddresses(Services.IGeoCodingService geocodingService,
            GeocodeStorageOptions geocodeStorageOptions,
            ILogger<GeocodeAddresses> logger)
        {
            _geocodingService = geocodingService;
            _geocodeStorageOptions = geocodeStorageOptions;
            _logger = logger;
        }

        [FunctionName("GeocodeAddressesStorage")]
        public async Task GeocodeAddressesStorageRequest(
           [BlobTrigger("data/upload/addresses.csv", Connection = "DataLakeConnectionString")] Stream blobStream,
           ILogger log)
        {
            log.LogInformation("Geocoding addresses from storage.");

            DataLakeServiceClient client = new DataLakeServiceClient(_geocodeStorageOptions.ConnectionString);
            DataLakeFileSystemClient fileSystem = client.GetFileSystemClient(_geocodeStorageOptions.Container);
            DataLakeDirectoryClient generatedDirectory = fileSystem.GetDirectoryClient(Path.GetDirectoryName(_geocodeStorageOptions.OutputFilePath));
            DataLakeFileClient outputFile = generatedDirectory.GetFileClient(Path.GetFileName(_geocodeStorageOptions.OutputFilePath));


            List<AddressData> addresses;
            TextReader textReader = new StreamReader(blobStream);
            using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
            {
                addresses = csvReader.GetRecords<AddressData>().ToList();
            }

            _logger.LogInformation($"Read in {addresses.Count} to geocode");

            List<GeocodedData> results = await _geocodingService.GeocodeAddressAsync(addresses);

            _logger.LogInformation($"geocoded in {results.Count} addresses");

            using (Stream outputStream = await outputFile.OpenWriteAsync(overwrite: true))
            using (TextWriter tw = new StreamWriter(outputStream))
            {
                var json = System.Text.Json.JsonSerializer.Serialize(results);
                await tw.WriteAsync(json);
            }

        }

        [FunctionName("GeocodeAddresses")]
        public async Task<IActionResult> GeocodeAddressesApiRequest(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("Geocoding addresses.");

            List<AddressData> addresses;
            string reqContent = "";
            using (StreamReader sr = new StreamReader(req.Body))
            {
                reqContent = await sr.ReadToEndAsync();
            }

            if (req.ContentType == "text/plain" || req.ContentType == "text/csv")
            {
                TextReader textReader = new StringReader(reqContent);
                using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
                {
                    addresses = csvReader.GetRecords<AddressData>().ToList();
                }
            }
            else if (req.ContentType == "application/json")
            {
                addresses = System.Text.Json.JsonSerializer.Deserialize<List<AddressData>>(reqContent);
            }
            else
            {
                return new BadRequestObjectResult(new { ErrorMessage = "Invalid content type." });
            }

            List<GeocodedData> results = await _geocodingService.GeocodeAddressAsync(addresses);

            return new OkObjectResult(results);

        }
    }
}

