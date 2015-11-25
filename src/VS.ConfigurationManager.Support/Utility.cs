using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security;
using System.ServiceProcess;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    ///   Functions and values to allow central logging and execution of processes
    /// </summary>
    public class Utility : IDisposable
    {
        private const string AppName = "Utility";

        /// <summary>
        /// Setting service state values
        /// </summary>
        public enum ServiceState
        {
            /// <summary>
            /// Do nothing
            /// </summary>
            None,
            /// <summary>
            /// Start the service
            /// </summary>
            Start,
            /// <summary>
            /// Stop the service
            /// </summary>
            Stop
        }
        /// <summary>Set properties as part of instantiation.</summary>
        public Utility()
        {
            Initialize();
        }

        private void Initialize() { }

        #region Public Methods

        /// <summary>
        /// Registry key reading function with call to native functions
        /// </summary>
        /// <param name="path"></param>
        /// <param name="findkey"></param>
        /// <returns></returns>
        public static string ReadRegKey(string path, string findkey)
        {
            var value = string.Empty;

            try { value = RegistryHandler.GetRegistryKey32(RegHive.HKEY_LOCAL_MACHINE, path, findkey); }
            catch (Exception ex) {
                Logger.Log(ex, AppName);
                value = string.Empty;
            }

            return value;
        }

        #endregion Public Methods

        #region Internal Methods


        /// <summary>Launches a process and returns the error code. 0 is success.</summary>
        /// <param name="file"></param>
        /// <param name="args"></param>
        [SecurityCritical]
        public static int ExecuteProcess(string file, string args)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture,"Creating process for {0} with arguments: {1}", file, args), Logger.MessageLevel.Information, AppName);
            var p = new Process();
            int exitcode;
            try
            {
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = file;
                p.StartInfo.Arguments = args;
                p.StartInfo.Verb = "runas";
                p.Start();
                p.WaitForExit();
                exitcode = p.ExitCode;
            }
            finally
            {
                p.Dispose();
            }

            return exitcode;
        }

        /// <summary>Stopping and starting services required to uninstall MSUs.</summary>
        /// <param name="ServiceName"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public static bool ServiceAction(string ServiceName, ServiceState status)
        {
            var serviceaction = false;

            sc.ServiceName = ServiceName;
            var check = ServiceControllerStatus.Stopped;
            try
            {
                switch (status)
                {
                    case ServiceState.Stop:
                        check = ServiceControllerStatus.Stopped;
                        sc.Stop();
                        break;

                    case ServiceState.Start:
                        check = ServiceControllerStatus.Running;
                        sc.Start();
                        break;
                }
                sc.WaitForStatus(check);
                serviceaction = true;
            }
            catch (InvalidOperationException ex)
            {
                Logger.Log(ex, AppName);
            }

            return serviceaction;
        }

        #endregion Internal Methods

        #region Private Fields

        private const string FILETYPE_WIXPDB = "WixPdb";
        private const string FILETYPE_BIN = "BIN";
        private const string WIXBUNDLE = "WixBundle";

        static private string temp = System.IO.Path.GetTempPath();
        static private ServiceController sc = new ServiceController();

        #endregion Private Fields

        #region Private Methods

        /// <summary>Define where the temp directory is located.</summary>
        public static string TempDir
        {
            get { return temp; }
            set { temp = TrailingSlash(value); }
        }

        private static string TrailingSlash(string dir)
        {
            if (!dir.EndsWith("\\", StringComparison.OrdinalIgnoreCase)) dir += "\\";
            return dir;
        }

        #endregion Private Methods

        #region IDisposable Support

        private bool disposedValue; // To detect redundant calls

        /// <summary>Clean up objects explicitly</summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                Logger.Log("Disposing of objects", Logger.MessageLevel.Verbose, AppName);
                if (disposing)
                {
                    sc.Dispose();
                    // Logger.Dispose();
                }

                disposedValue = true;
            }
            sc.Dispose();
        }

        /// <summary>Dispose of resources utilitized by Utility item</summary>
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Support
    }
}
