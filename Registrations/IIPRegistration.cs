using System;
using System.Collections.Generic;
using System.Text;

namespace Penguin.Web.Registrations
{
    internal interface IIPRegistration
    {
        bool IsMatch(string IPAddress);
    }
}
