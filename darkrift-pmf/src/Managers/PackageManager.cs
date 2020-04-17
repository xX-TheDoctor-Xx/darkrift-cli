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
        public static PackageState Install(string id, Version version, out Package package)
        {
            package = null;

            // check if is already installed
            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string packageDirectory))
            {
                // get package info for version
                Package remotePackage = RemotePackageManager.GetPackageInfo(id);

                if (remotePackage == null)
                    return PackageState.NotExisting;
                
                Asset asset = remotePackage.GetAssetVersion(version);

                if (asset == null)
                    return PackageState.VersionNotFound;

                // If it is not installed, packageDirectory will have the value of the directory where the package should be
                string zipFile = RemotePackageManager.DownloadAsset(id, asset);
                LocalPackageManager.InstallPackage(remotePackage, asset, zipFile, out package);
                return PackageState.Installed;
            }
            else
            {
                return PackageState.AlreadyInstalled;
            }
        }

        /// <summary>
        /// Installs a package to the most recent version given an sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true update succes, false update failed or cancelled</returns>
        public static PackageState InstallBySdkVersion(string id, out Package package)
        {
            package = null;

            Package remotePackage = RemotePackageManager.GetPackageInfo(id);

            if (remotePackage == null)
                return PackageState.NotExisting;

            Asset asset = RemotePackageManager.GetAssetLatestVersionBySdkVersion(remotePackage);

            if (asset == null)
                return PackageState.VersionNotFound;

            if (validateSdkVersion(asset))
            {
                return Install(id, asset.Version, out package);
            }

            return PackageState.Failed;
        }

        public static bool Uninstall(string id)
        {
            return LocalPackageManager.RemovePackage(id);
        }

        /// <summary>
        /// Updates a package to the most recent version regardless of sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true update succes, false update failed or cancelled</returns>
        public static PackageState UpdateLatest(string id, out Package package)
        {
            package = null;

            var remotePackage = RemotePackageManager.GetPackageInfo(id);

            if (remotePackage == null)
                return PackageState.NotExisting;

            var asset = RemotePackageManager.GetAssetLatestVersion(remotePackage);

            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string pd))
                return PackageState.NotInstalled;

            // You already have the latest version
            if (localPackage.Assets[0].Version == asset.Version)
                return PackageState.UpToDate;

            if (validateSdkVersion(asset))
            {
                Uninstall(id);
                return Install(id, asset.Version, out package);
            }

            return PackageState.Cancelled;
        }

        /// <summary>
        /// Updates a package to the most recent version given an sdk version
        /// </summary>
        /// <param name="id"></param>
        /// <returns>true if update success, false if package is not installed</returns>
        public static PackageState UpdateBySdkVersion(string id, out Package package)
        {
            package = null;

            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string pd))
                return PackageState.NotInstalled;

            var remotePackage = RemotePackageManager.GetPackageInfo(id);

            if (remotePackage == null)
                return PackageState.NotExisting;

            var asset = RemotePackageManager.GetAssetLatestVersionBySdkVersion(remotePackage);

            // You already have the latest version
            if (localPackage.Assets[0].Version == asset.Version)
                return PackageState.UpToDate;

            if (validateSdkVersion(asset))
            {
                Uninstall(id);
                return InstallBySdkVersion(id, out package);
            }

            return PackageState.Cancelled;
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
                    return true;
            }
        }
    }
}
