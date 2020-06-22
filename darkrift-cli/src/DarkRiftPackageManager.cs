using Crayon;
using PMF;
using PMF.Managers;
using System;
using System.IO;
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

            // If PackageVersion is null we just install the latest version for the sdk
            if (opts.Latest)
                state = PackageManager.InstallLatest(opts.PackageId, out Package package);
            else if (opts.PackageVersion != null)
                state = PackageManager.Install(opts.PackageId, opts.PackageVersion, out Package package);
            else
                state = PackageManager.InstallBySdkVersion(opts.PackageId, out Package package);

            if (state == PackageState.Installed)
            {
                Console.WriteLine(Output.Green($"{opts.PackageId}@{opts.PackageVersion} was installed successfully"));
                return 0;
            }
            else if (state == PackageState.AlreadyInstalled)
            {
                Console.WriteLine($"{opts.PackageId} is already installed");
                return 0;
            }
            else if (state == PackageState.NotExisting)
            {
                Console.Error.WriteLine($"Couldn't find {opts.PackageId}");
            }
            else if (state == PackageState.VersionNotFound)
            {
                Console.Error.WriteLine($"Couldn't find package version with SDK version {PMF.Config.CurrentSdkVersion}");
            }
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
            if (PackageManager.Uninstall(opts.PackageId))
            {
                Console.WriteLine(Output.Green($"{opts.PackageId} was uninstalled succesfully"));
                return 0;
            }
            else
            {
                Console.Error.WriteLine($"Couldn't find {opts.PackageId}");
                return 1;
            }
        }

        /// <summary>
        /// Updates a package given command line options - Check Options.cs
        /// </summary>
        /// <param name="opts">PackageOptions from cmd</param>
        /// <returns>Exit code</returns>
        public static int UpdatePackage(PackageOptions opts)
        {
            PackageState state = PackageState.Failed;
            if (opts.Latest)
                state = PackageManager.UpdateLatest(opts.PackageId, out Package package);
            else if (opts.PackageVersion != null)
                state = PackageManager.UpdatePackage(opts.PackageId, opts.PackageVersion, out Package package);
            // If PackageVersion is null we just install the latest version for the sdk
            else
                state = PackageManager.UpdateBySdkVersion(opts.PackageId, out Package package);

            if (state == PackageState.Installed)
            {
                Console.WriteLine($"{opts.PackageId} was updated to version {opts.PackageVersion}");
                return 0;
            }
            else if (state == PackageState.NotInstalled)
            {
                Console.Error.WriteLine($"{opts.PackageId} is not installed");
            }
            else if (state == PackageState.NotExisting)
            {
                Console.Error.WriteLine($"Couldn't find any matching package version for your options");
            }
            else if (state == PackageState.UpToDate)
            {
                Console.WriteLine($"{opts.PackageId} is up to date");
            }
            else // PackageState.Failed
            {
                Console.Error.WriteLine(Output.Red($"Something went wrong"));
            }

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

                // i have no idea how i should do this
            }
            // If it is not defined it just updates all packages to the latest version of the sdk
            else
            {
                foreach (Package package in PackageManager.PackageList)
                {
                    PackageState state = PackageManager.UpdateBySdkVersion(package.ID, out Package p);
                    // check if success
                    if (state == PackageState.Installed)
                    {
                        Console.WriteLine($"{opts.PackageId} was updated to version {package.Assets[0].Version}");
                        return 0;
                    }
                    else if (state == PackageState.UpToDate)
                    {
                        Console.WriteLine($"{opts.PackageId} is already up to date");
                        return 0;
                    }
                    else // PackageState.Failed
                    {
                        Console.WriteLine(Output.Red($"Something went wrong"));
                    }
                }
            }

            return 1;
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
