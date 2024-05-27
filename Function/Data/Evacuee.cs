using System;
namespace EvacAlert.Data
{
    public class Evacuee
    {
        public string Identifier { get; set; }
        public string Group { get; set; }
        public Coordinate Coordinate { get; set; }
        public string EvacAlertId { get; set; }
        public string EvacAlertName { get; set; }
        public string EvacAlertType { get; set; }
        public string EvacAlertOrderStatus { get; set; }

        /// <summary>
        /// The UTC timestamp that this evacuee status was determined.
        /// This is useful for determining state changes if required. 
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

