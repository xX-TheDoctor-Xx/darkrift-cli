using System;
using System.IO;

// Note: All links must not end with a slash

namespace DarkRift.Cli
{
    public static class Config
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        public static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// DarkRift executable name for .Net Framework
        /// </summary>
        public static readonly string DR_EXECUTABLE_NAME = "DarkRift.Server.Console.exe";

        /// <summary>
        /// DarkRift Dll name for .Net Core
        /// </summary>
        public static readonly string DR_DLL_NAME = "DarkRift.Server.Console.dll";

        /// <summary>
        /// DarkRift CLI releases link
        /// </summary>
        public static readonly string DR_CLI_RELEASE_URI = "https://www.darkriftnetworking.com/DarkRiftCLI/Releases";

        /// <summary>
        /// DarkRift 2 releases link
        /// </summary>
        public static readonly string DR_DR2_RELEASE_URI = "https://www.darkriftnetworking.com/DarkRift2/Releases";

        /// <summary>
        /// DarkRift 2 Documentation link
        /// </summary>
        public static readonly string DR_DR2_DOCS_URI = "https://darkriftnetworking.com/DarkRift2/Docs";
    }
}
