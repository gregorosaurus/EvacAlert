using System;
namespace EvacAlert.Data
{
    public class Evacuee
    {
        public string Identifier { get; set; }
        public Coordinate Coordinate { get; set; }
        public string EvacAlertId { get; set; }
        public string EvacAlertName { get; set; }
        public string EvacAlertType { get; set; }
    }
}

