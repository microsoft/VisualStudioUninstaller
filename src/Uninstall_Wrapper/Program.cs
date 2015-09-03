using Microsoft.VS.ConfigurationManager;
using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Microsoft.VS.Uninstaller
{
    internal class Program
    {
        private const string _explorer = "Explorer.exe";
        private const string AppName = "Console Application";

        private static bool _debug;
        private static bool _donotprocess;

        #region Private Methods

        private static void Main(string[] args)
        {
            if (args != null && args.Count() > 0)
            {
                foreach (var arg in args)
                {
                    switch(arg)
                    {
                        case "break":
                            Console.WriteLine("Program stopped, please attach debugger and then hit any key to continue.");
                            Console.ReadKey(true);
                            break;
                        case "debug":
                            _debug = true;
                            break;
                        case "noprocess":
                            _donotprocess = true;
                            break;
                    }
                }
            }

            ICollection<Bundle> Bundles = new List<Bundle>();
            ICollection<string> Userselected = new List<string>();
            var ip = new Primitives();

            ConsoleOperations.PrimitiveObject = ip;
            ConsoleOperations.SetUpLogging();

            ip.DoNotExecuteProcess = _donotprocess;
            ip.DebugReporting = _debug;

            try
            {
                // Change visual of console application to fit screen.
                ConsoleOperations.SetConsoleAttributes();

                // Check for permissions to run uninstall actions
                var elev = new ElevationDetection();
                if (!elev.Level)
                {
                    ConsoleOperations.SecurityWarning();
                }
                else
                {
                    Logger.Log("Running elevated or as administrator", Logger.MessageLevel.Information, AppName);
                }
                elev = null;

                // Define base variables for use of primitives object; adding filters, uninstall actions, logging location, and default location of data files
                ConsoleOperations.SetupPrimitivesValues(_debug, _donotprocess);

                var cmd = string.Empty;
                while (cmd != "quit")
                {
                    try
                    {
                        var cmdset = cmd.ToUpperInvariant().Split(' ');
                        cmd = cmdset[0];

                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Command value passed from user: {0}", cmd), Logger.MessageLevel.Information, AppName);
                        switch (cmd)
                        {
                            #region Command processor
                            case ConsoleOperations.COMMAND_COMPARE_TEMP:
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Temp directory command started."), Logger.MessageLevel.Verbose, AppName);
                                ConsoleOperations.OpenTempDirectory();
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Temp directory command ended."), Logger.MessageLevel.Verbose, AppName);
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_SELECT:
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Display select options command started."), Logger.MessageLevel.Verbose, AppName);
                                ConsoleOperations.SelectReleaseFromAvailableBundles();
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Display select options command ended."), Logger.MessageLevel.Verbose, AppName);
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_CREATE:
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Save command started."), Logger.MessageLevel.Verbose, AppName);
                                ip.Processed = false;
                                ip.GetDataFromPdb();
                                ip.SaveAll();
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Save command ended."), Logger.MessageLevel.Verbose, AppName);
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_LOAD:
                                try
                                {
                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Load command started."), Logger.MessageLevel.Verbose, AppName);
                                    // Load up object files from disk
                                    Bundles = ip.LoadFromFiles();
                                    Console.WriteLine("Files loaded successfully.");
                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Load command ended."), Logger.MessageLevel.Verbose, AppName);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Log(ex, AppName);
                                    Console.WriteLine(ex.Message);
                                }
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_INSTALLED:
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "What is installed command started."), Logger.MessageLevel.Verbose, AppName);
                                // Using WindowsInstaller.Deployment to determine what is installed on the machine.
                                var packages = ip.GetAllInstalledItems;

                                foreach (Package package in packages)
                                {
                                    var pname = package.ProductName.PadRight(50);
                                    var pcode = package.ProductCode.ToString().PadRight(30);
                                    Console.WriteLine(Logger.Log(String.Format(CultureInfo.InvariantCulture, "{0} {1}", pcode, pname), Logger.MessageLevel.Information, AppName));
                                }
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "What is installed command ended."), Logger.MessageLevel.Verbose, AppName);
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_DIRECTORY:
                                ConsoleOperations.ChangeWorkingDirectory(cmdset);
                                break;
                            case ConsoleOperations.COMMAND_COMPARE_LIST:  // What releases were loaded from config files or wixpdbs
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "List releases command started."), Logger.MessageLevel.Verbose, AppName);
                                Console.WriteLine(ConsoleOperations.PrintReleaseInfo());
                                Logger.Log(String.Format(CultureInfo.InvariantCulture, "List releases command ended."), Logger.MessageLevel.Verbose, AppName);
                                break;
                            default:
                                #region Make sure something is selected before executing these commands
                                // Check for if something was selected
                                if (ip.Releases.FirstOrDefault(x => x.Selected) != null)
                                {
                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Selection count is greater than 0."), Logger.MessageLevel.Information, AppName);
                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Check if Bundles object has an objects populated."), Logger.MessageLevel.Verbose, AppName);
                                    if (!ip.Processed)
                                    {
                                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Populate Bundles object started"), Logger.MessageLevel.Verbose, AppName);
                                        Bundles = IfNotProcessed(ip);
                                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "Populate Bundles object ended"), Logger.MessageLevel.Verbose, AppName);
                                    }
                                    else { Bundles = ip.Releases.Where(x => x.Selected).ToList(); }

                                    switch (cmd) // Commands that require release(s) to be selected
                                    {
                                        case ConsoleOperations.COMMAND_COMPARE_UNINSTALL:
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall command started."), Logger.MessageLevel.Verbose, AppName);
                                            ProcessUninstallBundles(ip, Bundles);
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall command ended."), Logger.MessageLevel.Verbose, AppName);
                                            break;
                                        case ConsoleOperations.COMMAND_COMPARE_MSIS:
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall MSI command started."), Logger.MessageLevel.Verbose, AppName);
                                            ProcessUninstallMSIs(ip);
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Uninstall MSI command ended."), Logger.MessageLevel.Verbose, AppName);
                                            break;
                                        case ConsoleOperations.COMMAND_COMPARE_VSINSTALLED:
                                            // On a given machine, this what VS installed.
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "VS Installs command started."), Logger.MessageLevel.Verbose, AppName);
                                            foreach (Bundle bundle in ip.GetAllInstalledItemsCompareWixPdb)
                                            {
                                                Console.WriteLine(bundle.Name);
                                                foreach (Package package in bundle.Packages)
                                                {
                                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "-- {0}", ip.ApplyFilter(package.ProductName).PadRight(40) + package.ProductCode.ToString().PadRight(30)), Logger.MessageLevel.Information, AppName);
                                                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "-- {0}", ip.ApplyFilter(package.ProductName).PadRight(40) + package.ProductCode.ToString().PadRight(30)));
                                                }
                                            }
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "VS Installs command ended."), Logger.MessageLevel.Verbose, AppName);
                                            break;
                                        case ConsoleOperations.COMMAND_COMPARE_LOGSELECTED:
                                            // Writes out product name, product code, and package type
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Log selected command started."), Logger.MessageLevel.Verbose, AppName);
                                            foreach (Bundle bundle in Bundles)
                                            {
                                                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Logging start: {0}", bundle.Name));
                                                foreach (Package package in bundle.Packages)
                                                {
                                                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "-- {0}", ip.ApplyFilter(package.ProductName).PadRight(80) + package.ChainingPackage.PadRight(60) + package.ProductCode.ToString().PadRight(45)) + package.Type.ToString().PadRight(30), Logger.MessageLevel.Information, AppName);
                                                }
                                                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Logging end: {0}", bundle.Name));
                                            }
                                            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Log selected command started."), Logger.MessageLevel.Verbose, AppName);
                                            break;
                                    }
                                }
                                else if ((Userselected.Count == 0) && (!String.IsNullOrEmpty(cmd)))
                                {
                                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Use the 'Select' command to determine which bundles you want to try \r\n to uninstall"));
                                }
                                break;  // End check for if something was selected
                                #endregion
                        }
                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(Logger.Log(ex, AppName));
                    }

                    if (ConsoleOperations.Options.Where(x => x.CommandCompareValue.Contains(cmd)) == null)
                    {
                        Console.WriteLine("\r\nInvalid command. Please try again.\r");
                    }

                    if (!String.IsNullOrEmpty(cmd))
                    {
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("\r\n\r\nPress any key to continue.");
                        Console.ReadLine();
                        Console.ResetColor();
                        Console.Clear();
                    }

                    ConsoleOperations.GetUsage();
                    cmd = Console.ReadLine();
                    Console.Clear();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ex, AppName);
            }
            finally
            {
                ip.Dispose();
            }
        }

        private static ICollection<Bundle> IfNotProcessed(Primitives ip)
        {
            ICollection<Bundle> Bundles;
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Attempting to load data from wixpdbs."), Logger.MessageLevel.Information, AppName);
            Bundles = ip.GetDataFromPdb(false);
            if (Bundles.FirstOrDefault() == null)
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "Attempting to load data from files."), Logger.MessageLevel.Information, AppName);
                Bundles = ip.LoadFromFiles();
            }

            if (Bundles.FirstOrDefault() == null)
            {
                var msg = "No WixPdbs or output files to use as source.\r\n  Please add WixPdbs or configuration files to the working directory.";
                Console.WriteLine(msg);
                throw new NoSourceFilesAvailableForParsingException(msg);
            }
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Configuration loaded successfully."), Logger.MessageLevel.Information, AppName);
            return Bundles;
        }

        private static void ProcessUninstallBundles(Primitives ip, ICollection<Bundle> Bundles)
        {
            // Uninstall only VS selected items
            var pos = 0;
            var selectedbundle = Bundles.ElementAtOrDefault(pos);
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "-Bundle uninstall started."), Logger.MessageLevel.Verbose, AppName);
            var bundleexitcode = 0;
            while ((selectedbundle != null) && (bundleexitcode == 0) && (pos <= (Bundles.Count() - 1)))
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "--Bundle uninstall [{0}]", selectedbundle.Name), Logger.MessageLevel.Verbose, AppName);

                // Is the bundle installed
                if (selectedbundle.Installed)
                {
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Initiating uninstall: {0}", selectedbundle.Name));
                    bundleexitcode = ip.Uninstall(selectedbundle);
                }
                pos++;
                selectedbundle = Bundles.ElementAtOrDefault(pos);
            }
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Bundle uninstall completed"), Logger.MessageLevel.Verbose, AppName);
            switch (bundleexitcode)
            {
                // Reboot error codes from bundle and then from MSU
                case -2147205120:
                case 3010:
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Reboot exception has been hit from bundle: {0}", selectedbundle.Name), Logger.MessageLevel.Error, AppName);
                    throw new RebootRequiredException("A reboot is required to complete the uninstall process or an error can occur in the uninstall process.");
                case 0:
                    ProcessUninstallMSIs(ip);
                    break;
                default:
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "Bundle exited with non-zero exit code: {0}", bundleexitcode), Logger.MessageLevel.Error, AppName);
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Bundle exited with non-zero exit code: {0}", bundleexitcode));
                    break;
            }
        }

        private static void ProcessUninstallMSIs(Primitives ip)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Bundle uninstall completed successfully"), Logger.MessageLevel.Information, AppName);
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI uninstall started."), Logger.MessageLevel.Verbose, AppName);
            var msiexitcode = UninstallMSIs(ip);
            if (msiexitcode != 0)
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI exited with non-zero exit code: {0}", msiexitcode), Logger.MessageLevel.Information, AppName);
                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "MSI exited with non-zero exit code: {0}", msiexitcode));
            }
            else
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI batch uninstall completed successfully"), Logger.MessageLevel.Information, AppName);
                Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "MSI batch uninstall completed successfully"));
            }
        }

        private static int UninstallMSIs(Primitives ip)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI uninstall starting"), Logger.MessageLevel.Verbose, AppName);
            var exitcode = 0;

            var Bundles = ip.GetAllInstalledItemsCompareWixPdb;
            foreach (Bundle bundle in Bundles)
            {
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "-Get list of remaining MSIs after bundle uninstall"), Logger.MessageLevel.Information, AppName);
                var packages = bundle.Packages;
                if (packages.Count() > 0)
                {
                    Logger.Log(String.Format(CultureInfo.InvariantCulture, "--MSI uninstall for bundle [{0}]", bundle.Name), Logger.MessageLevel.Information, AppName);
                    Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Bundle: {0} \r\n-- MSIs remaining after uninstall: {1}", bundle.Name, packages.Count().ToString(CultureInfo.InvariantCulture)));
                    var pos = 0;

                    var selectedPackage = bundle.Packages.ElementAtOrDefault(pos);
                    while ((selectedPackage != null) && (exitcode == 0) && (pos <= (bundle.Packages.Count() - 1)))
                    {
                        Logger.Log(String.Format(CultureInfo.InvariantCulture, "---MSI uninstall [{0}]", selectedPackage.ProductName), Logger.MessageLevel.Verbose, AppName);
                        exitcode = ip.Uninstall(selectedPackage);
                        Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "-- {0}", selectedPackage.ProductName));

                        if (exitcode != 0)
                        {
                            Console.WriteLine(Logger.Log(String.Format(CultureInfo.InvariantCulture, "---MSI uninstall [{0}] failed with error code: {1}", selectedPackage.ProductName, exitcode), Logger.MessageLevel.Error, AppName));
                            break;
                        }
                        pos++;
                        selectedPackage = bundle.Packages.ElementAtOrDefault(pos);
                    }
                }
                Logger.Log(String.Format(CultureInfo.InvariantCulture, "--MSI uninstall for bundle [{0}] completed", bundle.Name), Logger.MessageLevel.Information, AppName);
            }
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "MSI uninstall ending"), Logger.MessageLevel.Verbose, AppName);
            return exitcode;
        }

        #endregion Private Methods
    }
}
