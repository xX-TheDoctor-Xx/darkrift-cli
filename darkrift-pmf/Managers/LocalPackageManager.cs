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
        public static void Initialize()
        {
            var manifestPath = Path.Combine(Config.GetPackageFolder(), Config.ManifestFileName);
            try
            {
                var json = File.ReadAllText(manifestPath);
                PackageList = JsonConvert.DeserializeObject<List<Package>>(json);
            }
            catch (FileNotFoundException)
            {

            }
        }

        public static bool IsPackageInstalled(string id, out Package package, out string packageDirectory)
        {
            package = null;

            packageDirectory = Path.Combine(Config.GetPackageFolder(), id);
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

            string packageDirectory = Path.Combine(Config.GetPackageFolder(), id);
            Directory.Delete(packageDirectory);

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
            string packageDirectory = Path.Combine(Directory.GetCurrentDirectory(), remotePackage.ID);

            ZipFile.ExtractToDirectory(zipPath, packageDirectory);

            remotePackage.Assets.Clear();
            remotePackage.Assets.Add(asset);

            PackageList.Add(remotePackage);
        }
    }
}
