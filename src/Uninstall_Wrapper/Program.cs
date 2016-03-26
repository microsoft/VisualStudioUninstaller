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

        private static int Main(string[] args)
        {
            string wixpdbsPathsFile = string.Empty;
            string[] wixpdbsPaths = null;
            string dataFilePath = string.Empty;
            //args = new string[] { "noprocess", @"/wixpdbs:C:\Users\tobyhu\Desktop\test\paths.txt" };
            //args = new string[] { "noprocess", @"/binfile:C:\Users\tobyhu\Desktop\test\DataFile.bin" };
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
                        default:
                            // Path to the file containing a list of paths to the wixpdbs.
                            // e.g. /wixpdbs:c:\myPaths.txt
                            if (arg.StartsWith("/wixpdbs:", StringComparison.OrdinalIgnoreCase))
                            {
                                wixpdbsPathsFile = arg.Substring("/wixpdbs:".Length);
                                wixpdbsPaths = File.ReadAllLines(wixpdbsPathsFile);
                            }
                            // Path to the file containing the DataFile.bin; if no file is passed in, it will use the embedded one.
                            // e.g. /binfile:C:\DataFile.bin
                            else if (arg.StartsWith("/binfile:", StringComparison.OrdinalIgnoreCase))
                            {
                                dataFilePath = arg.Substring("/binfile:".Length);
                            }
                            break;
                    }
                }
            }

            var ip = new Primitives();

            ConsoleOperations.PrimitiveObject = ip;
            ConsoleOperations.SetUpLogging();

            ip.DoNotExecuteProcess = _donotprocess;
            ip.DebugReporting = _debug;

            try
            {
                // Check for permissions to run uninstall actions
                var elev = new ElevationDetection();
                if (!elev.Level)
                {
                    ConsoleOperations.SecurityWarning();
                    return 0;
                }
                else
                {
                    Logger.Log("Running elevated or as administrator", Logger.MessageLevel.Information, AppName);
                }
                elev = null;

                // Define base variables for use of primitives object; adding filters, uninstall actions, logging location, and default location of data files
                ConsoleOperations.SetupPrimitivesValues(_debug, _donotprocess);

                // If /wixpdbs is used, .bin data file is generated for the user.
                if (wixpdbsPaths != null && wixpdbsPaths.Length > 0)
                {
                    Logger.LogWithOutput("Generating data file from wixpdbs ....");
                    foreach (var wixpdbPath in wixpdbsPaths)
                    {
                        ip.LoadFromWixpdb(wixpdbPath);
                    }

                    ip.SaveToDataFile();
                    Logger.Log("Data File generation operation is successful.  Exiting ...", Logger.MessageLevel.Information, AppName);
                    return 0;
                }
                // Else uninstall Visual Studio 2013/2015/vNext
                else
                {
                    if (!string.IsNullOrEmpty(dataFilePath) && File.Exists(dataFilePath))
                    {
                        ip.LoadFromDataFile(dataFilePath);
                    }
                    else
                    {
                        // load from embedded.
                    }

                    ip.InstalledVisualStudioReport();
                    Logger.LogWithOutput("Would you like to continue? [Y/N]");
                    var action = Console.ReadLine();
                    if (!string.IsNullOrEmpty(action) && action.StartsWith("y", StringComparison.OrdinalIgnoreCase))
                    {
                        int exitCode = ip.Uninstall();

                        if (exitCode == 3010)
                        {
                            Logger.LogWithOutput("Bundle requested to reboot the system.  Please reboot your computer and run this application again.");
                        }
                    }
                    else
                    {
                        Logger.LogWithOutput("Exiting ...");
                    }
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

            return 0;
        }

        #endregion Private Methods
    }
}
