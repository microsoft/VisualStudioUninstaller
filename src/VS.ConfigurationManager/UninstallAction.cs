using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    ///   This class is being created to handle any post uninstall actions that are required. In the
    ///   case of Dev14, this will handle the /force issues found. If multiple post uninstall
    ///   actions are required, a list can be used to establish dependencies.
    /// </summary>
    public class UninstallAction
    {
        /// <summary>
        /// Constructor to evaluate all values to default values.
        /// </summary>
        public UninstallAction()
        {
            Template = TemplateType.Unset;
            Architectures = new List<ArchitectureConfiguration>(); 
            ProductCode = null;
            WixObject = WixObjectType.Unset;
            OS = new List<OperatingSystemConfiguration>();
        }
        /// <summary>
        /// Constructor to handle the creation of an uninstall action with all information passed in.
        /// </summary>
        /// <param name="os"></param>
        /// <param name="template"></param>
        /// <param name="arch"></param>
        /// <param name="productcode"></param>
        /// <param name="wixobj"></param>
        public UninstallAction(ICollection<OperatingSystemConfiguration> os, TemplateType template, ICollection<ArchitectureConfiguration> arch, string productcode, WixObjectType wixobj)
        {
            Template = template;
            Architectures = arch;
            ProductCode = productcode;
            WixObject = wixobj;
            OS = os;
        }

        /// <summary>
        /// Defined to use either bundle or MSI exec installer
        /// </summary>
        public enum WixObjectType {
            /// <summary>
            /// Default value for Enum
            /// </summary>
            Unset,
            /// <summary>
            /// Represents a bundle for this step
            /// </summary>
            Bundle,
            /// <summary>
            /// Represents an MSI for this step
            /// </summary>
            MSI,
            /// <summary>
            /// Represents an MSU for this step
            /// </summary>
            MSU
        }

        /// <summary>
        /// Identify when this step should run; before or after the uninstall process
        /// </summary>
        public enum TemplateType {
            /// <summary>
            /// Default value for Enum
            /// </summary>
            Unset,
            /// <summary>
            /// Run this uninstall action before the main uninstall process
            /// </summary>
            Pre,
            /// <summary>
            /// Run this uninstall action after the main uninstall process
            /// </summary>
            Post
        }

        /// <summary>
        /// What architectures is this uninstall action valid for
        /// </summary>
        public enum Arch {
            /// <summary>
            /// Default value for Enum
            /// </summary>
            Unset,
            /// <summary>
            /// Only valid on x86 configurations
            /// </summary>
            x86,
            /// <summary>
            /// Only valid on 64-bit configurations
            /// </summary>
            x64
        }

        private string _productcode;
        /// <summary>
        /// Maps to the installed product code of the WixObjectType
        /// </summary>
        public string ProductCode {
            get
            {
                return _productcode;
            }
            set
            {
                _productcode = value == null ? String.Empty : value;
            }
        }

        /// <summary>
        /// Is this a bundle or an MSI
        /// </summary>
        public WixObjectType WixObject { get; set; }

        /// <summary>
        /// Does this run before or after the normal uninstall process
        /// </summary>
        public TemplateType Template { get; set; }

        /// <summary>
        /// What architectures is this valid on?
        /// </summary>
        public ICollection<ArchitectureConfiguration> Architectures { get; set; }

        /// <summary>
        /// What OS version is this valid on?
        /// </summary>
        public ICollection<OperatingSystemConfiguration> OS { get; set; }

         /// <summary>
        /// Create an uninstall action with the given parameters
        /// </summary>
        /// <param name="archs"></param>
        /// <param name="oses"></param>
        /// <param name="productcode"></param>
        /// <param name="template"></param>
        /// <param name="objecttype"></param>
        /// <returns></returns>
        public static UninstallAction CreateUninstallAction(ICollection<ArchitectureConfiguration> archs, ICollection<OperatingSystemConfiguration> oses, string productcode, UninstallAction.TemplateType template, UninstallAction.WixObjectType objecttype)
        {
            var ua = new UninstallAction();
            foreach (ArchitectureConfiguration arch in archs) { ua.Architectures.Add(arch); }
            foreach (OperatingSystemConfiguration os in oses) { ua.OS.Add(os); }
            ua.ProductCode = productcode;
            ua.Template = template;
            ua.WixObject = objecttype;

            return ua;
        }
    }
}