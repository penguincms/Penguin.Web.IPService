using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    class OrgXmlReader : BlockXmlReader<Org>
    {
        public OrgXmlReader(string filePath, IProgress<float> reportProgress = null, int bufferSize = 10000) : base("org", filePath, bufferSize, reportProgress)
        {
        }
    }
}
