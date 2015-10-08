using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    /// Defining an architecture as x86 or 64-bit
    /// </summary>
    public class ArchitectureConfiguration
    {
        /// <summary>
        /// Setting the property value
        /// </summary>
        /// <param name="value"></param>
        public ArchitectureConfiguration(string value) { Value = value; }
        /// <summary>
        /// Property for holding value
        /// </summary>
        public string Value { get; set; }

        private static readonly List<ArchitectureConfiguration> _archlist = new List<ArchitectureConfiguration>();
        /// <summary>
        /// 64-bit property for use in the UninstallAction class
        /// </summary>
        public static ArchitectureConfiguration x64 { get { return new ArchitectureConfiguration("x64"); } }
        /// <summary>
        /// 32-bit property for use in the UninstallAction class
        /// </summary>
        public static ArchitectureConfiguration x86 { get { return new ArchitectureConfiguration("x86"); } }
        /// <summary>
        /// Generating a list of Architectures supported.
        /// </summary>
        public static ICollection<ArchitectureConfiguration> Architectures()
        {
            Logger.Log("Creating list of architectures", Logger.MessageLevel.Information, "ArchitectureConfiguration");
            if (_archlist == null)
            {
                _archlist.Add(x64);
                _archlist.Add(x86);
            }
            return _archlist;
        }
    }

}
