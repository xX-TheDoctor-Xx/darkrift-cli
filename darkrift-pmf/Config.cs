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

        public static string GetTemporaryFolder()
        {
            return Path.Combine(Path.GetTempPath(), ".pmf");
        }
    }
}
