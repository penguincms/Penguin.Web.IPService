using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Penguin.Web.IPServices.Arin
{
    public class NetBlock
    {
        public int CidrLenth { get; set; }
        public string EndAddress { get; set; }
        public string Type { get; set; }
        public string StartAddress { get; set; }
    }
}
