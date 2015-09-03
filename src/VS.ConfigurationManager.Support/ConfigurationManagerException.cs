using System;
using System.Runtime.Serialization;

namespace Microsoft.VS.ConfigurationManager.Support
{
    /// <summary>
    /// Custom exceptions derived from base class
    /// </summary>
    [Serializable()]
    public class ConfigurationManagerException : Exception, ISerializable
    {
        /// <summary>
        /// Default constructor for exception
        /// </summary>
        public ConfigurationManagerException() {  }
        /// <summary>
        /// Constructor with message only
        /// </summary>
        /// <param name="message"></param>
        public ConfigurationManagerException(string message) : base(message) {  }
        /// <summary>
        /// Constructor with message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public ConfigurationManagerException(string message, Exception inner) : base(message, inner) {  }
        /// <summary>
        /// Protected constructor for exception
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected ConfigurationManagerException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception to let user know that a reboot is required.
    /// </summary>
    [Serializable()]
    public class RebootRequiredException : ConfigurationManagerException
    {
        /// <summary>
        /// Default constructor for exception
        /// </summary>
        public RebootRequiredException() { }
        /// <summary>
        /// Constructor with message only
        /// </summary>
        /// <param name="message"></param>
        public RebootRequiredException(string message) : base(message) { }
        /// <summary>
        /// Constructor with message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public RebootRequiredException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Protected constructor for exception
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected RebootRequiredException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Exception when WixPdbs are unavailable and configuration files are unavailable.
    /// </summary>
    [Serializable()]
    public class NoSourceFilesAvailableForParsingException : ConfigurationManagerException
    {
        /// <summary>
        /// Default constructor for exception
        /// </summary>
        public NoSourceFilesAvailableForParsingException() { }
        /// <summary>
        /// Constructor with message only
        /// </summary>
        /// <param name="message"></param>
        public NoSourceFilesAvailableForParsingException(string message) : base(message) { }
        /// <summary>
        /// Constructor with message and inner exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public NoSourceFilesAvailableForParsingException(string message, Exception inner) : base(message, inner) { }
        /// <summary>
        /// Protected constructor for exception
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        protected NoSourceFilesAvailableForParsingException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
