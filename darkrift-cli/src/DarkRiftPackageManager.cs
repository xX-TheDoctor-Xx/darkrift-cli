﻿using Crayon;
using PMF;
using PMF.Managers;
using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Reflection;

namespace DarkRift.Cli
{
    /// <summary>
    /// Wrapper for PMF that implements functionality specific to the Dark Rift CLI
    /// </summary>
    public static class DarkRiftPackageManager
    {
        /// <summary>
        /// Installs a package given the options provided in the command line - Check Options.cs
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int Install(PackageOptions opts)
        {
            PackageState state;

            Package package;

            // If PackageVersion is null we just install the latest version for the sdk
            if (opts.PackageVersion == "latest")
                state = PackageManager.InstallLatest(opts.PackageId, out package);
            else
            {
                if (opts.PackageVersion != null)
                {
                    opts.RealPackageVersion = new Version(opts.PackageVersion);
                    state = PackageManager.Install(opts.PackageId, opts.RealPackageVersion, out package);
                }
                else
                    state = PackageManager.InstallBySdkVersion(opts.PackageId, out package);
            }

            // version of the asset that was installed
            if (package != null && package.Assets.Count > 0)
                opts.RealPackageVersion = package.Assets[0].Version;

            if (state == PackageState.Installed)
                return 0;
            else // PackageState.Failed
            {
                Console.Error.WriteLine(Output.Red($"Something went wrong"));
            }

            return 1;
        }

        /// <summary>
        /// Uninstalls a package given options provided in command line - check Options.cs
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int Uninstall(PackageOptions opts)
        {
            // if true uninstall success, if false, package was not even installed
            return PackageManager.Uninstall(opts.PackageId) ? 0 : 1;
        }

        /// <summary>
        /// Updates a package given command line options - Check Options.cs
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int UpdatePackage(PackageOptions opts)
        {
            PackageState state = PackageState.Failed;

            Package package;

            if (opts.PackageVersion == "latest")
                state = PackageManager.UpdateLatest(opts.PackageId, out package);
            else
            {
                if (opts.PackageVersion != null)
                {
                    opts.RealPackageVersion = new Version(opts.PackageVersion);
                    state = PackageManager.UpdatePackage(opts.PackageId, opts.RealPackageVersion, out package);
                }
                // If PackageVersion is null we just install the latest version for the sdk
                else
                    state = PackageManager.UpdateBySdkVersion(opts.PackageId, out package);
            }

            // version of the asset that was installed
            if (package != null && package.Assets.Count > 0)
                opts.RealPackageVersion = package.Assets[0].Version;

            if (state == PackageState.Installed)
                return 0;
            else // PackageState.Failed
                Console.Error.WriteLine(Output.Red($"Something went wrong"));

            return 1;
        }

        /// <summary>
        /// Updates all the package or the CLI given provided options in cmd - check Options.cs
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int UpdatePackagesOrCli(PackageOptions opts)
        {
            // If --cli is defined we upgrade our runtime
            if (opts.UpgradeCli)
            {
                string myPath = Directory.GetDirectoryRoot(Assembly.GetEntryAssembly().Location);

                using (WebClient myWebClient = new WebClient())
                {
                    string latestJson = null;

                    try
                    {
                        latestJson = myWebClient.DownloadString(Config.DR_CLI_RELEASE_URI);
                    }
                    catch (WebException)
                    {
                        Console.WriteLine(Output.Red("Couldn't check for the latest version of the CLI"));
                        return 1;
                    }

                    // Parse out 'latest' field
                    VersionMetadata versionMetadata = VersionMetadata.Parse(latestJson);

                    var version = Assembly.GetEntryAssembly().GetName().Version;
                    var serverVersion = new Version(versionMetadata.Latest);

                    if (serverVersion == version)
                    {
                        Console.WriteLine("DarkRift CLI is up to date");
                        return 0;
                    }
                    else if (serverVersion > version)
                    {
                        Console.WriteLine($"Most recent CLI version is {versionMetadata.Latest}.");
                        Console.WriteLine($"Current version installed is {version}");
                        Console.WriteLine("Updating...");

                        string stagingPath = Path.Combine(Config.USER_DR_DIR, "DownloadCLI.zip");

                        string uriDownload = $"{Config.DR_CLI_RELEASE_URI}/{serverVersion}";

                        try
                        {
                            myWebClient.DownloadFile(uriDownload, stagingPath);
                        }
                        catch (WebException)
                        {
                            Console.WriteLine(Output.Red($"Couldn't download DarkRift CLI {serverVersion}"));
                            return 1;
                        }

                        Console.WriteLine($"Extracting package...");

                        ZipFile.ExtractToDirectory(stagingPath, myPath, true);

                        Console.WriteLine(Output.Green($"Successfully downloaded and installed DarkRift CLI version {serverVersion}"));

                        return 0;
                    }
                    // This shouldn't happen
                    else
                    {
                        Console.WriteLine("How can you possibly have a greater version than it is on the server???");
                        return 1;
                    }
                }
            }
            // If it is not defined it just updates all packages to the latest version of the sdk
            else
            {
                foreach (Package package in PackageManager.PackageList)
                {
                    PackageState state = PackageManager.UpdateBySdkVersion(package.ID, out Package p);
                    // check if success
                    if (state != PackageState.Installed)
                        Console.WriteLine(Output.Red($"Something went wrong updating {package.ID}"));
                }
            }

            return 0;
        }

        /// <summary>
        /// Wrapper method for UpdatePackage() and UpdatePackagesOrCli()
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int Update(PackageOptions opts)
        {
            // if we have a package id we update that package
            // if we dont we just update them all or the cli if option is specified
            return !string.IsNullOrEmpty(opts.PackageId) ? UpdatePackage(opts) : UpdatePackagesOrCli(opts);
        }
    }
}
