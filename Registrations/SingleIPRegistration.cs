using System.Numerics;

namespace Penguin.Web.Registrations
{
    internal class SingleIPRegistration : IPRegistration
    {
        public SingleIPRegistration(string IP)
        {
            this.Source = IP;
        }

        public override bool IsMatch(BigInteger IPAddress)
        {
            return IpToInt(ParseIp(this.Source)).Equals(IPAddress);
        }
    }
}