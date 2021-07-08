using System.Numerics;

namespace Penguin.Web.Registrations
{
    internal class RangeRegistration : IPRegistration
    {
        public BigInteger From { get; set; }

        public BigInteger To { get; set; }

        public RangeRegistration(string Range)
        {
            string start = Range.Split('-')[0];
            string end = Range.Split('-')[1];

            this.From = IpToInt(ParseIp(start));
            this.To = IpToInt(ParseIp(end));
        }

        public override bool IsMatch(BigInteger IPAddress)
        {
            return IPAddress >= this.From && IPAddress <= this.To;
        }
    }
}