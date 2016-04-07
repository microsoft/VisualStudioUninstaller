using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Globalization;
using System.Security.Permissions;


namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    /// Representation of a WiX MSI with pertinent information for running an uninstall.
    /// </summary>
    [Serializable()]
    public class Package
    {
        private const string AppName = "Package";

        static private string systemdir;
        static private string temp;
        static private string LogLocation;

        /// <summary>
        /// Definition of what type of Package will be set.  Uninstall directives will be different for each.
        /// </summary>
        public enum PackageType
        {
            /// <summary>
            /// MSI type
            /// </summary>
            MSI,
            /// <summary>
            /// MSU type
            /// </summary>
            MSU,
            /// <summary>
            /// Redistributables or other bundles
            /// </summary>
            EXE
        }
        #region Public Properties

        /// <summary>
        /// When was the MSI installed
        /// </summary>
        public DateTime InstallDate { get; set; }
        /// <summary>
        /// Location of file that initiated the installation
        /// </summary>
        public string InstallLocation { get; set; }
        /// <summary>
        /// WiX MSI product code
        /// </summary>
        public String ProductCode { get; set; }
        /// <summary>
        /// WiX MSI upgrade code
        /// </summary>
        public String UpgradeCode { get; set; }
        /// <summary>
        /// Wix MSI Product Name
        /// </summary>
        public string ProductName { get; set; }
        /// <summary>
        /// Wix MSI Product Version
        /// </summary>
        public string ProductVersion { get; set; }
        /// <summary>
        /// Wix MSI URL value in the case of download
        /// </summary>
        public System.Uri Url { get; set; }
        /// <summary>
        /// Wix MSI Chaining Package value
        /// </summary>
        public string ChainingPackage { get; set; }
        /// <summary>
        /// What set of uninstall instructions should be used is defined by the type of installer that was provided.
        /// </summary>
        public PackageType Type { get; set; }

        private string msiuninstallarguments = @"/qn /norestart IGNOREDEPENDENCIES=ALL ";
        private const string msiEXEname = @"msiexec.exe";
        private string msuuninstallarguments = @"/quiet /norestart /uninstall /kb:";
        private const string MSUEXEname = @"wusa.exe";

        #endregion Public Properties

        #region Public Constructors
        /// <summary>
        /// Constructor to initialize variables necessary for the rest of configuration
        /// </summary>
        public Package()
        {
            Initialize();
        }

        /// <summary>
        /// Passing in all fields on creation of an instance
        /// </summary>
        /// <param name="upgradecode"></param>
        /// <param name="productcode"></param>
        /// <param name="productversion"></param>
        /// <param name="productname"></param>
        /// <param name="chainingpackage"></param>
        /// <param name="installDate"></param>
        /// <param name="installLocation"></param>
        /// <param name="url"></param>
        public Package(string upgradecode, string productcode, string productversion, string productname, string chainingpackage, DateTime installDate, string installLocation, System.Uri url)
        {
            Initialize();
            Type = PackageType.MSI;
            UpgradeCode = upgradecode;
            ProductCode = productcode;
            ProductVersion = productversion;
            ProductName = productname;
            ChainingPackage = chainingpackage;
            InstallDate = installDate;
            InstallLocation = installLocation;
            Url = url;
        }

        /// <summary>
        /// Passing in all fields on creation of an instance
        /// </summary>
        /// <param name="productcode"></param>
        /// <param name="productversion"></param>
        /// <param name="productname"></param>
        /// <param name="chainingpackage"></param>
        /// <param name="installDate"></param>
        /// <param name="installLocation"></param>
        /// <param name="url"></param>
        public Package(string productcode, string productversion, string productname, string chainingpackage, DateTime installDate, string installLocation, System.Uri url)
        {
            Initialize();
            Type = PackageType.MSI;
            ProductCode = productcode;
            ProductVersion = productversion;
            ProductName = productname;
            ChainingPackage = chainingpackage;
            InstallDate = installDate;
            InstallLocation = installLocation;
            Url = url;
        }
        /// <summary>
        /// Overloaded value to allow a different value for PackageType to be set.
        /// </summary>
        /// <param name="productcode"></param>
        /// <param name="productversion"></param>
        /// <param name="productname"></param>
        /// <param name="chainingpackage"></param>
        /// <param name="installDate"></param>
        /// <param name="installLocation"></param>
        /// <param name="url"></param>
        /// <param name="type"></param>
        public Package(string productcode, string productversion, string productname, string chainingpackage, DateTime installDate, string installLocation, System.Uri url, PackageType type)
        {
            Initialize();
            Type = type;
            ProductCode = productcode;
            ProductVersion = productversion;
            ProductName = productname;
            ChainingPackage = chainingpackage;
            InstallDate = installDate;
            InstallLocation = installLocation;
            Url = url;
        }
        #endregion Public Constructors

        #region Public Methods
        /// <summary>
        ///      <para> Override for the MSI uninstall command line installation. </para>
        ///      - /x command is already in place.
        ///      - /L*V command is already in place.
        /// </summary>
        public string MSUUninstallArguments
        {
            get { return msuuninstallarguments; }
            set { msuuninstallarguments = value.TrimEnd() + " "; }
        }

        /// <summary>
        ///      <para> Override for the MSI uninstall command line installation. </para>
        ///      - /x command is already in place.
        ///      - /L*V command is already in place.
        /// </summary>
        public string MSIUninstallArguments
        {
            get { return msiuninstallarguments; }
            set { msiuninstallarguments = value.TrimEnd() + " "; }
        }
        /// <summary>
        /// Returns the product name of this instantiation
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ProductName;
        }

        /// <summary>
        ///      Initiating an uninstall from the MSI to ensure that force uninstall is first run.
        ///      Once that has been completed, there can be MSIs that are left behind.
        /// </summary>
        /// <returns></returns>

        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust", Unrestricted = false)]
        public int Uninstall()
        {
            var exitcode = -1;
            var args = string.Empty;
            var file = string.Empty;
            switch (this.Type)
            {
                case PackageType.MSI:
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Installer: {0}", this.ProductName), Logger.MessageLevel.Information, AppName);
                    var msilogfilename = System.IO.Path.ChangeExtension(LogLocation + "_" + this.ProductName.Replace(" ", string.Empty).ToString(), "log");
                    // Run msiexec from the system path only.
                    file = System.IO.Path.Combine(systemdir, msiEXEname);
                    // Quiet uninstall with no restart requested and logging enabled
                    args = String.Format(CultureInfo.InvariantCulture, MSIUninstallArguments + "/x {0} /L*v \"{1}\"", this.ProductCode.ToString(), "dd_" + msilogfilename);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Arguments: {0}", args));

                    exitcode = Utility.ExecuteProcess(file, args);
                    if (exitcode == 0)
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI [{0}] Uninstall succeeded", this.ProductName), Logger.MessageLevel.Information, AppName);
                    else
                    {
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI [{0}] Uninstall failed with error code: {1}", this.ProductName, exitcode), Logger.MessageLevel.Information, AppName);
                    }
                    break;
                case PackageType.MSU:
                    Utility.ServiceAction("wuauserv", Utility.ServiceState.Stop);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "KB{0} - {1}", this.ProductCode, this.ProductName), Logger.MessageLevel.Information, AppName);
                    args = String.Format(CultureInfo.InvariantCulture, msuuninstallarguments.TrimEnd() + "{0}", this.ProductCode);
                    file = System.IO.Path.Combine(systemdir, MSUEXEname);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Arguments: {0}", args), Logger.MessageLevel.Information, AppName);
                    exitcode = Utility.ExecuteProcess(file, args);
                    if (
                            (exitcode == 0) ||          // success
                            (exitcode == 3010) ||       // reboot required
                            (exitcode == 2359303)       // already uninstalled
                        )
                    {
                        var msg = String.Empty;
                        switch (exitcode)
                        {
                            case 3010:
                                msg = "(Reboot required)";
                                break;
                            case 2359303:
                                exitcode = 0; // Override error message from the MSU to signal success
                                msg = "(Previously uninstalled)";
                                break;
                        }

                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSU [KB{0}] Uninstall succeeded {1}", this.ProductCode, msg), Logger.MessageLevel.Information, AppName);
                    }
                    else
                    {
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSU [KB{0}] Uninstall failed with error code: {1}", this.ProductCode, exitcode), Logger.MessageLevel.Information, AppName);
                    }
                    Utility.ServiceAction("wuauserv", Utility.ServiceState.Stop);
                    break;
                case PackageType.EXE:
                    break;
            }
            return exitcode;
        }

        #endregion Public Methods

        static private void Initialize()
        {
            systemdir = Environment.SystemDirectory;
            temp = System.IO.Path.GetTempPath();
            LogLocation = System.IO.Path.Combine(temp, @"Uninstall");
        }
    }
}
