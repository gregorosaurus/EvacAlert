using System;
namespace EvacAlert.Data
{
    public class AddressData
    {
        public string Identifier { get; set; }
        public string Address { get; set; }
        /*public string Street { get; set; }
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
        }*/
    }
}

