using System;
namespace EvacAlert.Data
{
    public class Address
    {
        public string Identifier { get; set; }
        public string Street { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }

        public string AddressQuery
        {
            get
            {
                return string.Join(", ",
                    Street, City, Province, PostalCode);
            }
        }
    }
}

