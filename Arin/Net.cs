using System;
using System.Collections.Generic;

namespace Penguin.Web.IPServices.Arin
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    public class Net
    {
        public string Ref { get; set; }
        public string EndAddress { get; set; }
        public string Handle { get; set; }
        public string Name { get; set; }
        public List<NetBlock> NetBlocks { get; set; }
        public List<PocLink> PocLinks { get; set; }
        public string OrgHandle { get; set; }
        public DateTime RegistrationDate { get; set; }
        public string StartAddress { get; set; }
        public DateTime UpdateDate { get; set; }
        public int Version { get; set; }
        public List<string> Comment { get; set; }
    }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}