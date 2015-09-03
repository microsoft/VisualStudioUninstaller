using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    /// SAM values for each set of commands and locations.
    /// </summary>
    public enum RegistrySAM
    {
        /// <summary>
        /// No value assigned
        /// </summary>
        None = 0x0000,
        /// <summary>
        /// Query verb
        /// </summary>
        QueryValue = 0x0001,
        /// <summary>
        /// Set verb
        /// </summary>
        SetValue = 0x0002,
        /// <summary>
        /// Create verb
        /// </summary>
        CreateSubKey = 0x0004,
        /// <summary>
        /// Enumerate verb
        /// </summary>
        EnumerateSubKeys = 0x0008,
        /// <summary>
        /// Notify verb
        /// </summary>
        Notify = 0x0010,
        /// <summary>
        /// Create link verb
        /// </summary>
        CreateLink = 0x0020,
        /// <summary>
        /// 32 bit registry location
        /// </summary>
        WOW64_32Key = 0x0200,
        /// <summary>
        /// 64 bit registry location
        /// </summary>
        WOW64_64Key = 0x0100,
        /// <summary>
        ///
        /// </summary>
        WOW64_Res = 0x0300,
        /// <summary>
        /// Read access permissions
        /// </summary>
        Read = 0x00020019,
        /// <summary>
        /// Write access permissions
        /// </summary>
        Write = 0x00020006,
        /// <summary>
        /// Execute access permissions
        /// </summary>
        Execute = 0x00020019,
        /// <summary>
        /// All access permissions
        /// </summary>
        AllAccess = 0x000f003f
    }

    /// <summary>
    /// Registry hive locations
    /// </summary>
    public static class RegHive
    {
        /// <summary>
        /// HKLM
        /// </summary>
        internal static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
        /// <summary>
        /// HKLU
        /// </summary>
        internal static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);
    }

    /// <summary>
    /// Exposing class to read from the registry
    /// </summary>
    public static class RegistryHandler
    {

        private const string AppName = "RegistryHandler";
        #region Functions
        /// <summary>
        /// Reading from the 64 bit hive
        /// </summary>
        /// <param name="inHive"></param>
        /// <param name="inKeyName"></param>
        /// <param name="inPropertyName"></param>
        /// <returns></returns>
        static public string GetRegistryKey64(UIntPtr inHive, String inKeyName, String inPropertyName)
        {
            Logger.Log("Reading from 64 bit registry hive", Logger.MessageLevel.Verbose, AppName);
            return GetRegKey64(inHive, inKeyName, RegistrySAM.WOW64_64Key, inPropertyName);
        }

        /// <summary>
        /// Reading from the 32 bit hive
        /// </summary>
        /// <param name="inHive"></param>
        /// <param name="inKeyName"></param>
        /// <param name="inPropertyName"></param>
        /// <returns></returns>
        static public string GetRegistryKey32(UIntPtr inHive, String inKeyName, String inPropertyName)
        {
            Logger.Log("Reading from 32 bit registry hive", Logger.MessageLevel.Verbose, AppName);
            return GetRegKey64(inHive, inKeyName, RegistrySAM.WOW64_32Key, inPropertyName);
        }

        /// <summary>
        /// Get registry key call to imports
        /// </summary>
        /// <param name="inHive"></param>
        /// <param name="inKeyName"></param>
        /// <param name="in32or64key"></param>
        /// <param name="inPropertyName"></param>
        /// <returns></returns>
        static public string GetRegKey64(UIntPtr inHive, String inKeyName, RegistrySAM in32or64key, String inPropertyName)
        {
            var hkey = 0;
            string Age;
            try
            {
                Logger.Log("Open registry handle", Logger.MessageLevel.Verbose, AppName);
                var lResult = NativeMethods.RegOpenKeyEx(inHive, inKeyName, 0, (int)RegistrySAM.QueryValue | (int)in32or64key, out hkey);
                if (0 != lResult) return null;
                uint lpType = 0;
                uint lpcbData = 1024;
                var AgeBuffer = new StringBuilder(1024);
                Logger.Log("Get value from registry", Logger.MessageLevel.Verbose, AppName);
                NativeMethods.RegQueryValueEx(hkey, inPropertyName, 0, ref lpType, AgeBuffer, ref lpcbData);
                Age = AgeBuffer.ToString();
            }
            finally
            {
                Logger.Log("Close registry handle", Logger.MessageLevel.Verbose, AppName);
                if (0 != hkey) NativeMethods.RegCloseKey(hkey);
            }
            return Age;
        }
        #endregion
    }
}
