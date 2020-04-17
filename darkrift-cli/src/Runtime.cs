using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;

namespace DarkRift.Cli
{
    /// <summary>
    /// Holds a project's runtime settings.
    /// </summary>
    public class Runtime
    {
        // This ensures the version object is correctly converted
        [JsonConverter(typeof(VersionConverter))]
        /// <summary>
        /// The version of DarkRift to use.
        /// </summary>
        public Version Version { get; set; }

        // This converts enum to string and vice versa when generating or parsing json
        [JsonConverter(typeof(StringEnumConverter))]
        /// <summary>
        /// If .NET core or .NET framework should be used.
        /// </summary>
        public ServerPlatform Platform { get; set; }

        // This converts enum to string and vice versa when generating or parsing json
        [JsonConverter(typeof(StringEnumConverter))]
        /// <summary>
        /// The tier of DarkRift to use.
        /// </summary>
        public ServerTier Tier { get; set; }

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        public Runtime()
        {
        }

        /// <summary>
        /// Creates a new Runtime configuration element.
        /// </summary>
        /// <param name="version">The version of DarkRift to use.</param>
        /// <param name="tier">The tier of DarkRift to use.</param>
        /// <param name="platform">If .NET standard or .NET framework should be used.</param>
        public Runtime(Version version, ServerTier tier, ServerPlatform platform)
        {
            Version = version;
            Tier = tier;
            Platform = platform;
        }
    }
}