using System.Numerics;

namespace Penguin.Web.Registrations
{
    internal interface IIPRegistration
    {
        bool IsMatch(BigInteger IPAddress);
    }
}