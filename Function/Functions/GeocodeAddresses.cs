﻿using System;
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
using System.Text;

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
           [BlobTrigger("data/upload/{name}.csv", Connection = "DataLakeConnectionString")] Stream blobStream,
           string name,
           ILogger log)
        {

            log.LogInformation("Geocoding addresses from storage.");

            DataLakeServiceClient client = new DataLakeServiceClient(_geocodeStorageOptions.ConnectionString);
            DataLakeFileSystemClient fileSystem = client.GetFileSystemClient(_geocodeStorageOptions.Container);
            DataLakeDirectoryClient generatedDirectory = fileSystem.GetDirectoryClient(_geocodeStorageOptions.OutputFilePath);

            //backwards compatibility
            string filename = $"geocoded_{name}.json";
            DataLakeFileClient outputFile = generatedDirectory.GetFileClient(filename);


            List<AddressData> addresses;
            TextReader textReader = new StreamReader(blobStream);
            using (CsvReader csvReader = new CsvReader(textReader, CultureInfo.InvariantCulture))
            {
                addresses = csvReader.GetRecords<AddressData>().ToList();
            }

            Dictionary<string, GeocodedData> existingGeoCodeData = await ReadExistingGeoCodeDataAsync(outputFile);

            //determine new addresses, which match the identifier and the hash.
            List<AddressData> newAddresses = addresses
                .Where(x => !existingGeoCodeData.ContainsKey(x.Identifier) || //if we've never set this identifier
                (existingGeoCodeData[x.Identifier].AddressHash != null /*ignore if we've never set the hash, this will be set later*/
                && existingGeoCodeData[x.Identifier].AddressHash != Crypto.GenerateHash(x.Address)))
                .ToList();

            _logger.LogInformation($"Total addresses read read: {addresses.Count}. New addresses to geocode: {newAddresses.Count}");

            List<GeocodedData> results = await _geocodingService.GeocodeAddressAsync(newAddresses);

            _logger.LogInformation($"geocoded in {results.Count} addresses");

            //merge the results.
            foreach (GeocodedData geocodedAddress in results)
            {
                if (existingGeoCodeData.ContainsKey(geocodedAddress.Identifier))
                    existingGeoCodeData[geocodedAddress.Identifier] = geocodedAddress;
                else
                    existingGeoCodeData.Add(geocodedAddress.Identifier, geocodedAddress);

                //always set the hash here
                AddressData address = addresses.Where(x => x.Identifier == geocodedAddress.Identifier).FirstOrDefault();
                if (address != null)
                {  //should always be the case
                    existingGeoCodeData[geocodedAddress.Identifier].AddressHash = Crypto.GenerateHash(address.Address);
                }
            }

            using (Stream outputStream = await outputFile.OpenWriteAsync(overwrite: true))
            using (TextWriter tw = new StreamWriter(outputStream))
            {
                try
                {
                    //write all geocoded addresses back to blob
                    var json = System.Text.Json.JsonSerializer.Serialize(existingGeoCodeData.Select(x => x.Value).ToList());
                    await tw.WriteAsync(json);
                }
                catch(Exception e)
                {
                    _logger.LogError($"Could not save geocoded addresses: {e.Message} {e.StackTrace}");
                    throw; 
                }
            }

        }

        private async Task<Dictionary<string, GeocodedData>> ReadExistingGeoCodeDataAsync(DataLakeFileClient gecodeOutputFile)
        {
            if (!(await gecodeOutputFile.ExistsAsync()))
                return new Dictionary<string, GeocodedData>();

            List<GeocodedData> existingGeocodedData = new List<GeocodedData>();
            //we read in the output file so that we can only geocode NEW addresses
            using (Stream geocodedStream = await gecodeOutputFile.OpenReadAsync())
            using (StreamReader sr = new StreamReader(geocodedStream))
            {
                string existingGeoCodedJson = await sr.ReadToEndAsync();
                if (string.IsNullOrEmpty(existingGeoCodedJson))
                    return new Dictionary<string, GeocodedData>(); //no data

                existingGeocodedData = System.Text.Json.JsonSerializer.Deserialize<List<GeocodedData>>(existingGeoCodedJson, new System.Text.Json.JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<GeocodedData>();
            }
            //convert to a dictionary
            Dictionary<string, GeocodedData> existingGeoCodedDataDictionary = existingGeocodedData
                .GroupBy(x => x.Identifier)
                .ToDictionary(x => x.Key, x => x.First());

            return existingGeoCodedDataDictionary;
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

