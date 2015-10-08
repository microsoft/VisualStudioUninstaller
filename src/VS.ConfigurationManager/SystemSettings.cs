using System;
using System.Globalization;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    ///   Getting version information and architecture information for use with UninstallAction
    ///   class. This will define which items are applicable.
    ///
    ///   Source: https://msdn.microsoft.com/en-us/library/windows/desktop/ms724832(v=vs.85).aspx
    ///         Operating system	                Version number
    ///         Windows                             10	10.0*
    ///         Windows Server Technical Preview	10.0*
    ///         Windows 8.1	                        6.3*
    ///         Windows Server 2012 R2	            6.3*
    ///         Windows 8	                        6.2
    ///         Windows Server 2012	                6.2
    ///         Windows 7	                        6.1
    ///         Windows Server 2008 R2	            6.1
    ///         Windows Server 2008	                6.0
    ///         Windows Vista                       6.0
    ///         Windows Server 2003 R2	            5.2
    ///         Windows Server 2003	                5.2
    ///         Windows XP 64-Bit Edition           5.2
    ///         Windows XP                          5.1
    ///         Windows 2000	                    5.0
    /// </summary>
    public static class SystemSettings
    {
        static private string _version = string.Empty;
        /// <summary>
        /// Returns true for 64 bit system and false for x86.
        /// </summary>
        /// <returns></returns>
        static public bool Is64()
        {
            return IntPtr.Size == 8 ? true : false;
        }
        /// <summary>
        /// Returns Major.Minor version as a string back to the caller.
        /// </summary>
        /// <returns></returns>
        static public string Version()
        {
            if (string.IsNullOrEmpty(_version))
            {
                var _VersionMajor = Environment.OSVersion.Version.Major.ToString(CultureInfo.InvariantCulture);
                var _VersionMinor = Environment.OSVersion.Version.Minor.ToString(CultureInfo.InvariantCulture);
                _version = _VersionMajor + "." + _VersionMinor;
            }
            return _version;
        }
    }
}
