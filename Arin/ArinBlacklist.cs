using System;
using System.Collections.Generic;
using System.Linq;

namespace Penguin.Web.IPServices.Arin
{
    /// <summary>
    /// Used for defining a blacklisted range of addresses by providing property matches for ARIN registration data
    /// </summary>
    public class ArinBlacklist
    {
        /// <summary>
        /// An individual property match. Can not be used more than once or after list is set
        /// </summary>
        public string Property
        {
            set
            {
                if (!Properties.Any())
                {
                    Properties = new List<string>()
                    {
                        value
                    };
                }
                else
                {
                    throw new Exception("Can not set individual property more than once, or after a property list has been set");
                }
            }
        }

        /// <summary>
        /// A list of property names to match on (TXT and NET use different names for the same property so this is required usually)
        /// </summary>
        public List<string> Properties { get; set; } = new List<string>();

        /// <summary>
        /// The matching value to block
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// The method to use when matching the property
        /// </summary>
        public MatchMethod MatchMethod { get; set; }
    }

    /// <summary>
    /// Used to determine how the services will decide if a property value is a match or not
    /// </summary>
    public enum MatchMethod
    {
        /// <summary>
        /// The match value is a regex expression. Slow obviously
        /// </summary>
        Regex,

        /// <summary>
        /// The match value must be exact
        /// </summary>
        Exact,

        /// <summary>
        /// The property value must only contain the specified value (Case Sensitive)
        /// </summary>
        Contains,

        /// <summary>
        /// The property value must only contain the specified value (Case Insensitive)
        /// </summary>
        CaseInsensitiveContains
    }
}