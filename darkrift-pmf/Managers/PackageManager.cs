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
        /// <returns>Installation successful</returns>
        public static bool Install(string id, Version version)
        {
            // get package info for version
            Package remotePackage = RemotePackageManager.GetPackageInfo(id);
            Asset asset = remotePackage.GetAssetVersion(version);

            // check if is already installed
            if (!LocalPackageManager.IsPackageInstalled(id, out Package localPackage, out string packageDirectory))
            {
                string zipFile = RemotePackageManager.DownloadAsset(asset);
                LocalPackageManager.InstallPackage(remotePackage, asset, zipFile);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
