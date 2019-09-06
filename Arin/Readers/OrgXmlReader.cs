using System;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal class OrgXmlReader : BlockXmlReader<Org>
    {
        public OrgXmlReader(string filePath, IProgress<float> reportProgress = null, int bufferSize = 10000) : base("org", filePath, bufferSize, reportProgress)
        {
        }
    }
}