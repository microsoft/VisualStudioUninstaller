using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    /// Listing of operating systems and their versions for use with UninstallAction
    /// </summary>
    public class OperatingSystemConfiguration : IEnumerable<OperatingSystemConfiguration>
    {
        private const string AppName = "OperatingSystemConfiguration";
        private static List<OperatingSystemConfiguration> _oslist = new List<OperatingSystemConfiguration>();
        /// <summary>
        /// Setting the value property
        /// </summary>
        /// <param name="value"></param>
        public OperatingSystemConfiguration(string value) { Value = value; }
        /// <summary>
        /// The version number for the given instance.
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Windows 2000 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows2000 { get { return new OperatingSystemConfiguration("5.0"); } }
        /// <summary>
        /// Windows XP version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsXP { get { return new OperatingSystemConfiguration("5.1"); } }
        /// <summary>
        /// Windows XP 64-bit version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsXP64Bit { get { return new OperatingSystemConfiguration("5.2"); } }
        /// <summary>
        /// Windows 2003 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows2003 { get { return new OperatingSystemConfiguration("5.2"); } }
        /// <summary>
        /// Windows 2003R2 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows2003R2 { get { return new OperatingSystemConfiguration("5.2"); } }
        /// <summary>
        /// Windows Vista version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsVista { get { return new OperatingSystemConfiguration("6.0"); } }
        /// <summary>
        /// Windows Server 2008 version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsServer2008 { get { return new OperatingSystemConfiguration("6.0"); } }
        /// <summary>
        /// Windows Server 2008R2 version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsServer2008R2 { get { return new OperatingSystemConfiguration("6.1"); } }
        /// <summary>
        /// Windows 7 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows7 { get { return new OperatingSystemConfiguration("6.1"); } }
        /// <summary>
        /// Windows Server 2012 version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsServer2012 { get { return new OperatingSystemConfiguration("6.2"); } }
        /// <summary>
        /// Windows 8 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows8 { get { return new OperatingSystemConfiguration("6.2"); } }
        /// <summary>
        /// Windows 8.1 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows81 { get { return new OperatingSystemConfiguration("6.3"); } }
        /// <summary>
        /// Windows Server 2012R2 version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsServer2012R2 { get { return new OperatingSystemConfiguration("6.3"); } }
        /// <summary>
        /// Windows 10 version number
        /// </summary>
        public static OperatingSystemConfiguration Windows10 { get { return new OperatingSystemConfiguration("10.0"); } }
        /// <summary>
        /// Windows Server Technical Preview version number
        /// </summary>
        public static OperatingSystemConfiguration WindowsServerTechnicalPreview { get { return new OperatingSystemConfiguration("10.0"); } }

        /// <summary>
        /// Overloaded function for generating a list of operating systems
        /// </summary>
        /// <returns></returns>
        public static ICollection<OperatingSystemConfiguration> ToList()
       {
            Logger.Log("Creating list of OSes available for detection", Logger.MessageLevel.Information, AppName);
            if (_oslist.FirstOrDefault() == null)
            {
                _oslist.Add(Windows2000);
                _oslist.Add(WindowsXP);
                _oslist.Add(WindowsXP64Bit);
                _oslist.Add(Windows2003);
                _oslist.Add(Windows2003R2);
                _oslist.Add(WindowsVista);
                _oslist.Add(WindowsServer2008);
                _oslist.Add(WindowsServer2008R2);
                _oslist.Add(Windows7);
                _oslist.Add(WindowsServer2012);
                _oslist.Add(Windows8);
                _oslist.Add(Windows81);
                _oslist.Add(WindowsServer2012R2);
                _oslist.Add(Windows10);
                _oslist.Add(WindowsServerTechnicalPreview);

            }
            return _oslist;
        }
        /// <summary>
        /// Referencing list via index value
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public OperatingSystemConfiguration this[int index]
        {
            get { return this[index]; }
            set { _oslist.Insert(index, value); }
        }

        /// <summary>
        /// Send back list in context of collections
        /// </summary>
        /// <returns></returns>
        public IEnumerator<OperatingSystemConfiguration> GetEnumerator()
        {
            return _oslist.GetEnumerator();
        }

        /// <summary>
        /// System.Collections.IEnumerator implementation
        /// </summary>
        /// <returns></returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
