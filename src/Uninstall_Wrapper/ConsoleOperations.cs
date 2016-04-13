using Microsoft.VS.ConfigurationManager;
using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Microsoft.VS.Uninstaller
{
    static internal class ConsoleOperations
    {
        private const string _explorer = "Explorer.exe";
        private const int _width = 85;
        private const int _height = 45;

        public static List<CommandOption> Options = new List<CommandOption>();

        private static string line = new StringBuilder().Append('-', 85).ToString();
        private static string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DataFiles");
        public const string AppName = "ConsoleOperations";
        public const string COMMAND_COMPARE_DIRECTORY = "DIR";
        public const string COMMAND_COMPARE_LIST = "LIST";
        public const string COMMAND_COMPARE_CREATE = "CREATE";
        public const string COMMAND_COMPARE_LOAD = "LOAD";
        public const string COMMAND_COMPARE_SELECT = "SELECT";
        public const string COMMAND_COMPARE_INSTALLED = "INSTALLED";
        public const string COMMAND_COMPARE_VSINSTALLED = "VSINSTALLED";
        public const string COMMAND_COMPARE_UNINSTALL = "UNINSTALL";
        public const string COMMAND_COMPARE_MSIS = "MSIS";
        public const string COMMAND_COMPARE_TEMP = "TEMP";
        public const string COMMAND_COMPARE_LOGSELECTED = "LOG";

        internal static Primitives PrimitiveObject { get; set; }

        static internal string MsgRelease { get; set; }

        static internal void SecurityWarning()
        {
            Console.WriteLine(Logger.Log("Not running as elevated or administrator.", Logger.MessageLevel.Information, AppName));
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Logger.Log("Possible error condition found.", Logger.MessageLevel.Information, AppName));
            Console.ResetColor();
            Console.WriteLine(Logger.Log("If you are not running with elevated permissions, the uninstall processes can result in errors.  For optimal results, please run command console as administrator.", Logger.MessageLevel.Information, AppName));
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\r\nPress enter to continue.");
            Console.ResetColor();
            Console.ReadLine();
        }

        static internal void SetConsoleAttributes()
        {
            Console.WindowHeight = Console.LargestWindowHeight < _height ? Console.LargestWindowHeight : _height;
            Console.WindowWidth = _width;
            Console.Title = "WixPdb sourced uninstall driver";
        }

        static internal void OpenTempDirectory()
        {
            Console.WriteLine(Logger.Log("Opening temp directory", Logger.MessageLevel.Verbose, AppName));
            Utility.ExecuteProcess(_explorer, Utility.TempDir);
        }

        static internal void SetUpLogging()
        {
            var time = DateTime.Now.ToString("MM-dd-yy-hhmmss", CultureInfo.InvariantCulture);
            var logfilepath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dd_Microsoft.VS.Uninstaller_" + System.IO.Path.ChangeExtension(time, "log"));
            Logger.LogLocation = logfilepath;
            Logger.LoggingLevel = Logger.MessageLevel.Verbose;
            Logger.ConsoleOutput = true;
        }

        static internal void GetUsage()
        {

            var sb = new StringBuilder();

            sb.AppendLine("Please use the key words to execute a command.\r\n");
            sb.AppendLine("Command".PadRight(30) + "Key words".PadRight(15) + "Description".PadRight(40));
            sb.AppendLine(line);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(sb.ToString());
            Console.ResetColor();

            foreach (CommandOption op in GetOptions())
            {
                sb = new StringBuilder();
                sb.Append(op.Command.PadRight(30));
                sb.Append(op.CommandCompareValue.PadRight(15));
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write(sb.ToString());
                Console.ResetColor();
                FormatOutput(op.Description, 30, 45);
            }
            Console.ForegroundColor = ConsoleColor.White;
            sb = new StringBuilder();

            sb.AppendLine();
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "Working directory: {0}", PrimitiveObject.DataFilesPath));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "Debug: {0}", PrimitiveObject.DebugReporting.ToString()));
            sb.AppendLine(String.Format(CultureInfo.InvariantCulture, "Do Not Process: {0}", PrimitiveObject.DoNotExecuteProcess.ToString()));
            sb.AppendLine();
            sb.AppendLine("Enter in a key word to run the next command or 'quit' to exit");
            sb.AppendLine();
            sb.Append("> ");
            Console.Write(sb.ToString());
            sb = null;
        }

        private static ICollection<CommandOption> GetOptions()
        {
            if (Options.FirstOrDefault() == null)
            {
                Options.Add(new CommandOption("Directory", "Identifies which directory files are loaded from and saved to.  This has no impact on the Content directory where the WixPdbs go.", COMMAND_COMPARE_DIRECTORY, null));
                Options.Add(new CommandOption("List", "Lists all the bundles that the application is aware of.", COMMAND_COMPARE_LIST, null));
                Options.Add(new CommandOption("Create config files", "Creates configuration files from wixpdbs.", COMMAND_COMPARE_CREATE, null));
                Options.Add(new CommandOption("Load config files", "Loads configuration files from disk", COMMAND_COMPARE_LOAD, null));
                Options.Add(new CommandOption("Select", "Allows you to choose which bundle(s) to uninstall", COMMAND_COMPARE_SELECT, null));
                Options.Add(new CommandOption("Show what is installed", "Shows what is currently installed on this machine.", COMMAND_COMPARE_INSTALLED, null));
                Options.Add(new CommandOption("Show what VS installs what", "Shows of the things that are installed, which ones are installed by Visual Studio", COMMAND_COMPARE_VSINSTALLED, null));
                Options.Add(new CommandOption("Uninstall", "Triggers an uninstall of the selected bundle(s)", COMMAND_COMPARE_UNINSTALL, null));
                Options.Add(new CommandOption("Uninstall MSIs", "Used after uninstalling the selected bundle(s) to remove any loose MSIs left behind.", COMMAND_COMPARE_MSIS, null));
                Options.Add(new CommandOption("Open temp dir", "Opens the temporary directory where logs are stored.", COMMAND_COMPARE_TEMP, null));
            }
            return Options;
        }

        private static void FormatOutput(string textselection, int pos, int startpad)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Formatting output: {0}", textselection), Logger.MessageLevel.Verbose, AppName);
            if (textselection.Length > pos && textselection.IndexOf(' ', pos) != -1)
            {
                var space = textselection.IndexOf(' ', pos);

                Console.WriteLine(textselection.Substring(0, space).PadLeft(pos));

                while (textselection.Length >= pos && textselection.IndexOf(' ', pos) != -1)
                {
                    textselection = textselection.Length >= pos ? textselection.Substring(space, textselection.Length - space).Trim() : textselection;
                    space = textselection.IndexOf(' ', pos >= textselection.Length ? textselection.Length : pos);
                    Console.WriteLine(" ".PadLeft(startpad) + textselection.Substring(0, space == -1 ? textselection.Length : space));
                }
            }
            else
            {
                Console.WriteLine(textselection);
            }
            Logger.Log("Formatting output ended", Logger.MessageLevel.Verbose, AppName);
        }


        static internal void ChangeWorkingDirectory(string[] cmdset)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Change working directory command started."), Logger.MessageLevel.Verbose, AppName);
            var dir = cmdset[1].Replace(COMMAND_COMPARE_DIRECTORY, "");

            PrimitiveObject.DataFilesPath = (String.IsNullOrEmpty(dir)) ? PrimitiveObject.DataFilesPath : dir;
            var di = new DirectoryInfo(dir);

            if (!di.Exists) { di.Create(); }

            PrimitiveObject.DataFilesPath = dir;

            Console.WriteLine(String.Format(CultureInfo.InvariantCulture, "Working directory has been reset to {0}", PrimitiveObject.DataFilesPath));
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Change working directory: {0}", PrimitiveObject.DataFilesPath), Logger.MessageLevel.Information, AppName);
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Change working directory command ended."), Logger.MessageLevel.Verbose, AppName);
        }

        static internal void SetupPrimitivesValues(bool debug, bool donotprocess)
        {
            // uti.bDebug = op.Debug; ip.DebugReporting = op.Debug;
            PrimitiveObject.MachineArchitectureConfiguration = SystemSettings.Is64() ? ArchitectureConfiguration.x64 : ArchitectureConfiguration.x86;
            PrimitiveObject.MachineOSVersion = SystemSettings.Version();

            PrimitiveObject.Filters.Clear();
            PrimitiveObject.UninstallActions.Clear();

            VisualStudioSpecific.VSFilters(PrimitiveObject);
            VisualStudioSpecific.VSUninstallActions(PrimitiveObject);

            PrimitiveObject.DataFilesPath = path;
            PrimitiveObject.DoNotExecuteProcess = donotprocess;
            PrimitiveObject.DebugReporting = debug;

            //Initialize
            PrimitiveObject.Initialize();
        }
    }
}
