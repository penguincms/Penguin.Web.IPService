using Penguin.Web.IPServices.Arin;
using Penguin.Web.IPServices.Arin.Readers;
using Penguin.Web.IPServices.Objects;
using Penguin.Web.Objects;
using Penguin.Web.Registrations;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Penguin.Web.IPServices
{
    public abstract class ArinBaseService
    {
        public string OrgPath { get; set; }
        public string NetPath { get; set; }
        internal ArinBaseService(string orgPath, string netPath)
        {
            OrgPath = orgPath;
            NetPath = netPath;
        }
        public Blacklist BlackList { get; set; } = new Blacklist();

        public BlacklistStatus CheckIP(string address) => CheckIP(IPRegistration.ParseIp(address));
        public BlacklistStatus CheckIP(IPAddress address)
        {
            BigInteger toCheck = IPRegistration.IpToInt(address);

            BlacklistStatus toReturn = new BlacklistStatus();

            if (!(BlackList?.IsLoaded ?? false))
            {
                toReturn.State = BlacklistState.NotLoaded;
            }
            else
            {
                toReturn.State = BlacklistState.Pass;

                foreach (IPAnalysis analysis in this.BlackList.Analysis)
                {
                    if (analysis.IsMatch(toCheck))
                    {
                        toReturn.Matches.Add(analysis);
                        toReturn.State = BlacklistState.Fail;
                    }
                }
            }

            return toReturn;
        }


        public static bool CheckProperty(string value, string against, MatchMethod method)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            switch (method)
            {
                case MatchMethod.Regex:
                    if (Regex.IsMatch(value, against))
                    {
                        return true;
                    }
                    break;
                case MatchMethod.Contains:
                    if (value.Contains(against))
                    {
                        return true;
                    }
                    break;
                case MatchMethod.Exact:
                    if (string.Equals(value, against))
                    {
                        return true;
                    }
                    break;
                case MatchMethod.CaseInsensitiveContains:                   
                    if (value.IndexOf(against, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return true;
                    }
                    break;
                default:
                    throw new NotImplementedException($"{nameof(MatchMethod)} value {method.ToString()} is not implemented");

            }

            return false;
        }
    }
}
