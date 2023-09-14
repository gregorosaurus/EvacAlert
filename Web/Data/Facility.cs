using System;
namespace EvacAlert.Explore.Data
{
    public class Facility
    {
        public int Id { get; set; }
        public string? FacilityName { get; set; }
        public string? FullAddress { get; set; }
        public string? LocalityName { get; set; }
        public double Longitude { get; set; }
        public double Latitude { get; set; }
    }
}

