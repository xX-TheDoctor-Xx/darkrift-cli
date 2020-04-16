using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace DarkRift.PMF.Managers
{
    public static class RemotePackageManager
    {
        /// <summary>
        /// Gets package info from the server along with ALL the assets in the json
        /// </summary>
        /// <param name="id"></param>
        /// <returns>The package object downloaded</returns>
        public static Package GetPackageInfo(string id)
        {
            using (WebClient client = new WebClient())
            {
                string json = client.DownloadString($"{Config.RepositoryEndpoint}/{id}");
                return JsonConvert.DeserializeObject<Package>(json);
            }
        }

        /// <summary>
        /// Downloads a specific version of a certain package
        /// </summary>
        /// <param name="asset"></param>
        /// <returns>The zip file which was downloaded</returns>
        public static string DownloadAsset(Asset asset)
        {
            using (WebClient client = new WebClient())
            {
                var zipPath = Path.Combine(Config.GetTemporaryFolder(), asset.FileName);
                client.DownloadFile(asset.Url, zipPath);
                return zipPath;
            }
        }

        /// <summary>
        /// Gets you the latest version of a package
        /// </summary>
        /// <param name="package"></param>
        /// <returns>The latest asset version of a given package</returns>
        public static Asset GetAssetLatestVersion(Package package)
        {
            if (package == null)
                throw new ArgumentNullException();
            if (package.Assets.Count == 0)
                throw new ArgumentNullException("asset count");

            Asset ret_asset = null;
            foreach (var asset in package.Assets)
            {
                if (ret_asset == null || ret_asset.Version < asset.Version)
                    ret_asset = asset;
            }

            return ret_asset;
        }

        /// <summary>
        /// Gets you the latest version of a package given an SDK version
        /// </summary>
        /// <param name="package"></param>
        /// <param name="sdkVersion"></param>
        /// <returns>The latest asset version of a given package and given SDK version</returns>
        public static Asset GetAssetLatestVersion(Package package, Version sdkVersion)
        {
            if (package == null)
                throw new ArgumentNullException();
            if (package.Assets.Count == 0)
                throw new ArgumentNullException("asset count");

            Asset ret_asset = null;
            foreach (var asset in package.Assets)
            {
                if (asset.SdkVersion == sdkVersion)
                {
                    if (ret_asset == null || ret_asset.Version < asset.Version)
                        ret_asset = asset;
                }
            }

            return ret_asset;
        }
    }
}
