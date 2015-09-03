using System;
using System.Runtime.InteropServices;

namespace Microsoft.VS.ConfigurationManager.Support
{
    internal static class NativeMethods
    {
        #region Read 64bit Reg from 32bit app
        [DllImport("Advapi32.dll")]
        internal static extern uint RegOpenKeyEx(
            UIntPtr hKey,
            string lpSubKey,
            uint ulOptions,
            int samDesired,
            out int phkResult);

        [DllImport("Advapi32.dll")]
        internal static extern uint RegCloseKey(int hKey);

        /// <summary>
        /// Importing call to allow reading under 3.5
        /// </summary>
        /// <param name="hKey"></param>
        /// <param name="lpValueName"></param>
        /// <param name="lpReserved"></param>
        /// <param name="lpType"></param>
        /// <param name="lpData"></param>
        /// <param name="lpcbData"></param>
        /// <returns></returns>
        [DllImport("advapi32.dll", EntryPoint = @"RegQueryValueEx")]
        internal static extern int RegQueryValueEx(
            int hKey, string lpValueName,
            int lpReserved,
            ref uint lpType,
            System.Text.StringBuilder lpData,
            ref uint lpcbData);
        #endregion
    }
}
