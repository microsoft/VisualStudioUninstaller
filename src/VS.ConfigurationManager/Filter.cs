using Microsoft.VS.ConfigurationManager.Support;
using System;
using System.Globalization;

namespace Microsoft.VS.ConfigurationManager
{
    /// <summary>
    ///      This class supports search and replace of strings in bundles. It helps in shortening
    ///      text for easier readability.
    /// </summary>
    public class Filter
    {
        private const string AppName = "Filter";
        #region Public Methods
        /// <summary>
        /// Create a filter class object with parameters added.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static Filter CreateFilter(string name, string source, string target)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Creating Filter - name: {0}, source: {1}, target: {2}", name, source, target), Logger.MessageLevel.Information, AppName);
            var filter = new Filter
            {
                Name = name,
                ReplaceSource = source,
                ReplaceValue = target
            };

            return filter;
        }
        #endregion Public Methods
        #region Public Properties
        /// <summary>
        /// Name/description of what this text filter does.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// What would you like to search for to replace?
        /// </summary>
        public string ReplaceSource { get; set; }
        /// <summary>
        /// What are you going to replace the ReplaceSource with?
        /// </summary>
        public string ReplaceValue { get; set; }

        #endregion Public Properties
    }
}
