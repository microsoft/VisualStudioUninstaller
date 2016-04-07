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

        public BundlesAndPackagesStore BundlesAndPackagesStore { get; set; }

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
        /// Constructor.
        /// </summary>
        public Primitives()
        {
            this.BundlesAndPackagesStore = new BundlesAndPackagesStore();
            this.BundlesAndPackagesStore.UpgradeCodeHash = new HashSet<string>();
            this.BundlesAndPackagesStore.NoUpgradeCodeProductCodeHash = new HashSet<string>();
            this.BundlesAndPackagesStore.Bundles = new List<Bundle>();
        }

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
        /// GetInstalledItems lists all items that are installed on this machine.
        /// </summary>
        /// <returns></returns>
        public ICollection<Package> GetAllInstalledItems()
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Getting all installed items", AppName), Logger.MessageLevel.Information, AppName);
            ICollection<Package> installations = new List<Package>();

            try
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Do we already have an object in memory?", AppName), Logger.MessageLevel.Verbose, AppName);
                if (installedmsis.FirstOrDefault() == null)
                {
                    var hi = ProductInstallation.GetProducts(null, null, UserContexts.All);
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "No installpackages object found - creating", AppName), Logger.MessageLevel.Verbose, AppName);
                    installations = ProductInstallation.GetProducts(null, null, UserContexts.All)
                               .Where(ins => ins.ProductName != null)
                               .Select(ins => new Package(
                                                      this.GetUpgradeCode(ins.LocalPackage),
                                                      ins.ProductCode,
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

        /// <summary>
        /// Clean up HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\VisualStudio 
        /// Clean up HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\VisualStudio
        /// </summary>
        public void CleanupVisualStudioRegistryHives()
        {
            var keyPaths = new string[] {
                @"SOFTWARE\Microsoft\VisualStudio\12.0",
                @"SOFTWARE\Microsoft\VisualStudio\14.0",
                @"SOFTWARE\Microsoft\VisualStudio\15.0" };

            foreach(var keyPath in keyPaths)
            {
                Logger.LogWithOutput(string.Format("Deleting registry: {0}", keyPath));
                this.DeleteRegistryKey(keyPath);
            }

        }

        private void DeleteRegistryKey(string keyPath)
        {
            try
            {
                var x86View = Win32.RegistryKey.OpenBaseKey(Win32.RegistryHive.LocalMachine, Win32.RegistryView.Registry32);
                x86View.DeleteSubKeyTree(keyPath, false);

                var x64View = Win32.RegistryKey.OpenBaseKey(Win32.RegistryHive.LocalMachine, Win32.RegistryView.Registry64);
                x64View.DeleteSubKeyTree(keyPath, false);
            }
            catch (Exception ex)
            {
                Logger.Log(string.Format("Cannot delete registry with error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Cleanup %ProgramData%\Microsoft\VisualStudioSecondaryInstaller
        /// </summary>
        public void CleanupSecondaryInstallerCache()
        {
            try
            {
                if (Directory.Exists(CommonApplicationDataDirectory))
                {
                    Logger.LogWithOutput(string.Format("Deleting: {0}", CommonApplicationDataDirectory));
                    this.RecursivelyDeleteFolder(CommonApplicationDataDirectory);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWithOutput(string.Format("Cannot delete Secondary Installer cache with error: {0}", ex.Message));
            }
        }

        /// <summary>
        /// Clean up sub-folders in %ProgramData%\Package Cache created by Visual Studio.
        /// </summary>
        public void CleanupVisualStudioPackageCache()
        {
            // TBD
        }

        private static string CommonApplicationDataDirectory
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                                    Path.Combine(@"Microsoft", "VisualStudioSecondaryInstaller"));
            }
        }

        /// <summary>
        /// delete a folder and all its content
        /// </summary>
        /// <param name="folder"></param>
        private void RecursivelyDeleteFolder(string folder)
        {
            foreach (string subDirectory in Directory.GetDirectories(folder))
            {
                RecursivelyDeleteFolder(subDirectory);
            }

            foreach (string file in Directory.GetFiles(folder))
            {
                DeleteFileIfExists(file);
            }

            Directory.Delete(folder);
        }


        private void DeleteFileIfExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException("filePath");
            }

            if (File.Exists(filePath))
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException) // The specified file is in use. -or- ...
                {
                    System.Threading.Thread.Sleep(500);
                    File.Delete(filePath);
                }
                catch (UnauthorizedAccessException)
                {
                    // see if it was because file is read only
                    System.IO.FileAttributes copiedFileAttributes = File.GetAttributes(filePath);
                    if ((copiedFileAttributes & System.IO.FileAttributes.ReadOnly).Equals(System.IO.FileAttributes.ReadOnly))
                    {
                        // remove read only flag
                        File.SetAttributes(filePath, copiedFileAttributes & ~System.IO.FileAttributes.ReadOnly);

                        // try again
                        File.Delete(filePath);
                    }
                }
            }
        }

        private string GetUpgradeCode(string installSource)
        {
            if (File.Exists(installSource))
            {
                try
                {
                    using (var database = new Database(installSource, DatabaseOpenMode.ReadOnly))
                    {
                        using (var view = database.OpenView(database.Tables["Property"].SqlSelectString))
                        {
                            view.Execute();
                            foreach (var rec in view)
                            {
                                if ("UpgradeCode".Equals(rec.GetString("Property"), StringComparison.OrdinalIgnoreCase))
                                {
                                    return rec.GetString("Value");
                                }
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    Logger.Log(e);
                }
            }
            else
            {
                Logger.Log(string.Format("The {0} doesn't exist, cannot find upgrade code.", installSource));
            }

            return string.Empty;
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
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Initialize called successfully."), Logger.MessageLevel.Information, AppName);
                Processed = false;
            }
            catch (Exception ex)
            {
                Logger.Log(ex.Message, Logger.MessageLevel.Error, AppName);
            }
        }

        /// <summary>
        /// Load from a data file.
        /// </summary>
        /// <param name="path"></param>
        public void LoadFromDataFile(string path)
        {
            // Generate file name based on configuration and name of the wixpdb
            long position = 0;
            // create a new formatter instance
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                if (position < stream.Length)
                {
                    stream.Seek(position, SeekOrigin.Begin);
                    this.BundlesAndPackagesStore = (BundlesAndPackagesStore)formatter.Deserialize(stream);
                    position = stream.Position;
                }
            }
            formatter = null;
        }

        /// <summary>
        /// Load from a data file stream.
        /// </summary>
        ///<param name="stream"></param>
        public void LoadFromDataFile(Stream stream)
        {
            // Generate file name based on configuration and name of the wixpdb
            long position = 0;
            // create a new formatter instance
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            if (position < stream.Length)
            {
                stream.Seek(position, SeekOrigin.Begin);
                this.BundlesAndPackagesStore = (BundlesAndPackagesStore)formatter.Deserialize(stream);
                position = stream.Position;
            }
            formatter = null;
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
        /// Save BundlesAndPackageStore object to a data file.
        /// </summary>
        public void SaveToDataFile()
        {
            // create a new formatter instance
            var formatter = new System.Runtime.Serialization.Formatters.Binary.BinaryFormatter();

            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);

            string fileName = Path.Combine(tempDirectory, "DataFile.bin");

            Console.WriteLine(@"Writing data file to " + fileName);

            try
            {
                // open a filestream
                using (FileStream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    formatter.Serialize(stream, this.BundlesAndPackagesStore);
                }
            }catch(Exception e)
            {
                Console.WriteLine(@"Failed to write data file to " + fileName + " reason: " + e.Message);
            }

            Console.WriteLine(string.Format("Writing data file to {0}, completed. ", fileName));
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
        /// Report what Visual Studio's were installed on this system.
        /// </summary>
        public void InstalledVisualStudioReport()
        {
            List<Bundle> installedBundles = new List<Bundle>(this.BundlesAndPackagesStore.Bundles.Where(b => b.Installed));
            var installedBundleStrings = installedBundles.Select<Bundle, string>(b =>
                String.Format("(Name: {0}, Version: {1}, BundleId: {2})", b.Name, b.Version, b.BundleId)).ToArray();

            if (installedBundleStrings.Count() > 0)
            {
                Logger.LogWithOutput(string.Format(@"The following bundles were detected on your system: "), Logger.MessageLevel.Information, AppName);

                foreach (var ib in installedBundleStrings)
                {
                    Logger.LogWithOutput(string.Format(ib), Logger.MessageLevel.Information, AppName);
                }
            }
            else
            {
                Logger.LogWithOutput(string.Format(@"No bundle found.  Uninstalling stale MSIs. "), Logger.MessageLevel.Information, AppName);
            }
        }

        /// <summary>
        /// Uninstall Visual Studio 2013/2015/vNext
        /// </summary>
        public int Uninstall()
        {
            List<Bundle> installedBundles = new List<Bundle>(this.BundlesAndPackagesStore.Bundles.Where(b => b.Installed));

            List<Bundle> orderedBundles = new List<Bundle>();

            foreach (var ib in installedBundles)
            {
                if (!ib.Name.ToLowerInvariant().Contains(@"(kb"))
                {
                    orderedBundles.Add(ib);
                }
            }

            foreach (var ib in installedBundles)
            {
                if (ib.Name.ToLowerInvariant().Contains(@"(kb"))
                {
                    orderedBundles.Add(ib);
                }
            }

            foreach (var ib in orderedBundles)
            {
                int exitCode = 0;
                if (!this.DoNotExecuteProcess)
                {
                    try
                    {
                        exitCode = ib.Uninstall();
                    }
                    catch(Exception ex)
                    {
                        Logger.LogWithOutput(
                            string.Format("Bundle: {0} uninstalled failed with exception: {1}. ", ib.Name, ex.Message));
                    }
                }
                Logger.LogWithOutput(string.Format("Bundle: {0} has been uninstalled with exit code: {1}. ", ib.Name, exitCode));

                if (exitCode == 3010)
                {
                    return exitCode;
                }
            }

            Logger.LogWithOutput("Normal Visual Studio Uninstall completed.");
            Logger.LogWithOutput("Searching for stale MSIs and clean up stale MSIs.");

            var installedPackages = this.GetAllInstalledItems();
            List<Package> packagesToBeUninstalled = new List<Package>();

            foreach(var ip in installedPackages)
            {
                if (this.BundlesAndPackagesStore.UpgradeCodeHash.Contains(ip.UpgradeCode))
                {
                    packagesToBeUninstalled.Add(ip);
                }
                else if (this.BundlesAndPackagesStore.NoUpgradeCodeProductCodeHash.Contains(ip.ProductCode))
                {
                    packagesToBeUninstalled.Add(ip);
                }
            }

            if (packagesToBeUninstalled.Count > 0)
            {
                Logger.LogWithOutput(string.Format("{0} stale MSIs found.  Uninstalling them.", packagesToBeUninstalled.Count ));

                int count = packagesToBeUninstalled.Count;
                foreach (var p in packagesToBeUninstalled)
                {
                    int rc = 0;

                    if (!this.DoNotExecuteProcess)
                    {
                        try
                        {
                            rc = p.Uninstall();
                        }
                        catch(Exception ex)
                        {
                            Logger.LogWithOutput(
                                 string.Format("Msi: {0} uninstalled failed with exception: {1}. ", p.ProductName, ex.Message));
                        }
                    }
                    count--;
                    Logger.LogWithOutput(string.Format("Uninstalled {0} with exit code: {1}. {2}/{3}", p.ProductName, rc, count, packagesToBeUninstalled.Count));
                }
            }

            return 0;
        }
        
        #endregion Public Methods

        #region Protected Methods

        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing) {
                    BundlesAndPackagesStore.UpgradeCodeHash = null;
                    BundlesAndPackagesStore.NoUpgradeCodeProductCodeHash = null;
                    BundlesAndPackagesStore.Bundles = null;
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

        private string releaseoutput = string.Empty;

        private Utility ut = new Utility();

        #endregion Private Fields

        #region Private Methods
        private void GetUniquePackages(HashSet<string> upgradeCodeHash, 
            HashSet<string> noUpgradeCodeProductCodeHash,
            Wix.Table chainmsipackageTable, 
            Wix.Table uxPackageBehavior)
        {
            try
            {
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
                                case "ReallyPermanent": // nullable.
                                    if (field.Data != null)
                                    {
                                        reallyPerm = field.Data.ToString();
                                    }
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

                            case "UPGRADECODE": // nullable.
                                if (field.Data != null)
                                {
                                    msi.UpgradeCode = field.Data.ToString();
                                }
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

                    if (string.IsNullOrEmpty(msi.UpgradeCode))
                    {
                        noUpgradeCodeProductCodeHash.Add(msi.ProductCode);
                    }
                    else
                    {
                        if (!upgradeCodeHash.Contains(msi.UpgradeCode))
                        {
                            upgradeCodeHash.Add(msi.UpgradeCode);
                        }
                    }
                }
                // We should not be uninstalling MSU because they are usually perm and they are windows comp.
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }
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

        /// <summary>
        /// Load a wixpdb
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool LoadFromWixpdb(string path)
        {
            try
            {
                // Get all files associated with WixPDBs in directory
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Loading {0}", path), Logger.MessageLevel.Information, "Utility");
                var pdb = Wix.Pdb.Load(Path.GetFullPath(path), true, true);
                var bundlerowinfo = (Wix.WixBundleRow)GetBundlesFromWixPDB(pdb).Rows[0];

                var bundle = new Bundle(bundlerowinfo.BundleId, bundlerowinfo.Name, bundlerowinfo.Version, Path.GetFileNameWithoutExtension(path), Path.GetFullPath(path), FILETYPE_WIXPDB, false);
                if (!this.BundlesAndPackagesStore.Bundles.Any(b => b.BundleId == bundle.BundleId))
                {
                    this.BundlesAndPackagesStore.Bundles.Add(bundle);
                }

                if (pdb.Output.Type == Wix.OutputType.Bundle)
                {
                    var wixbundle = pdb.Output.Tables[WIXBUNDLE];  //Id: 32 in pdb.Output.Rows
                    var chainmsipackageTable = pdb.Output.Tables[CHAINMSIPACKAGE]; //Id: 0 in pdb.Output.Rows
                    var uxPackageBehavior = pdb.Output.Tables[UXPACKAGEBEHAVIOR]; //Id: 0 in pdb.Output.Rows

                    if (wixbundle != null)
                    {
                        if (chainmsipackageTable != null)
                        {
                            this.GetUniquePackages(
                                this.BundlesAndPackagesStore.UpgradeCodeHash, 
                                this.BundlesAndPackagesStore.NoUpgradeCodeProductCodeHash, 
                                chainmsipackageTable, 
                                uxPackageBehavior);
                        }
                    }
                }

                bundlerowinfo = null;
            }
            catch (Exception e)
            {
                Logger.Log("Unable to load wixpdb: " + path, Logger.MessageLevel.Error);
                Logger.Log(e);
                return false;
            }
            return true;
        }

        private static Wix.Table GetBundlesFromWixPDB(Microsoft.Tools.WindowsInstallerXml.Pdb pdb)
        { 
            var wixbundle = pdb.Output.Tables[WIXBUNDLE];  //Id: 32 in pdb.Output.Rows
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
        
        #endregion Private Methods
    }
}
