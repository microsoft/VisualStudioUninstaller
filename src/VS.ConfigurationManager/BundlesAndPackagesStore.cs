using Microsoft.VS.ConfigurationManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    /// Storing and loading list of bundles and packages.
    /// </summary>
    [Serializable()]
    public class BundlesAndPackagesStore
    {
        /// <summary>
        /// Dictionary of UpgradeCode to Package mappings.
        /// </summary>
        public Dictionary<string, Package> UpgradeCodeToPackageDictionary { get; set; }

        /// <summary>
        /// A list of packages without upgrade code.
        /// </summary>
        public List<Package> NoUpgradeCodePackages { get; set; }

        /// <summary>
        /// A list of bundles.
        /// </summary>
        public List<Bundle> Bundles
        {
            get; set;
        }

        /// <summary>
        /// DELETE
        /// </summary>
        public List<Bundle> Releases
        {
            get; set;
        }
    }
}
