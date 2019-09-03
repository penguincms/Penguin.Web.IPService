using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal abstract class IPRegistration : IIPRegistration
    {
        public string Source { get; set; }

        public abstract bool IsMatch(string IPAddress);
    }
}
