using System;
using System.Security.Principal;
using System.Threading;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    /// Class to determine if a user has run the application with administrative priviledges.
    /// </summary>
    public class ElevationDetection
    {
        private const string AppName = "ElevationDetection";
        #region Constructor:
        /// <summary>
        /// Constructor that sets whether permission is available to read the registry and uninstall.
        /// </summary>
        public ElevationDetection()
        {
            // Invoke Method On Creation:
            Elevate();
        }

        #endregion Constructor:
        /// <summary>
        /// Property that defines a user has permission.
        /// </summary>
        public bool Level { get; set; }

        private void Elevate()
        {
            try
            {
                Logger.Log("Identifying if elevation has been provided", Logger.MessageLevel.Information, AppName);
                // Was this thread started with admin priviledges?
                var domain = Thread.GetDomain();

                domain.SetPrincipalPolicy(PrincipalPolicy.WindowsPrincipal);
                var role = (WindowsPrincipal)Thread.CurrentPrincipal;

                if (Environment.OSVersion.Platform != PlatformID.Win32NT || Environment.OSVersion.Version.Major < 6)
                {
                    Logger.Log("Lower OS found (< 6.0 revision or not Win32 NT)", Logger.MessageLevel.Information, AppName);
                    Level = false;
                    // Todo: Exception/ Exception Log
                }
                else
                {
                    if (!role.IsInRole(WindowsBuiltInRole.Administrator))
                    {
                        Logger.Log("Not part of the Administrator role", Logger.MessageLevel.Information, AppName);
                        Level = false;
                        // Todo: "Exception Log / Exception"
                    }
                    else
                    {
                        Logger.Log("Part of the Administrator role", Logger.MessageLevel.Information, AppName);
                        Level = true;
                    }
                } // Initial Else 'Close'
            }
            catch (Exception ex)
            {
                Logger.Log(ex, AppName);
                Level = false;
            }
        }
    }
}
