using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Text;

namespace DarkRift.PMF.Managers
{
    public static class PackageManager
    {
        /// <summary>
        /// Installs a package given a version
        /// </summary>
        /// <param name="id">The id of the package</param>
        /// <param name="version">The version of the asset</param>
        /// <returns>true Installation successful, false already installed</returns>
        public static bool Install(string id, Version version)
        {
            // check if is already installed
            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string packageDirectory))
            {
                // get package info for version
                Package remotePackage = RemotePackageManager.GetPackageInfo(id);

                if (remotePackage == null)
                    return false;

                Asset asset = remotePackage.GetAssetVersion(version);

                // If it is not installed, packageDirectory will have the value of the directory where the package should be
                string zipFile = RemotePackageManager.DownloadAsset(id, asset);
                LocalPackageManager.InstallPackage(remotePackage, asset, zipFile);
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Installs a package to the most recent version given an sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true update succes, false update failed or cancelled</returns>
        public static bool InstallBySdkVersion(string id)
        {
            Package remotePackage = RemotePackageManager.GetPackageInfo(id);

            if (remotePackage == null)
                return false;

            Asset asset = RemotePackageManager.GetAssetLatestVersionBySdkVersion(remotePackage);

            if (asset == null)
            {
                Console.WriteLine($"Asset with SDK Version - {Config.CurrentSdkVersion} - was not found");
                return false;
            }

            if (validateSdkVersion(asset))
            {
                return Install(id, asset.Version);
            }

            return false;
        }

        public static bool Uninstall(string id)
        {
            if (LocalPackageManager.IsPackageInstalled(id, out Package package, out string packageDirectory))
                LocalPackageManager.RemovePackage(id);

            return true;
        }

        /// <summary>
        /// Updates a package to the most recent version given an sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static bool UpdateBySdkVersion(string id)
        {
            // normal update
            if (Uninstall(id))
                return InstallBySdkVersion(id);

            return false;
        }

        /// <summary>
        /// Updates a package to the most recent version regardless of sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true update succes, false update failed or cancelled</returns>
        public static bool UpdateLatest(string id)
        {
            Uninstall(id);

            var remotePackage = RemotePackageManager.GetPackageInfo(id);

            if (remotePackage == null)
                return false;

            var asset = RemotePackageManager.GetAssetLatestVersion(remotePackage);

            if (validateSdkVersion(asset))
            {
                return Install(id, asset.Version);
            }

            return false;
        }

        private static bool validateSdkVersion(Asset asset)
        {
            if (asset.SdkVersion > Config.CurrentSdkVersion)
                return askUser("You are installing a package which the sdk version is more recent than what you have. Would you like to continue?");
            else if (asset.SdkVersion < Config.CurrentSdkVersion)
                return askUser("You are installing a package which the sdk version is older than what you have. Would you like to continue?");

            return true;
        }

        /// <summary>
        /// Just asks the user something
        /// </summary>
        /// <returns>true yes, false no</returns>
        private static bool askUser(string question)
        {
            Console.WriteLine($"{question} [Y][N]");
            while (true)
            {
                char answer = char.ToLower(Console.ReadKey().KeyChar);
                if (answer == 'n')
                    return false;
                else if (answer == 'y')
                    return false;
            }
        }
    }
}
