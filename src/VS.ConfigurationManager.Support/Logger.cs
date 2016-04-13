using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>All logging capabilites for class run through these trace listeners</summary>
    public static class Logger
    {
        private const string displayName = "VS.ConfigurationManager";
        private const string _warning = "Warning";
        private const string _error = "ERROR";
        private const string _info = "Info";
        private static readonly object _syncObject = new object();
        private static string _logLocation = String.Empty;
        private static string _ThisAssembly = String.Empty;

        /// <summary>Defining event types that can be written out.</summary>
        public enum MessageLevel
        {
            /// <summary>No value provided and default to writing errors out</summary>
            None = 0,

            /// <summary>Write an error message</summary>
            Error = 1,

            /// <summary>Write a warning message</summary>
            Warning = 2,

            /// <summary>Write informational message</summary>
            Information = 3,

            /// <summary>Write a verbose message</summary>
            Verbose = 4 
        }

        /// <summary>
        ///   Setting debug property to do verbose logging or generating warnings only.
        /// </summary>
        public static bool Debug { get; set; }

        /// <summary>
        /// Define what level of logging should be done
        /// </summary>
        public static MessageLevel LoggingLevel { get; set; }

        /// <summary>Log location used for this instance of the object</summary>
        public static string LogLocation
        {
            get
            {
                return _logLocation;
            }
            set
            {
                // If LogLocation is not set and a location has been passed in, then use the temp directory.
                _logLocation = String.IsNullOrEmpty(value) ? System.IO.Path.GetTempPath() : value;

                // Add a unique filename if a file name is not already in place
                _logLocation = _logLocation.ToUpperInvariant().Contains(".LOG") ? _logLocation :
                                        System.IO.Path.ChangeExtension
                                        (
                                        System.IO.Path.Combine
                                            (
                                            _logLocation, "ApplicationLog-" + DateTime.Now.ToString("MM-dd-yy-hhmmss", CultureInfo.InvariantCulture)
                                            ), "log"
                                        );

            }
        }

        /// <summary>
        /// Property to force information to the console window
        /// </summary>
        public static bool ConsoleOutput { get; set; }
        #region Log Overloads

        /// <summary>
        /// With passed exceptions, information is written to log
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="sourcelocation"></param>
        /// <returns></returns>
        public static string Log(Exception ex, string sourcelocation)
        {
            return Log(String.Format(CultureInfo.InvariantCulture, "Caller: {0}  Msg: {1}", GetCurrentMethod(new StackTrace(ex, true)), ex.Message), MessageLevel.Error, sourcelocation);
        }

        /// <summary>With passed exceptions, information is written to log</summary>
        /// <param name="ex"></param>
        public static string Log(Exception ex)
        {
            return Log(ex.Message, MessageLevel.Error, GetCurrentMethod(new StackTrace(ex, true)));
        }

        /// <summary>
        /// Logging source location as well as event level
        /// </summary>
        /// <param name="logtext"></param>
        /// <param name="eventlevel"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string Log(string logtext, MessageLevel eventlevel = MessageLevel.Information, string source = "Default")
        {
            // Ensure that stream writer is instantiated
            var consoleoutput = String.Empty;
            source = String.IsNullOrEmpty(source) ? "Default" : source;

            if (Debug || ConsoleOutput || eventlevel == MessageLevel.Error || eventlevel == MessageLevel.Warning)
            {
                consoleoutput = logtext;
            }
            try
            {
                switch (eventlevel)
                {
                    case 0:
                        break;
                    case MessageLevel.Error:
                        GenerateOutputMessage(logtext, MessageLevel.Error, source);
                        break;
                    case MessageLevel.Warning:
                        GenerateOutputMessage(logtext, MessageLevel.Warning, source);
                        break;
                    case MessageLevel.Information:
                        GenerateOutputMessage(logtext, MessageLevel.Information, source);
                        break;
                    case MessageLevel.Verbose:
                        GenerateOutputMessage(logtext, MessageLevel.Verbose, source);
                        break;
                }
            }
            catch(IOException ex) {
                System.Diagnostics.Debug.Write(String.Format(CultureInfo.InvariantCulture, "IO Exception hit: {0}", ex.Message));
            }

            return consoleoutput;

        }

        /// <summary>
        /// Logging source location as well as event level
        /// </summary>
        /// <param name="logtext"></param>
        /// <param name="eventlevel"></param>
        /// <param name="source"></param>
        /// <param name="consoleOut"></param>
        /// <returns></returns>
        public static string LogWithOutput(string logtext, MessageLevel eventlevel = MessageLevel.Information, string source = "Default")
        {
            Console.WriteLine(logtext);
            return Log(logtext, eventlevel, source);
        }

        #endregion Log Overloads

        private static void GenerateOutputMessage(string logtext, MessageLevel prefix, string source)
        {
            switch (LoggingLevel >= prefix)
            {
                case true:
                    var _sb = new StringBuilder();
                    var logtimestamp = DateTime.Now.ToString("MM-dd-yyyy hh:mm:ss:sss.fff", CultureInfo.InvariantCulture);
                    _sb.Append(logtimestamp.PadRight(30));
                    _sb.Append(prefix.ToString().PadRight(30));
                    _sb.Append(source.PadRight(40));
                    _sb.Append(GetCurrentMethod(GetCaller()).PadRight(30));
                    _sb.Append(logtext);
                    lock (_syncObject)
                    {
                        using (StreamWriter sw = new StreamWriter(LogLocation, true)) { sw.WriteLine(_sb.ToString()); }
                    }
                    break;
                case false:
                    break;
            }
        }

        private static StackFrame GetCaller()
        {
            // TODO: GetCaller frame is returning method names that do not appear to be correct in the log

            // Get our own, current namespace name.
            _ThisAssembly = String.IsNullOrEmpty(_ThisAssembly) ? new StackFrame(0).GetMethod().DeclaringType.ToString() : _ThisAssembly;

            // We’ll use this to walk the stack looking for a
            // method name that is not the same as our method
            // name—that is, the name of the method that called
            // this method name.
            var trace = new StackTrace(true);

            // Look for the first occurence of a stack frame that
            // contains a namespace name that is different from our
            // own.
            var i = 0;
            var frame = trace.GetFrame(i);
            while ((frame.GetMethod().DeclaringType.FullName.ToUpperInvariant() == _ThisAssembly.ToUpperInvariant()) && (i < trace.FrameCount))
            {
                i++;
                frame = trace.GetFrame(i);
            }

            return frame;
        }

        private static string GetCurrentMethod(StackFrame sf)
        {
            return sf.GetMethod().Name;
        }

        private static string GetCurrentMethod(StackTrace st = null)
        {
            return GetCurrentMethod(st.GetFrame(0));
        }
    }
}
