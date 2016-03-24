using Microsoft.VS.ConfigurationManager.Support;
using Microsoft.Deployment.WindowsInstaller;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using Wix = Microsoft.Tools.WindowsInstallerXml;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    /// Publically available actions for handling uninstall actions
    /// </summary>
    public class Primitives : IDisposable
    {

        #region Public Properties
        /// <summary>
        /// Current machine OS version
        /// </summary>
        public string MachineOSVersion { get; set; }
        /// <summary>
        /// Current machine architecture
        /// </summary>
        public ArchitectureConfiguration MachineArchitectureConfiguration { get; set; }
        /// <summary>
        /// List of installed MSIs on this machine when (Processed)
        /// </summary>
        public ICollection<Package> InstalledPackages
        {
            get { return InstalledPackages; }
        }

        /// <summary>
        /// List of releases supported by this application when (Processed)
        /// </summary>
        public ICollection<Bundle> Releases
        {
            get { return releases; }
        }

        /// <summary>
        /// Flag for ensuring data is loaded successfully.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        ///      Used to debug on development machine. Prevents execution of bundle uninstall and
        ///      msi uninstalls.
        /// </summary>
        public bool DoNotExecuteProcess { get; set; }

        /// <summary>
        ///      The location where you want files written to when creating output files.
        /// </summary>
        public string DataFilesPath { get; set; }

        /// <summary>
        ///      string of releases that can be passed in to initiate an uninstall for.
        /// </summary>
        public bool DebugReporting
        {
            get { return debugreporting; }
            set
            {
                debugreporting = value;
                Logger.Debug = value;
            }
        }
        /// <summary>
        /// A set of uninstall actions that will be done either before or after the main uninstall process
        /// </summary>
        public ICollection<UninstallAction> UninstallActions
        {
            get { return uninstallactions; }
        }
        /// <summary>
        /// List of filters (search and replace) to be applied to text
        /// </summary>
        public ICollection<Filter> Filters
        {
            get { return filters; }
        }

        /// <summary>
        /// Accepts a List of Filter (class) object that provides search and replace on for
        /// text in product and msi names. Helps shorten output. Progress
        /// indicator value when running an uninstall
        /// </summary>
        public decimal Progress { get; set; }

        /// <summary>
        ///      Provides a string that reports on the releases supported and selected
        /// </summary>
        public string ReleaseOutput { get; set; }

        #endregion Public Properties

        #region Public Methods

        /// <summary>
        ///      Using the List of Filter classes, do a replace of strings per the user's
        ///      specification.
        /// </summary>
        /// <param name="Source"></param>
        /// <returns></returns>
        public string ApplyFilter(string Source)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Applying filters"), Logger.MessageLevel.Verbose, AppName);
            foreach (Filter fil in filters) { Source = Source.Replace(fil.ReplaceSource, fil.ReplaceValue); }
            return Source;
        }

        /// <summary>
        /// Explicit disposal of objects
        /// </summary>
        public void Dispose()
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Disposing of {0}", AppName), Logger.MessageLevel.Verbose, AppName);
            Dispose(true);
            GC.SuppressFinalize(this);
            ut.Dispose();
        }

        /// <summary>
        /// Identifies all the selected releases based on the passed in values from the user
        /// </summary>
        /// <param name="userselect"></param>
        public void SelectedReleases(string userselect)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Selection passed: {0}", userselect), Logger.MessageLevel.Information, AppName);
            var selected = userselect.Split(',').Select(i => i.Trim()).ToList();
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Selected releases: {0}", userselect.Count().ToString(CultureInfo.InvariantCulture)));

            foreach (string id in selected)
            {
                // 0-based array
                var pos = Convert.ToInt32(id, CultureInfo.InvariantCulture) - 1;
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Installed: {0} ({1})", Releases.ElementAt(pos).Name, Releases.ElementAt(pos).Installed.ToString()));
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Selected set to true: {0}", Releases.ElementAt(pos).Name));
                if (pos <= this.Releases.Count())
                {
                    this.Releases.ElementAtOrDefault(pos).Selected = true;
                }
            }
        }


        /// <summary>
        /// GetInstalledItems lists all items that are installed on this machine.
        /// </summary>
        /// <returns></returns>
        public ICollection<Package> GetAllInstalledItems
        {
            get
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Getting all installed items", AppName), Logger.MessageLevel.Information, AppName);
                ICollection<Package> installations = new List<Package>();

                try
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Do we already have an object in memory?", AppName), Logger.MessageLevel.Verbose, AppName);
                    if (installedmsis.FirstOrDefault() == null)
                    {
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "No installpackages object found - creating", AppName), Logger.MessageLevel.Verbose, AppName);
                        installations = ProductInstallation.GetProducts(null, null, UserContexts.All)
                                   .Where(ins => ins.ProductName != null)
                                   .Select(ins => new Package(ins.ProductCode,
                                                          ins.ProductVersion.ToString(),
                                                          ApplyFilter(ins.ProductName),
                                                          null,
                                                          (DateTime)ins.InstallDate,
                                                          ins.InstallLocation,
                                                          ins.UrlInfoAbout
                                                        )
                                                        )
                                   .OrderBy(ins => ins.ProductName).ToList();
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Packages installed: {0}", installations.Count().ToString(CultureInfo.InvariantCulture)));
                    }
                    else
                    {
                        Logger.Log("installedpackages is populated.");
                        installations = (List<Package>)installedmsis;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex.Message, Logger.MessageLevel.Error, AppName);
                }
                installedmsis = installations;
                return installations;
            }
        }

        /// <summary>
        ///      <para>
        ///           GetAllInstalledItemsCompareWixPdb lists all items that are installed on this
        ///           machine.
        ///      </para>
        ///      <para>
        ///           It restricts the list to items that are being searched for based on the WixPdb
        ///           configuration that is loaded.
        ///      </para>
        /// </summary>
        /// <returns></returns>
        public ICollection<Bundle> GetAllInstalledItemsCompareWixPdb
        {
            get
            {
                ICollection<Bundle> outBundles = new List<Bundle>();
                Bundle outBundle = null;
                ICollection<Package> installations = null;

                try
                {
                    Logger.Log("GetAllInstalledItemsCompareWixPdb start");
                    installations = this.GetAllInstalledItems;
                    var installableitems = releases.Where(rel => rel.Selected).ToList();
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Total releases: {0}", releases.Count().ToString(CultureInfo.InvariantCulture)), Logger.MessageLevel.Information, AppName);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Selected releases: {0}", releases.Count(rel => rel.Selected == true).ToString(CultureInfo.InvariantCulture)), Logger.MessageLevel.Information, AppName);
                    foreach (Bundle bundle in installableitems)
                    {
                        var  msis = bundle.Packages;
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Populating bundle: {0}", bundle.Name), Logger.MessageLevel.Verbose, AppName);
                        var query = from ins in installations
                                    join installable in msis on ins.ProductCode equals installable.ProductCode
                                    select new Package
                                    {
                                        InstallDate = ins.InstallDate,
                                        ProductName = ins.ProductName,
                                        InstallLocation = ins.InstallLocation,
                                        ProductCode = ins.ProductCode,
                                        ProductVersion = ins.ProductVersion,
                                        Url = ins.Url,
                                        ChainingPackage = installable.ChainingPackage
                                    };

                        if (query != null)
                        {
                            outBundle = bundle;
                            outBundle.Packages.Clear();
                            foreach (Package package in query)
                            {
                                outBundle.Packages.Add(package);
                            }
                            outBundles.Add(outBundle);
                        }
                        else
                        {
                            Logger.Log(" - GetAllInstalledItemsCompareWixPdb - Query resulted in no results", Logger.MessageLevel.Information, AppName);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
                Logger.Log("GetAllInstalledItemsCompareWixPdb end", Logger.MessageLevel.Information, AppName);
                return outBundles;
            }
        }

        /// <summary>
        /// Pull information from WixPdbs unless processing has already happened.  getall overrides this behavior.
        /// </summary>
        /// <param name="getall"></param>
        /// <returns></returns>
        public ICollection<Bundle> GetDataFromPdb(bool getall = true)
        {
            var bun = GetDataFromPdb(releases, getall);
            return bun;
        }

        /// <summary> <para>This function returns a list of installable items based on a
        /// pre-selected list of releases passed in.</para> <para>It can take an array of releases
        /// or a single release. Pass in no value for release and it will return for all configured
        /// releases.</para> <para>string rel - Single release passed results in limiting the list
        /// to that release</para> </summary> <param name="rel"></param>
        public ICollection<Bundle> GetDataFromPdb(string rel)
        {
            var rels = new List<Bundle>();
            rels = releases.Where(x => x.ReleasePdb == rel).ToList();
            var bun = GetDataFromPdb(rels, false);
            return bun;
        }

        /// <summary> <para>This function returns a list of installable items based on a
        /// pre-selected list of releases passed in.</para> <para>It can take an array of releases
        /// or a single release. Pass in no value for release and it will return for all configured
        /// releases.</para> <para>string[] rel - Passing multiple items will iterate through all
        /// matched WixPdbs</para> </summary> <param name="rel"></param>
        public ICollection<Bundle> GetDataFromPdb(string[] rel)
        {
            var rels = new List<Bundle>();
            foreach (string releasename in rel)
            {
                rels.Add(releases.FirstOrDefault(x => x.ReleasePdb == releasename && x.FileType == FILETYPE_WIXPDB));
            }
            var bun = GetDataFromPdb(rels, false);
            return bun;
        }

        /// <summary>
        ///      Produces a string that lists all the releases that are being parsed from the
        ///      WixPdb.
        /// </summary>
        /// <returns></returns>
        public string GetReleases
        {
            get
            {
                Logger.Log("GetRelease started", Logger.MessageLevel.Information, AppName);
                string releasestring;
                var sb = new StringBuilder();
                var i = 1;

                foreach (Bundle rel in releases)
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Adding bundle: {0}", rel.Name), Logger.MessageLevel.Verbose, AppName);
                    var installvalue = new StringBuilder();
                    if (rel.Installed) { installvalue.Append("Installed"); }
                    if (rel.Selected)
                    {
                        installvalue.Append(String.IsNullOrEmpty(installvalue.ToString()) ? string.Empty : ", ");
                        installvalue.Append("Selected");
                    }
                    installvalue.Insert(0, "[");
                    installvalue.Append("]");

                    sb.Append(i.ToString(CultureInfo.InvariantCulture).PadLeft(2) + ". " + ApplyFilter(rel.Name).PadRight(55));
                    sb.Append(installvalue.ToString());
                    sb.AppendLine();
                    i++;
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Added bundle: {0}", rel.Name), Logger.MessageLevel.Verbose, AppName);
                }

                releasestring = sb.ToString();
                Logger.Log("GetRelease ended", Logger.MessageLevel.Information, AppName);
                return releasestring;
            }
        }

        /// <summary>
        ///      Locate PDB files on disk and store them in the release object
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Get all files that are in the content directory. Record them as a Bundle for
                // later usage. Additionally, check the install state of each Bundle.
                releases = new List<Bundle>();
                GetContent(ref releases, DataFilesPath);
                ReleaseOutput = String.IsNullOrEmpty(releaseoutput) ? GetReleases : releaseoutput;
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Initialize called successfully."), Logger.MessageLevel.Information, AppName);
                Processed = false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Logger.MessageLevel.Error, AppName);
            }
        }

        /// <summary>
        ///      <para>
        ///           Given a file that has been serialized out, this will read in the file and
        ///           hydrate the object model for a list of InstallableItem.
        ///      </para>
        ///      <para>
        ///           It specifically takes a directory to read all BIN files associated. If an
        ///           invalid BIN file is found, it will report an error and the loop will continue.
        ///      </para>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Bundle LoadFromFile(string value)
        {
            var outObj = FileToBundle(value);
            Processed = true;
            return outObj;
        }

        /// <summary>
        /// Load all files from a given directory into a collection of bundle objects
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public ICollection<Bundle> LoadFromFiles(string value = null)
        {
            return FilesToBundles(string.IsNullOrEmpty(value) ? DataFilesPath : value);
        }
        /// <summary>
        ///      <para>
        ///           Takes a list of InstallableItem and converts it to binary output.
        ///      </para>
        ///      <para>
        ///           This is used after data is extracted from a wixpdb using
        ///           Primitive.GetDataFromPdb()
        ///      </para>
        /// </summary>
        /// <param name="installable"></param>
        public void Save(ICollection<Bundle> installable)
        {
            if (Processed)
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "File path: {0} Files saved: {1}", this.DataFilesPath, installable.Count().ToString(CultureInfo.InvariantCulture)));
                BundlesToFiles(installable, this.DataFilesPath, FILETYPE_BIN);
            }
            else
            {
                Logger.Log("WixPdbs have not been processed or the configuration files have not been loaded. Nothing to save.", Logger.MessageLevel.Warning, AppName);
            }
        }

        /// <summary>
        /// Releases object serialized to disk
        /// </summary>
        public void SaveAll()
        {
            Save(Releases);
        }

        /// <summary>
        /// Uninstall a specific WiX MSI
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust", Unrestricted = false)]
        public int Uninstall(Package package)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Package uninstall initiated: {0}", package.ProductName), Logger.MessageLevel.Information, AppName);
            var exitcode = -1;

            try
            {
                if (!DoNotExecuteProcess)
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Package uninstall method called"), Logger.MessageLevel.Verbose, AppName);
                    exitcode = package.Uninstall();
                }
                else
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Package uninstall method bypassed - DoNotExecute is true"), Logger.MessageLevel.Verbose, AppName);
                    exitcode = 0;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, AppName);
            }

            return exitcode;
        }

        /// <summary>
        /// Uninstall a specific WiX bundle
        /// </summary>
        /// <param name="bundle"></param>
        /// <returns></returns>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust", Unrestricted = false)]
        public int Uninstall(Bundle bundle)
        {
            var exitcode = -1;
            var uninstallactionerrorcode = -1;
            if (!Processed) { GetDataFromPdb(); }

            if (Releases.Where(x=>x.Selected) != null)
            {
                try
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Pre-requisite uninstall actions running"), Logger.MessageLevel.Information, AppName);
                    uninstallactionerrorcode = UninstallActionExecution(bundle, UninstallAction.TemplateType.Pre);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Pre-requisite uninstall actions finished"), Logger.MessageLevel.Information, AppName);
                    switch (uninstallactionerrorcode)
                    {
                        case 3010:  // Reboot required
                            break;
                        default:
                            if (!DoNotExecuteProcess)
                            {
                                exitcode = bundle.Uninstall();
                            }
                            else
                            {
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Bundle uninstall method bypassed - DoNotExecute is true"), Logger.MessageLevel.Verbose, AppName);
                                exitcode = 0;
                            }
                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Post-requisite uninstall actions started"), Logger.MessageLevel.Information, AppName);
                            UninstallActionExecution(bundle, UninstallAction.TemplateType.Post);
                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Post-requisite uninstall actions finished"), Logger.MessageLevel.Information, AppName);
                            break;
                    }

                }
                catch (Exception ex)
                {
                    Logger.Log(ex, AppName);
                }
            }
            else {
                throw new ConfigurationManagerException("A release has not been selected for use with this method. Please use SelectedReleases to select a Release.");
            }

            return exitcode;
        }

        /// <summary>
        /// Given a list of bundles, run the uninstall on the bundles in sequence
        /// </summary>
        /// <param name="bundles"></param>
        /// <returns></returns>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust", Unrestricted = false)]
        public int Uninstall(ICollection<Bundle> bundles)
        {
            var exitcode = -1;
            if (Releases.Where(x => x.Selected) != null)
            {
                try
                {
                    foreach (Bundle bundle in bundles.Where(x => x.Installed))
                    {
                        exitcode = Uninstall(bundle);
                        if (exitcode != 0) { break; }
                    }
                    exitcode = 0;
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            else
            {
                throw new Exception("A release has not been selected for use with this method.  Property UserSelectedReleases needs to be set.");
            }
            return exitcode;
        }

        private int UninstallActionExecution(ICollection<Bundle> bundles, UninstallAction.TemplateType template)
        {
            var exitcode = -1;
            // Only iterate through selected bundles
            foreach (Bundle bundle in bundles)
            {
                exitcode = UninstallActionExecution(bundle, template);
            }

            return exitcode;
        }

        private int UninstallActionExecution(Bundle bundle, UninstallAction.TemplateType template)
        {
            var DidInstallRun = false;
            var errorcode = 0;
            var currenterrorcode = 0;
            var rebootrequired = false;
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Starting Template Type: {0} Bundle: {1}", template.ToString(), bundle.Name), Logger.MessageLevel.Information, AppName);
            ICollection<UninstallAction> _uas = UninstallActions.Where(x => x.Template == template).ToList();
            // For each uninstall item, only select those that are either before or after the main uninstall process
            // and match the machine OS and Architecture.  This is defined by the template parameter.
            // Only for given machine architecture
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall actions found: {0}", _uas.Count().ToString(CultureInfo.InvariantCulture)), Logger.MessageLevel.Verbose, AppName);

            foreach (UninstallAction ua in _uas)
            {
                foreach (ArchitectureConfiguration ac in ua.Architectures)
                {
                    if (ac.Value == MachineArchitectureConfiguration.Value)
                    {
                        // Only get one OS version that matches to integer value from the OS
                        foreach (OperatingSystemConfiguration osc in ua.OS)
                        {
                            if (osc.Value == MachineOSVersion)
                            {
                                // Uninstall the selected package
                                foreach (Package package in bundle.Packages.Where(x => x.ProductCode == ua.ProductCode))
                                {
                                    if (!DoNotExecuteProcess) {
                                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Package found to match architecture and OS: {0}", package.ProductName), Logger.MessageLevel.Information, AppName);
                                        currenterrorcode = package.Uninstall();
                                        DidInstallRun = true;

                                        switch (currenterrorcode)
                                        {
                                            case 0:
                                                errorcode = currenterrorcode;
                                                break;
                                            case 3010:
                                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Reboot required"), Logger.MessageLevel.Information, AppName);
                                                errorcode = currenterrorcode;
                                                rebootrequired = true;
                                                break;
                                            default:
                                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Exitcode: {0}  Returned Error code: {1}", errorcode.ToString(CultureInfo.InvariantCulture), currenterrorcode.ToString(CultureInfo.InvariantCulture)), Logger.MessageLevel.Information, AppName);
                                                errorcode = errorcode == 3010 ? errorcode : currenterrorcode;
                                                break;
                                        }
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (
                    (errorcode != 0 || errorcode != 3010) // Did we get a non-zero error code
                    && DidInstallRun                      // Was an installer run?
                   )                                      // If so, we need to break the loop as the user likely has an action to take
                {
                    // Allow reboot required to drop out like a success.  This will allow all pre-requisite installers to complete regardless of a reboot being required.
                    //  - Break out on all other error codes.  Any other status is a hard error that needs to be handled outside of this process.
                    break;
                }
            }
            errorcode = errorcode != 0 ? errorcode : rebootrequired ? 3010 : errorcode;
            return errorcode;
        }

        #endregion Public Methods

        #region Protected Methods

        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) {
                    releases = null;
                    ut.Dispose();
                }
            }
            //dispose unmanaged resources
            disposed = true;
        }

        #endregion Protected Methods

        #region Private Fields

        private const string CHAINMSIPACKAGE = "ChainMsiPackage";

        private const string UXPACKAGEBEHAVIOR = "UxPackageBehavior";

        private const string FILETYPE_WIXPDB = "WixPdb";

        private const string FILETYPE_BIN = "BIN";

        private const string WIXBUNDLE = "WixBundle";

        private const string AppName = "Primitives";

        private bool debugreporting;

        private bool disposed;

        private ICollection<Filter> filters = new List<Filter>();

        private ICollection<UninstallAction> uninstallactions = new List<UninstallAction>();

        private ICollection<Package> installedmsis = new List<Package>();

        private ICollection<Bundle> releases = new List<Bundle>();

        private string releaseoutput = string.Empty;

        private Utility ut = new Utility();

        #endregion Private Fields

        #region Private Methods
        private static ICollection<Package> GetMSIDataFromTable(Bundle bundle, Wix.Table chainmsipackageTable, Wix.Table uxPackageBehavior)
        {
            try {
                Dictionary<string, string> uxPackageBehaviorDict = new Dictionary<string, string>();
                if (uxPackageBehavior != null)
                {
                    foreach (Wix.Row msirow in uxPackageBehavior.Rows)
                    {
                        string packageId = string.Empty;
                        string reallyPerm = string.Empty;
                        foreach (Wix.Field field in msirow.Fields)
                        {
                            switch (field.Column.Name.ToString(CultureInfo.InvariantCulture).ToUpperInvariant())
                            {
                                case "PackageId":
                                    packageId = field.Data.ToString();
                                    break;
                                case "ReallyPermanent":
                                    reallyPerm = field.Data.ToString();
                                    break;

                            }
                        }
                        if (!string.IsNullOrEmpty(packageId) && !uxPackageBehaviorDict.ContainsKey(packageId))
                        {
                            uxPackageBehaviorDict.Add(packageId, reallyPerm);
                        }
                    }
                }

                foreach (Wix.Row msirow in chainmsipackageTable.Rows)
                {
                    var msi = new Package();

                    foreach (Wix.Field field in msirow.Fields)
                    {
                        switch (field.Column.Name.ToString(CultureInfo.InvariantCulture).ToUpperInvariant())
                        {
                            case "CHAINPACKAGE_":
                                msi.ChainingPackage = field.Data.ToString();
                                break;

                            case "PRODUCTCODE": // id 23
                                msi.ProductCode = field.Data.ToString();
                                break;

                            case "PRODUCTVERSION":
                                msi.ProductVersion = field.Data.ToString();
                                break;

                            case "PRODUCTNAME":
                                msi.ProductName = field.Data.ToString();
                                break;
                            case "PACKAGETYPE":
                                msi.Type = Package.PackageType.MSI;
                                break;
                            default:
                                break;
                        }
                    }

                    // if the package is really perm, then, don't uninstall it.
                    if (!string.IsNullOrEmpty(msi.ChainingPackage)
                        && uxPackageBehaviorDict.ContainsKey(msi.ChainingPackage) 
                        && uxPackageBehaviorDict[msi.ChainingPackage].Equals("yes", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    bundle.Packages.Add(msi);
                }
                // We should not be uninstalling MSU because they are usually perm and windows comp.
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            return bundle.Packages;
        }

        static private Bundle FileToBundle(string file)
        {
            // Generate file name based on configuration and name of the wixpdb
            long position = 0;
            // create a new formatter instance
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            // read the animal as position back
            Bundle installable = null;
            using (FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                if (position < stream.Length)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    installable = (Bundle)formatter.Deserialize(stream);
                    position = stream.Position;
                }
            }
            formatter = null;
            return installable;
        }

        private ICollection<Bundle> FilesToBundles(string directory, string ext = FILETYPE_BIN)
        {
            ICollection<Bundle> outObj = new List<Bundle>();
            var di = new DirectoryInfo(directory);
            try
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Loading files from: {0}", directory), Logger.MessageLevel.Information, AppName);
                Releases.Clear();

                if (!di.Exists)
                {
                    var msg = "Directory provided does not exist!";
                    throw new DirectoryNotFoundException(msg);
                }
                else
                {
                    // Iterate through all the BIN files in the given directory. In case one is
                    // passed in which is incorrect, the ReadFile function is wrapped in a try
                    // catch.
                    var allfiles = System.IO.Directory.GetFiles(directory);
                    var files = Array.FindAll(allfiles, s => s.EndsWith(ext, StringComparison.OrdinalIgnoreCase));

                    foreach (var file in files) { Releases.Add(FileToBundle(file)); }
                }
            }
            catch (DirectoryNotFoundException dex)
            {
                Logger.Log(dex.Message, Logger.MessageLevel.Warning, AppName);
            }
            catch (Exception ex)
            {
                Logger.Log(ex, AppName);
            }
            finally
            {
                di = null;
            }
            ReleaseOutput = GetReleases;
            Processed = true;
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Files loaded", directory), Logger.MessageLevel.Information, AppName);

            return outObj;
        }

        private void GetContent(ref ICollection<Bundle> rels, string path)
        {
            try
            {
                if (Directory.Exists(@"content"))
                {
                    if (Directory.GetFiles(@"content").Length != 0)
                    {
                        ICollection<string> list = new List<string>(Directory.GetFiles(@"content"));

                        rels = new List<Bundle>();
                        foreach (string entry in list)
                        {
                            try
                            {
                                // Get all files associated with WixPDBs in directory
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Loading {0}", entry), Logger.MessageLevel.Information, "Utility");
                                Wix.Pdb.Load(Path.GetFullPath(entry), true, true);
                                var bundlerowinfo = (Wix.WixBundleRow)GetBundlesFromWixPDB(entry).Rows[0];

                                var newrel = new Bundle(bundlerowinfo.BundleId, bundlerowinfo.Name, bundlerowinfo.Version, Path.GetFileNameWithoutExtension(entry), Path.GetFullPath(entry), FILETYPE_WIXPDB, false);
                                rels.Add(newrel);

                                bundlerowinfo = null;
                            }
                            catch(Exception e)
                            {
                                Logger.Log("Unable to load wixpdb: " + entry, Logger.MessageLevel.Error);
                                Logger.Log(e);
                            }
                        }
                    }
                }
                if (Directory.Exists(path) && Directory.GetFiles(path).Length != 0)
                {
                    // Get all already processed files available Get working directory iterate
                    // through remove

                    foreach (string fileitem in Directory.GetFiles(path))
                    {
                        // Does a PDB exist for the file that we are reading?
                        var filenamewithoutextension = Path.GetFileNameWithoutExtension(fileitem);

                        if (releases.Where(x => x.Name == filenamewithoutextension).Count() != 0)
                        {
                            // Update values to use the file instead
                            releases.First(x => x.Name == filenamewithoutextension).FileType = FILETYPE_BIN;
                            releases.First(x => x.Name == filenamewithoutextension).binPath = Path.Combine(path, fileitem);
                        }
                        else // If there is no wixpdb does not exist, pull data from the file to populate releases
                        {
                            try
                            {
                                var tempbundle = FileToBundle(fileitem);

                                var rel = new Bundle(tempbundle.BundleId, tempbundle.Name, tempbundle.Version, "", path, FILETYPE_BIN, false, tempbundle.Packages)
                                {
                                    binPath = Path.Combine(path, fileitem)
                                };
                                rels.Add(rel);
                                tempbundle = null;
                                rel = null;
                            }
                            catch(Exception e)
                            {
                                Logger.Log("Unable to load bundle file: " + fileitem, Logger.MessageLevel.Error);
                                Logger.Log(e);
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
            finally
            {
            }
        }

        private static Wix.Table GetBundlesFromWixPDB(string entry)
        {
            var pdb = Wix.Pdb.Load(Path.GetFullPath(entry), true, false);
            var wixbundle = pdb.Output.Tables[WIXBUNDLE];  //Id: 32 in pdb.Output.Rows
            pdb = null;
            return wixbundle;
        }

        private static void BundlesToFiles(ICollection<Bundle> installable, string DataFilesPath, string ext)
        {
            var path = DataFilesPath ?? System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DataFiles");
            var di = new DirectoryInfo(path);

            if (!di.Exists)
                di.Create();

            string filename = null;

            foreach (Bundle insitem in installable)
            {
                // Generate file name based on configuration and name of the wixpdb
                filename = System.IO.Path.Combine(path, Path.ChangeExtension(insitem.Name, ext));

                // create a new formatter instance
                var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

                // open a filestream
                using (FileStream stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
                {
                    formatter.Serialize(stream, insitem);
                }
            }
        }

        /// <summary> <para>This internal function returns a list of installable items based on a
        /// pre-selected list of releases passed in.</para> <para>It can take an array of releases
        /// or a single release. Pass in no value for release and it will return for all configured
        /// releases.</para>
        /// <para>List of Release (class) rels - Metadata from Initialize procedure filtered
        /// by public calls.</para>
        /// </summary>
        /// <param name="rels"></param>
        /// <param name="getall"></param>
        /// <returns>List Bundle</returns>
        private ICollection<Bundle> GetDataFromPdb(ICollection<Bundle> rels, bool getall)
        {
            ICollection<Bundle> installables = new List<Bundle>();
            Logger.Log("GetDataFromPdb started.");

            Logger.Log(String.Format(CultureInfo.InvariantCulture, " - Select releases: {0}", Releases.Count(x => x.Selected).ToString(CultureInfo.InvariantCulture)));
            Logger.Log(String.Format(CultureInfo.InvariantCulture, " - Get all WixPdbs: {0}", getall.ToString(CultureInfo.InvariantCulture)));
            if (Releases.Count(x => x.Selected) != 0 || getall)
            {
                try
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, " -- Releases: {0}", rels == null ? "Null" : rels.Count().ToString(CultureInfo.InvariantCulture)));
                    if (releases == null || releases.ElementAt(0).Packages == null || getall)
                    {
                        if ((rels == null) || (releases == null))
                        {
                            GetContent(ref releases, DataFilesPath);
                        }

                        if (getall)
                        {
                            rels = Releases;
                        }

                        foreach (Bundle rel in rels)
                        {
                            var filetype = rel.FileType;
                            var filepath = (filetype == FILETYPE_WIXPDB) ? rel.Path : rel.binPath;
                            if (getall)
                            {
                                filetype = FILETYPE_WIXPDB;
                                filepath = rel.Path;
                            }
                            if (File.Exists(filepath))
                            {
                                GetDataFromSource(installables, rel, filetype, filepath);
                            }
                            else
                            {
                                throw new FileNotFoundException(String.Format(CultureInfo.InvariantCulture, "Error File Not Found: {0}", rel.Path), rel.Path);
                            }
                        }
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Installables count: {0}", installables.Count().ToString(CultureInfo.InvariantCulture), AppName));
                        releases = installables;
                    }
                    else
                    {
                        installables = releases;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(ex);
                }
            }
            else
            {
                var msg = "A release has not been selected for use with this method.  Property UserSelectedReleases needs to be set.";
                Logger.Log(msg);
                throw new Exception(msg);
            }

            Processed = true;
            Logger.Log("GetDataFromPdb ended.");
            SaveAll();
            installables = getall ? installables : installables.Where(x => x.Selected).ToList();
            return installables;
        }

        private void GetDataFromSource(ICollection<Bundle> installables, Bundle rel, string filetype, string filepath)
        {
            switch (filetype)
            {
                case FILETYPE_WIXPDB:
                    var pdb = Wix.Pdb.Load(filepath, false, false);
                    if (pdb.Output.Type == Wix.OutputType.Bundle)
                    {
                        var wixbundle = pdb.Output.Tables[WIXBUNDLE];  //Id: 32 in pdb.Output.Rows
                        var chainmsipackageTable = pdb.Output.Tables[CHAINMSIPACKAGE]; //Id: 0 in pdb.Output.Rows
                        var uxPackageBehavior = pdb.Output.Tables[UXPACKAGEBEHAVIOR]; //Id: 0 in pdb.Output.Rows

                        if (wixbundle != null)
                        {
                            var bundlerow = (Wix.WixBundleRow)wixbundle.Rows[0];
                            var bundle = new Bundle(bundlerow.BundleId, bundlerow.Name, bundlerow.Version);
                            bundlerow = null;
                            if (chainmsipackageTable != null)
                            {
                                GetMSIDataFromTable(bundle, chainmsipackageTable, uxPackageBehavior);
                            }
                            bundle.Selected = rel.Selected;
                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Successfully loaded: {0} [{1}]", rel.Name, FILETYPE_WIXPDB));
                            installables.Add(bundle);
                        }
                    }
                    break;

                case FILETYPE_BIN:
                    var binload = LoadFromFile(filepath);

                    rel.FileType = string.IsNullOrEmpty(rel.FileType) ? binload.FileType : rel.FileType;
                    rel.BundleId = rel.BundleId.ToString().Count() == 0 ? binload.BundleId : rel.BundleId;

                    if (rel.Packages == null) // if the Packages object is null, we have no MSIs listed in the bundle
                    {
                        foreach (Package package in binload.Packages)
                        {
                            rel.Packages.Add(package);
                        }
                    }
                    rel.Name = string.IsNullOrEmpty(rel.Name) ? binload.Name : rel.Name;
                    rel.binPath = binload.Path;
                    rel.ReleasePdb = string.IsNullOrEmpty(rel.ReleasePdb) ? binload.ReleasePdb : rel.ReleasePdb;
                    rel.Version = rel.Version == null ? binload.Version : rel.Version;
                    rel.Selected = rel.Selected ? rel.Selected : binload.Selected;

                    installables.Add(rel);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Successfully loaded: {0} [{1}]", rel.Name, FILETYPE_BIN));
                    break;
            }
        }
        #endregion Private Methods
    }
}
