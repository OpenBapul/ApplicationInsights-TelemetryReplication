using System;

namespace ApplicationInsights.TelemetryReplication
{
    /// <summary>
    /// Represents an application identifier.
    /// </summary>
    public class AppId
    {
        public AppId(string name) : this(name, null, null) { }
        public AppId(string name, Version version) : this(name, version, null) { }
        public AppId(string name, Version version, string environment)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            if (version == null)
            {
                version = new Version("1.0.0");
            }
            Name = name;
            Version = version;
            Environment = environment;
        }

        /// <summary>
        /// The name of the application.
        /// </summary>
        public string Name { get; }
        /// <summary>
        /// The Assembly version of the application.
        /// </summary>
        public Version Version { get; }
        /// <summary>
        /// The name of the environment where the application running.
        /// </summary>
        public string Environment { get; }
    }
}