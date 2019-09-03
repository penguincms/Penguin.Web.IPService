using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.IPServices.Arin.Readers
{
    class OrgReader : BlockReader<Org>
    {
        public OrgReader(string filePath, int bufferSize = 2000) : base("org", filePath, bufferSize)
        {
        }
    }
}
