using System;
using System.IO;

namespace DarkRift.Cli
{
    public static class Config
    {
        /// <summary>
        /// The DarkRift settings directory path.
        /// </summary>
        public static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        public static readonly string DR_EXECUTABLE_NAME = "DarkRift.Server.Console.exe";

        public static readonly string DR_DLL_NAME = "DarkRift.Server.Console.dll";
    }
}
