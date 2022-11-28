using System;
namespace EvacAlert.Explore.Data
{
    public class EvacuationArea
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public DateTime DateModified { get; set; }
        public List<BoundingArea> BoundingAreas { get; set; } = new List<BoundingArea>();

        public string? IssuingAgency { get; set; }
        public string? EventType { get; set; }
        public int? NumberOfHomesAffected { get; set; }
        public string? OrderStatus { get; set; }
    }
}

