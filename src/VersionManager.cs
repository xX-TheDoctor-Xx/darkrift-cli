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
        /// The DarkRift settings directory path.
        /// </summary>
        private static readonly string USER_DR_DIR = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".darkrift");

        /// <summary>
        /// The latest version of DarkRift.
        /// </summary>
        private static Version latestDarkRiftVersion;

        /// <summary>
        /// Gets the path to a specified installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        /// <returns>The path to the installation, or null, if it cannot be provided.</returns>
        public static string GetInstallationPath(Version version, ServerTier tier, ServerPlatform platform, bool force = false)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "installed", tier.ToString().ToLower(), platform.ToString().ToLower(), version.ToString());

            if (!Directory.Exists(fullPath) || force)
            {
                if (!Directory.Exists(fullPath))
                    Console.WriteLine($"DarkRift {version} - {tier} (.NET {platform}) not installed! Downloading package...");

                string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");

                string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/{tier}/{platform}/";
                if (tier == ServerTier.Pro)
                {
                    string invoiceNumber = GetInvoiceNumber();
                    if (invoiceNumber == null)
                    {
                        Console.Error.WriteLine(Output.Red($"You must provide an invoice number in order to download Pro DarkRift releases."));
                        return null;
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
                    return null;
                }

                Console.WriteLine($"Extracting package...");

                Directory.CreateDirectory(fullPath);

                ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

                Console.WriteLine(Output.Green($"Successfully downloaded package."));
            }
            else
                Console.WriteLine(Output.Green($"DarkRift {version} - {tier} (.NET {platform}) already installed! "));

            return fullPath;
        }

        /// <summary>
        /// Gets a list of version with specific tier and platform
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <returns>The list of versions or an empty array</returns>
        public static Version[] GetVersionDirectories(ServerTier tier, ServerPlatform platform)
        {
            try
            {
                string[] paths = Directory.GetDirectories(Path.Combine(USER_DR_DIR, "installed", $"{tier.ToString().ToLower()}", $"{platform.ToString().ToLower()}"));

                Version[] versions = new Version[paths.Length];

                // This removes the path and just leaves the version number
                for (int i = 0; i < paths.Length; i++)
                {
                    FileInfo fi = new FileInfo(paths[i]);
                    versions[i] = new Version(fi.Name);
                }

                return versions;
            }
            catch
            {
                return new Version[0];
            };
        }


        /// <summary>
        /// Lists installed DarkRift versions on the console along with the documentation
        /// </summary>
        public static void ListInstalledVersions()
        {
            // Since the free version only supports .Net Framework I'm not adding support here
            Version[] freeVersions = GetVersionDirectories(ServerTier.Free, ServerPlatform.Framework);

            // I suppose it's "pro" like this
            Version[] proFramework = GetVersionDirectories(ServerTier.Pro, ServerPlatform.Framework);
            Version[] proCore = GetVersionDirectories(ServerTier.Pro, ServerPlatform.Core);

            // Well, you gotta install it, you don't know what you are losing
            if (freeVersions.Length == 0 && proFramework.Length == 0 && proCore.Length == 0)
            {
                Console.WriteLine(Output.Red($"You don't have any version of DarkRift installed"));
                return;
            }

            foreach (Version version in freeVersions)
                PrintVersion(version, ServerTier.Free, ServerPlatform.Framework);
            foreach (Version version in proFramework)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Framework);
            foreach (Version version in proCore)
                PrintVersion(version, ServerTier.Pro, ServerPlatform.Core);
        }

        /// <summary>
        /// Prints version information on the console
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <param name="pro">Whether the pro version should be used.</param>
        /// <param name="platform">Whether the .NET Standard build should be used.</param>
        private static void PrintVersion(Version version, ServerTier tier, ServerPlatform platform)
        {
            string output = "";

            // There's no free or pro in documentation
            string documentation = string.Empty;
            try
            {
                documentation = Directory.GetDirectories(Path.Combine(USER_DR_DIR, "documentation", version.ToString()))[0];
            }
            catch { }

            output += Output.Green($"{tier} {platform} version {version}");

            if (documentation.Length > 0)
                output += Output.Green($" and it's documentation are");
            else output += Output.Green($" is");

            output += Output.Green($" installed");

            Console.WriteLine(output);
        }

        /// <summary>
        /// Queries or loads the latest version of DarkRift
        /// </summary>
        /// <returns>The latest version of DarkRift</returns>
        public static Version GetLatestDarkRiftVersion()
        {
            if (latestDarkRiftVersion != null)
                return latestDarkRiftVersion;

            Console.WriteLine("Querying server for the latest DarkRift version...");

            string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/";
            try
            {
                using (WebClient myWebClient = new WebClient())
                {
                    string latestJson = myWebClient.DownloadString(uri);

                    // Parse out 'latest' field
                    VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

                    Console.WriteLine($"Server says the latest version is {versionMetadata.Latest}.");

                    Profile profile = Profile.Load();
                    profile.LatestKnownDarkRiftVersion = versionMetadata.Latest;
                    profile.Save();

                    latestDarkRiftVersion = versionMetadata.Latest;

                    return versionMetadata.Latest;
                }
            }
            catch (WebException e)
            {
                Console.WriteLine(Output.Yellow($"Could not query latest DarkRift version from the server. Will use the last known latest instead.\n\t{e.Message}"));

                latestDarkRiftVersion = Profile.Load().LatestKnownDarkRiftVersion;

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
            Profile profile = Profile.Load();

            if (string.IsNullOrWhiteSpace(profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your recept from the Unity Asset Store.");
                Console.WriteLine("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.Error.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                profile.InvoiceNumber = invoiceNumber;
                profile.Save();
            }

            return profile.InvoiceNumber;
        }

        /// <summary>
        /// Gets the path to a specified documentation installation, downloading it if required.
        /// </summary>
        /// <param name="version">The version number required.</param>
        /// <returns>The path to the documentation, or null, if it cannot be provided.</returns>
        internal static string GetDocumentationPath(Version version, bool force = false)
        {
            string fullPath = Path.Combine(USER_DR_DIR, "documentation", version.ToString());

            if (!Directory.Exists(fullPath) || force)
            {
                if (!Directory.Exists(fullPath))
                    Console.WriteLine($"Documentation for DarkRift {version} not installed! Downloading package...");

                string stagingPath = Path.Combine(USER_DR_DIR, "Download.zip");

                string uri = $"https://www.darkriftnetworking.com/DarkRift2/Releases/{version}/Docs/";
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
                    return null;
                }

                Console.WriteLine($"Extracting package...");

                Directory.CreateDirectory(fullPath);

                ZipFile.ExtractToDirectory(stagingPath, fullPath, true);

                Console.WriteLine(Output.Green($"Successfully downloaded package."));
            }
            else
                Console.WriteLine(Output.Green($"Documentation for DarkRift {version} already installed!"));

            return fullPath;
        }
    }
}
