using Penguin.Reflection.Serialization.XML.Classes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin
{
    class Org
    {
        public string Ref { get; set; }
        public string City { get; set; }
        public Iso3166_1 Iso3166_1 { get; set; }
        public string Handle { get; set; }
        public string Customer { get; set; }
        public string Name { get; set; }
        public List<PocLink> PocLinks { get; set; }
        public string PostalCode { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string Iso3166_2 { get; set; }
        public List<string> StreetAddress { get; set; }
        public DateTime UpdateDate { get; set; }

    }
}
