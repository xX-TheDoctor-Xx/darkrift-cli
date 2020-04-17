using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DarkRift.PMF
{
    public static class Config
    {
        // This is project manifest, not package manifests, those are handled automagically
        public static string ManifestFileName { get; set; }

        public static string PackageInstallationFolder { get; set; }

        public static string RepositoryEndpoint { get; set; }

        public static Version CurrentSdkVersion { get; set; }

        public static bool IsDebugging { get; set; }

        public static string TemporaryFolder = ".pmf-temp";

        public static void DEBUG(string message)
        {
            Console.WriteLine("DEBUG: " + message);
        }
    }
}
