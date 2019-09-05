using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class NetXmlReader : BlockXmlReader<Net>
    {
        public NetXmlReader(string filePath, IProgress<float> reportProgress = null, int bufferSize = 10000) : base("net", filePath, bufferSize, reportProgress)
        {

        }
    }
}
