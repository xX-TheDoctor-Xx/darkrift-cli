﻿using System.IO;
using System;
using System.Net;
using System.IO.Compression;
using Crayon;
using System.Collections.Generic;

namespace DarkRift.Cli
{
    /// <summary>
    /// Manages DarkRift installations.
    /// </summary>
    internal class VersionManager
    {
        /// <summary>
        /// The latest version of DarkRift.
        /// </summary>
        private static string latestDarkRiftVersion;

        /// <summary>
        /// Gets the path to a specified installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it cannot be provided.</returns>
        public static string GetInstallationPath(string version, ServerTier tier, ServerPlatform platform)
        {
            return Path.Combine(Config.USER_DR_DIR, "installed", tier.ToString().ToLower(), platform.ToString().ToLower(), version);
        }

        /// <summary>
        /// Downloads and installs a DarkRift version
        /// </summary>
        /// <param name="version">The version to be installed</param>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns>True if installed successfully otherwise false</returns>
        public static bool DownloadVersion(string version, ServerTier tier, ServerPlatform platform)
        {
            string fullPath = GetInstallationPath(version, tier, platform);

            string stagingPath = Path.Combine(Config.USER_DR_DIR, "Download.zip");

            string uri = $"{Config.DR_DR2_RELEASE_URI}/{version}/{tier}/{platform}/";
            if (tier == ServerTier.Pro)
            {
                string invoiceNumber = GetInvoiceNumber();
                if (invoiceNumber == null)
                {
                    Console.Error.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                    return false;
                }

                uri += $"?invoice={invoiceNumber}";
            }

            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(uri, stagingPath);
                }
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download DarkRift {version} - {tier} (.NET {platform}):\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(fullPath);

            ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

            Console.WriteLine(Output.Green($"Successfully downloaded DarkRift {version} - {tier} (.NET {platform})"));

            return true;
        }

        /// <summary>
        /// Checks if a version of Dark Rift is installed
        /// </summary>
        /// <param name="version">Version to be checked</param>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns>True if is installed otherwise false</returns>
        public static bool IsVersionInstalled(string version, ServerTier tier, ServerPlatform platform)
        {
            return GetVersions(tier, platform).Contains(version);
        }

        /// <summary>
        /// Gets a list of versions with specific tier and platform
        /// </summary>
        /// <param name="tier">The tier</param>
        /// <param name="platform">The platform</param>
        /// <returns>List of paths to the versions</returns>
        public static List<string> GetVersions(ServerTier tier, ServerPlatform platform)
        {
            var installationFolder = GetInstallationPath("", tier, platform);

            List<string> versions = new List<string>();

            if (Directory.Exists(installationFolder))
            {
                string[] paths = Directory.GetDirectories(installationFolder);

                // This removes the path and just leaves the version number
                for (int i = 0; i < paths.Length; i++)
                {
                    versions.Add(Path.GetFileName(paths[i]));
                }
            }

            return versions;
        }

        /// <summary>
        /// Lists installed DarkRift versions on the console along with the documentation
        /// </summary>
        public static void ListInstalledVersions()
        {
            // Since the free version only supports .Net Framework I'm not adding support here
            List<string> freeVersions = GetVersions(ServerTier.Free, ServerPlatform.Framework);

            List<string> proFramework = GetVersions(ServerTier.Pro, ServerPlatform.Framework);
            List<string> proCore = GetVersions(ServerTier.Pro, ServerPlatform.Core);

            // Well, you gotta install it, you don't know what you are losing
            if (freeVersions.Count == 0 && proFramework.Count == 0 && proCore.Count == 0)
            {
                Console.Error.WriteLine(Output.Red($"You don't have any versions of DarkRift installed"));
                return;
            }

            foreach (string version in freeVersions)
                PrintVersion(version, ServerTier.Free, ServerPlatform.Framework);
            foreach (string version in proFramework)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Framework);
            foreach (string version in proCore)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Core);
        }

        /// <summary>
        /// Prints formatted DarkRift version information on the console
        /// </summary>
        /// <param name="version">The version to be printed.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        private static void PrintVersion(string version, ServerTier tier, ServerPlatform platform)
        {
            string output = "";

            // There's no free or pro in documentation

            output += $"DarkRift {version} - {tier} (.NET {platform})";

            if (Directory.Exists(Path.Combine(Config.USER_DR_DIR, "documentation", version)))
                output += " and its documentation are";
            else output += " is";

            output += " installed";

            Console.WriteLine(output);
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public static string GetLatestDarkRiftVersion()
        {
            if (latestDarkRiftVersion != null)
                return latestDarkRiftVersion;

            Console.WriteLine("Querying server for the latest DarkRift version...");

            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    string latestJson = myWebClient.DownloadString(Config.DR_DR2_RELEASE_URI);

                    // Parse out 'latest' field
                    VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

                    Console.WriteLine($"Server says the latest version is {versionMetadata.Latest}.");

                    Profile.LatestKnownDarkRiftVersion = versionMetadata.Latest;
                    Profile.Save();

                    latestDarkRiftVersion = versionMetadata.Latest;

                    return versionMetadata.Latest;
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(Output.Yellow($"Could not query latest DarkRift version from the server. Will use the last known latest instead.\n\t{e.Message}"));

                latestDarkRiftVersion = Profile.LatestKnownDarkRiftVersion;

                if (latestDarkRiftVersion == null)
                {
                    Console.Error.WriteLine(Output.Red($"No latest DarkRift version stored locally!"));
                    return null;
                }

                Console.WriteLine($"Last known latest version is {latestDarkRiftVersion}.");

                return latestDarkRiftVersion;
            }
        }

        /// <summary>
        /// Returns the user's invoice number, or prompts for it if not set.
        /// </summary>
        /// <returns>The user's invoice number, or null if they do not have one.</returns>
        private static string GetInvoiceNumber()
        {
            if (string.IsNullOrWhiteSpace(Profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your recept from the Unity Asset Store.");
                Console.WriteLine("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.Error.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                Profile.InvoiceNumber = invoiceNumber;
                Profile.Save();
            }

            return Profile.InvoiceNumber;
        }

        /// <summary>
        /// Gets the path to a specified documentation installation
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the documentation, or null, if it cannot be provided.</returns>
        public static string GetDocumentationPath(string version)
        {
            return Path.Combine(Config.USER_DR_DIR, "documentation", version);
        }

        /// <summary>
        /// Downloads and installs the documentation of a version of DarkRift
        /// </summary>
        /// <param name="version">The version of DarkRift</param>
        /// <returns>True for success otherwise false</returns>
        public static bool DownloadDocumentation(string version)
        {
            string fullPath = GetDocumentationPath(version);

            string stagingPath = Path.Combine(Config.USER_DR_DIR, "Download.zip");

            string uri = $"{Config.DR_DR2_RELEASE_URI}/{version}/Docs/";

            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    myWebClient.DownloadFile(uri, stagingPath);
                }
            }
            catch (WebException e)
            {
                Console.Error.WriteLine(Output.Red($"Could not download documentation for DarkRift {version}:\n\t{e.Message}"));
                return false;
            }

            Console.WriteLine($"Extracting package...");

            Directory.CreateDirectory(fullPath);

            ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

            Console.WriteLine(Output.Green($"Successfully downloaded documentation for version {version}"));

            return true;
        }

        /// <summary>
        /// Checks if documentation for a specific version exists
        /// </summary>
        /// <param name="version">Version of Dark Rift</param>
        /// <returns>True if documentation found otherwise false</returns>
        public static bool IsDocumentationInstalled(string version)
        {
            return Directory.Exists(GetDocumentationPath(version));
        }
    }
}
