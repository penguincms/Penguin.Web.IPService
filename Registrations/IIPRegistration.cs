using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal interface IIPRegistration
    {
        bool IsMatch(IPAddress IPAddress);
    }
}
