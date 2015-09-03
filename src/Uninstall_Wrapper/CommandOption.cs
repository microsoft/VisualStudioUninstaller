using System;
using Microsoft.VS.ConfigurationManager.Support;
using System.Globalization;

namespace Microsoft.VS.Uninstaller
{
    /// <summary>
    /// Option class to describe name-value pairs.
    /// </summary>
    public class CommandOption
    {
        private static readonly string AppName = "CommandOption";
        /// <summary>
        /// This is a descriptor of a command that can be run via a console application.
        /// </summary>
        public string Command { get; set; }
        /// <summary>
        /// This is a description showed to the user of what the command does.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// What value should we try to check for comparison?
        /// </summary>
        public string CommandCompareValue { get; set; }
        /// <summary>
        /// What is the value of the parameter
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// Creating a new option takes the 4 properties values as parameters
        /// </summary>
        /// <param name="command"></param>
        /// <param name="description"></param>
        /// <param name="commandcomparevalue"></param>
        /// <param name="value"></param>
        public CommandOption(string command, string description, string commandcomparevalue, string value)
        {
            Logger.Log(String.Format(CultureInfo.InvariantCulture, "Creating option: Command: {0}, Description: {1}, Keyword: {2}, Comparison value: {3}", command, description, commandcomparevalue, value), Logger.MessageLevel.Verbose, AppName);
            Command = command;
            Description = description;
            CommandCompareValue = commandcomparevalue;
            Value = value;
        }
    }
}
