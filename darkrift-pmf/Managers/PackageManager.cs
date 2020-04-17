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
            // get package info for version
            Package remotePackage = RemotePackageManager.GetPackageInfo(id);
            Asset asset = remotePackage.GetAssetVersion(version);

            // check if is already installed
            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string packageDirectory))
            {
                // If it is not installed, packageDirectory will have the value of the directory where the package should be
                string zipFile = RemotePackageManager.DownloadAsset(asset);
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
            Asset asset = RemotePackageManager.GetAssetLatestVersionBySdkVersion(remotePackage);

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

            return false;
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
            {

            }
            else if (asset.SdkVersion < Config.CurrentSdkVersion)
            {

            }

            return true;
        }
    }
}
