using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class NetXmlReader : BlockXmlReader<Net>
    {
        public NetXmlReader(string filePath, int bufferSize = 10000) : base("net", filePath, bufferSize)
        {

        }
    }
}
