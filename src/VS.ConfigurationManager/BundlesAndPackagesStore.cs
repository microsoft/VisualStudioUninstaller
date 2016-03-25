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
        /// HashSet of UpgradeCode; we should use UpgradeCode to do package search.
        /// </summary>
        public HashSet<string> UpgradeCodeHash { get; set; }

        /// <summary>
        /// HashSet of ProductCode; we should use ProductCode to do package search if there's no UpgradeCode is set.
        /// </summary>
        public HashSet<string> NoUpgradeCodeProductCodeHash { get; set; }

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
