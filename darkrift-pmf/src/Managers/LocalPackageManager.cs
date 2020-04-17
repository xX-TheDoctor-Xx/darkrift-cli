using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DarkRift.PMF.Managers
{
    /// <summary>
    /// Manages all the local files
    /// </summary>
    public static class LocalPackageManager
    {
        public static List<Package> PackageList { get; private set; }

        /// <summary>
        /// Does all the checking locally when the program starts
        /// THIS NEEDS TO BE CALLED!
        /// </summary>
        public static void Start()
        {
            validateManifestFile();

            try
            {
                var json = File.ReadAllText(Config.ManifestFileName);
                PackageList = JsonConvert.DeserializeObject<List<Package>>(json);
            }
            catch (FileNotFoundException)
            {
                // Something failed with validateManifestFile()
            }
        }

        /// <summary>
        /// Saves everything to disk
        /// THIS NEEDS TO BE CALLED!
        /// </summary>
        public static void Stop()
        {
            validateManifestFile();

            var json = JsonConvert.SerializeObject(PackageList);

            try
            {
                File.WriteAllText(Config.ManifestFileName, json);
                Directory.Delete(Config.TemporaryFolder, true);
            }
            catch (IOException)
            {
                // Something failed with validateManifestFile()
            }
        }

        private static void validateManifestFile()
        {
            if (!File.Exists(Config.ManifestFileName))
                File.Create(Config.ManifestFileName).Close();
            if (PackageList == null)
                PackageList = new List<Package>();
        }

        public static bool IsPackageInstalled(string id, out Package package, out string packageDirectory)
        {
            package = null;

            packageDirectory = Path.Combine(Config.PackageInstallationFolder, id);
            if (!Directory.Exists(packageDirectory))
                return false;

            try
            {
                package = PackageList.GetPackage(id);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void RemovePackage(string id)
        {
            if (string.IsNullOrEmpty(id))
                throw new InvalidOperationException();

            try
            {
                string packageDirectory = Path.Combine(Config.PackageInstallationFolder, id);
                Directory.Delete(packageDirectory, true);
            }
            catch
            {
                // Do nothing, user probably already deleted the folder
            }

            PackageList.Remove(id);
        }

        /// <summary>
        /// Extracts zip files and registeres this package as installed
        /// </summary>
        /// <param name="remotePackage">The package which is to be installed</param>
        /// <param name="asset">The version of the asset being installed</param>
        /// <param name="zipPath"></param>
        public static void InstallPackage(Package remotePackage, Asset asset, string zipPath)
        {
            string packageDirectory = Path.Combine(Config.PackageInstallationFolder, remotePackage.ID);
            ZipFile.ExtractToDirectory(zipPath, packageDirectory);

            remotePackage.Assets.Clear();
            remotePackage.Assets.Add(asset);

            PackageList.Add(remotePackage);
        }
    }
}
