using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    public class NetReader : BlockReader<Net>
    {
        public NetReader(string filePath, int bufferSize = 2000) : base("net", filePath, bufferSize)
        {

        }
    }
}
