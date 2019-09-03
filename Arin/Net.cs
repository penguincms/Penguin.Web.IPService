using Penguin.Reflection.Serialization.XML.Classes;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Penguin.Web.IPServices.Arin
{
    public class Net
    {
        public string Ref { get; set; }
        public IPAddress EndAddress { get; set; }
        public string Handle { get; set; }
        public string Name { get; set; }
        public List<NetBlock> NetBlocks { get; set; }
        public List<PocLink> PocLinks { get; set; }
        public string OrgHandle { get; set; }
        public DateTime RegistrationDate { get; set; }
        public IPAddress StartAddress { get; set; }
        public DateTime UpdateDate { get; set; }
        public int Version { get; set; }
        public List<string> comment { get; set; }
    }
}
