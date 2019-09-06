using System;

namespace Penguin.Web.IPServices.Arin.Readers
{
    internal class NetXmlReader : BlockXmlReader<Net>
    {
        public NetXmlReader(string filePath, IProgress<float> reportProgress = null, int bufferSize = 10000) : base("net", filePath, bufferSize, reportProgress)
        {
        }
    }
}