using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Permissions;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>Representation of a Wix bundle that is serializable to disk</summary>
    [Serializable()]
    public class Bundle
    {
        private const string PackageCacheRegistryPath = @"Software\Policies\Microsoft\WiX\Burn";
        private const string PackageCacheValue = @"Package Cache";
        private const string AppName = "Bundle";

        // TODO: do these vars need to be static?
        private string Temp
        {
            get
            {
                return System.IO.Path.GetTempPath();
            }
        }
        private static string ProgramData
        {
            get
            {
                return Environment.GetEnvironmentVariable("ALLUSERSPROFILE");
            }
        }
        private string LogLocation
        {
            get
            {
                return System.IO.Path.Combine(Temp, @"Uninstall");
            }
        }
        private static string PackageCache
        {
            get
            {
                if (!regcheck) // if there is no registry value for package cache, it will return an empty string resulting in all bundles requiring a registry read.
                {
                    cache = String.IsNullOrEmpty(cache) && !regcheck ? Utility.ReadRegKey(PackageCacheRegistryPath, PackageCacheValue.Replace(" ", "")) : cache;
                    regcheck = true;
                }
                return String.IsNullOrEmpty(cache) ? System.IO.Path.Combine(ProgramData, PackageCacheValue) : cache;
            }
        }
        private static string cache = string.Empty;
        private static bool regcheck;

        private System.Guid bundleid;

        // TODO: Refactor constructors to use defaults.

        /// <summary>Bundle creation forcing variables to be set.</summary>
        public Bundle()
        {
            SetObjectVariables();
        }

        /// <summary>Bundle creation with values being passed in before hand.</summary>
        /// <param name="bundleid"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        public Bundle(System.Guid bundleid, string name, string version)
        {
            Initialize(bundleid, name, version);
        }

        /// <summary>Bundle creation for all parameters being passed in.</summary>
        /// <param name="bundleid"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="releasepdb"></param>
        /// <param name="path"></param>
        /// <param name="filetype"></param>
        /// <param name="selected"></param>
        /// <param name="packages"></param>
        public Bundle(System.Guid bundleid, string name, string version, string releasepdb, string path, string filetype, bool selected, ICollection<Package> packages)
        {
            Initialize(bundleid, name, version);
            ReleasePdb = releasepdb;
            Path = path;
            FileType = filetype;
            Selected = selected;
        }
        /// <summary>
        /// Creating a bundle without passing package information
        /// </summary>
        /// <param name="bundleid"></param>
        /// <param name="name"></param>
        /// <param name="version"></param>
        /// <param name="releasepdb"></param>
        /// <param name="path"></param>
        /// <param name="filetype"></param>
        /// <param name="selected"></param>
        public Bundle(System.Guid bundleid, string name, string version, string releasepdb, string path, string filetype, bool selected)
        {
            Initialize(bundleid, name, version);
            ReleasePdb = releasepdb;
            Path = path;
            FileType = filetype;
            Selected = selected;
        }

        private void Initialize(System.Guid passedbundleid, string name, string version)
        {
            SetObjectVariables();
            BundleId = passedbundleid;
            Name = name;
            Version = version;
            _installed = Directory.Exists(LocalInstallLocation) ? true : false;
        }

        // TODO: does this need to be static.
        static private void SetObjectVariables()
        {
        }

        /// <summary>
        ///   Override the command line for the bundle installation. The last parameter needs to be
        ///   the log command line parameter.
        /// </summary>
        public string BundleUninstallArguments
        {
            get { return "/uninstall /force /Q /Log \"{0}\""; }
        }
        /// <summary>Location of the package cache on disk</summary>
        public string LocalInstallLocation { get; set; }

        /// <summary>Bundle product code</summary>
        public System.Guid BundleId
        {
            get { return bundleid; }
            set
            {
                bundleid = value;
                LocalInstallLocation = System.IO.Path.Combine(PackageCache, '{' + BundleId.ToString() + '}');
            }
        }

        /// <summary>WiX Bundle Product Name</summary>
        public string Name { get; set; }

        /// <summary>WiX Bundle Version</summary>
        public string Version { get; set; }

        internal string FileType { get; set; }

        /// <summary>Is the bundle installed?</summary>
        public bool Installed
        {
            get {
                LocalInstallLocation = System.IO.Path.Combine(PackageCache, '{' + BundleId.ToString() + '}');
                return Directory.Exists(LocalInstallLocation) ? true : false;
            }
        }

        /// <summary>Has the user selected this item for uninstall?</summary>
        public bool Selected { get; set; }
        /// <summary>
        /// Path to directory for the wixpdb/config file that created bundle
        /// </summary>
        public string Path { get; set; }
        /// <summary>
        /// Name of the WixPDB
        /// </summary>
        public string ReleasePdb { get; set; }
        /// <summary>
        /// Location of the serialized config file
        /// </summary>
        public string binPath { get; set; }

        private bool _installed;

        /// <summary>
        ///   Initiating an uninstall from the bundle to ensure that force uninstall is first run.
        ///   Once that has been completed, there can be MSIs that are left behind.
        /// </summary>
        /// <returns></returns>
        [PermissionSet(SecurityAction.LinkDemand, Name = "FullTrust", Unrestricted = false)]
        public int Uninstall()
        {
            var exitcode = -1;
            try
            {
                Logger.Log("Bundle uninstall started.", Logger.MessageLevel.Information, AppName);
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "this.Installed: {0}", this.Installed), Logger.MessageLevel.Information, AppName);
                if (this.Installed)
                {
                    Logger.Log("Bundle uninstall called and bundle is installed.", Logger.MessageLevel.Information, AppName);
                    foreach (string file in Directory.GetFiles(LocalInstallLocation, "*.exe"))
                    {
                        var bundlelogfilename = System.IO.Path.ChangeExtension(LogLocation + "_" + System.IO.Path.GetFileNameWithoutExtension(file), "log");
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Installer: {0}", file), Logger.MessageLevel.Information, AppName);
                        var args = String.Format(CultureInfo.InvariantCulture, BundleUninstallArguments, bundlelogfilename);
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Arguments: {0}", args), Logger.MessageLevel.Information, AppName);

                        Logger.LogWithOutput(string.Format("Uninstalling: {0}", file));
                        exitcode = Utility.ExecuteProcess(file, args);
                        if (exitcode == 0)
                            Logger.Log("Uninstall succeeded");
                        else
                        {
                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall failed with error code: {0}", exitcode), Logger.MessageLevel.Warning, AppName);
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
            }

            Logger.Log("Bundle uninstall ended.", Logger.MessageLevel.Information, AppName);

            return exitcode;
        }
    }
}
