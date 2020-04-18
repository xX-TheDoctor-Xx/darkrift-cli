using System.Text.RegularExpressions;
using System.Reflection.Metadata;
using System.Reflection;
using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using CommandLine;
using Crayon;
using System.Collections.Generic;
using PMF.Managers;
using PMF;

namespace DarkRift.Cli
{
    internal class Program
    {
        /// <summary>
        /// The location of the template archives.
        /// <summary>
        private static readonly string TEMPLATES_PATH = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "templates");

        public static int Main(string[] args)
        {
            Project.Load();
            return new Parser(SetupParser).ParseArguments<NewOptions, RunOptions, PullOptions, DocsOptions, PackageOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (PullOptions opts) => Pull(opts),
                    (DocsOptions opts) => Docs(opts),
                    (PackageOptions opts) => Packages(opts),
                    _ => 1);
        }

        /// <summary>
        /// Setup the parser for our application.
        /// </summary>
        /// <param name="settings">The settings for the parser.</param>
        private static void SetupParser(ParserSettings settings)
        {
            // Default
            settings.HelpWriter = Console.Error;

            settings.CaseInsensitiveEnumValues = true;

            // Added for 'run' command
            settings.EnableDashDash = true;
        }

        private static int New(NewOptions opts)
        {
            Version version = opts.Version ?? VersionManager.GetLatestDarkRiftVersion();

            // Executes the command to download the version if it doesn't exist
            if (Pull(new PullOptions()
            {
                Version = version,
                Pro = opts.Pro,
                Platform = opts.Platform,
                Force = false
            }) != 0)
            {
                Console.Error.WriteLine(Output.Red("An error occured while trying to download the version required, exiting New"));
                return 2;
            }

            string targetDirectory = opts.TargetDirectory ?? Environment.CurrentDirectory;
            string templatePath = Path.Combine(TEMPLATES_PATH, opts.Type + ".zip");

            Directory.CreateDirectory(targetDirectory);

            if (Directory.GetFiles(targetDirectory).Length > 0 && !opts.Force)
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, the directory is not empty. Use -f to force creation."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + Parser.Default.FormatCommandLine(new NewOptions { Type = opts.Type, TargetDirectory = opts.TargetDirectory, Force = true }));
                return 1;
            }

            if (!File.Exists(templatePath))
            {
                Console.Error.WriteLine(Output.Red("Cannot create from template, no template with that name exists."));
                return 1;
            }

            Console.WriteLine($"Creating new {opts.Type} '{Path.GetFileName(targetDirectory)}' from template...");

            ZipFile.ExtractToDirectory(templatePath, targetDirectory, true);

            Console.WriteLine($"Cleaning up extracted artifacts...");

            foreach (string path in Directory.GetFiles(targetDirectory, "*.*", SearchOption.AllDirectories))
                FileTemplater.TemplateFileAndPath(path, Path.GetFileName(targetDirectory), version, opts.Pro ? ServerTier.Pro : ServerTier.Free, opts.Platform);

            Project.Runtime.Platform = opts.Platform;
            Project.Runtime.Tier = opts.Pro ? ServerTier.Pro : ServerTier.Free;
            Project.Runtime.Version = version;

            Project.Save(targetDirectory);

            Console.WriteLine(Output.Green($"Created '{Path.GetFileName(targetDirectory)}'"));

            return 0;
        }

        private static int Run(RunOptions opts)
        {
            // its simply not a project
            if (Project.Loaded)
            {
                Console.WriteLine(Output.Red($"The current folder is not a project"));
                return 1;
            }

            // Executes the command to download the version if it doesn't exist
            if (Pull(new PullOptions()
            {
                Version = Project.Runtime.Version,
                Pro = Project.Runtime.Tier == ServerTier.Pro,
                Platform = Project.Runtime.Platform,
                Force = false
            }) != 0)
            {
                Console.Error.WriteLine(Output.Red("An error occured while trying to download the version required, exiting New"));
                return 2;
            }

            string path = VersionManager.GetInstallationPath(Project.Runtime.Version, Project.Runtime.Tier, Project.Runtime.Platform);

            // Calculate the executable file to run
            string fullPath;
            IEnumerable<string> args;
            if (Project.Runtime.Platform == ServerPlatform.Framework)
            {
                fullPath = Path.Combine(path, "DarkRift.Server.Console.exe");
                args = opts.Values;
            }
            else
            {
                fullPath = "dotnet";
                args = opts.Values.Prepend(Path.Combine(path, "Lib", "DarkRift.Server.Console.dll"));
            }

            using (Process process = new Process())
            {
                process.StartInfo = new ProcessStartInfo(fullPath, string.Join(" ", args));
                process.Start();

                process.WaitForExit();

                return process.ExitCode;
            }
        }

        private static int Pull(PullOptions opts)
        {
            // If --list was specified, list installed versions and tell if documentation for that version is available locally
            if (opts.List)
            {
                VersionManager.ListInstalledVersions();
                return 0;
            }

            // if version provided is "latest", it is being replaced with currently most recent one
            if (opts.Latest)
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // if version info was omitted, overwrite any parameters with current project settings
                if (Project.Loaded)
                {
                    opts.Version = Project.Runtime.Version;
                    opts.Platform = Project.Runtime.Platform;
                    opts.Pro = Project.Runtime.Tier == ServerTier.Pro;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to install. To download latest version use option --latest"));
                    return 2;
                }
            }

            ServerTier actualTier = opts.Pro ? ServerTier.Pro : ServerTier.Free;

            // If --docs was specified, download documentation instead
            bool success = false;
            if (opts.Docs)
            {
                bool docsInstalled = VersionManager.IsDocumentationInstalled(opts.Version);

                if (docsInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"Documentation for DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use the option -f or --force"));
                    success = true;
                }
                else
                    success = VersionManager.DownloadDocumentation(opts.Version);
            }
            else if (opts.Version != null)
            {
                bool versionInstalled = VersionManager.IsVersionInstalled(opts.Version, actualTier, opts.Platform);
                if (versionInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use the option -f or --force"));
                    success = true;
                }
                else
                    success = VersionManager.DownloadVersion(opts.Version, actualTier, opts.Platform);
            }

            if (!success)
            {
                Console.Error.WriteLine(Output.Red("Invalid command"));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + Parser.Default.FormatCommandLine(new PullOptions()));
                return 1;
            }

            return 0;
        }

        private static int Docs(DocsOptions opts)
        {
            // If "latest" option is provided we use the most recent version
            if (opts.Latest)
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // If version info was omitted, overwrite version with current project settings
                if (Project.Loaded)
                {
                    opts.Version = Project.Runtime.Version;
                }
                else
                {
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to download documentation. To download latest version use option --latest"));
                    return 2;
                }
            }

            if (opts.Local)
            {
                if (VersionManager.IsDocumentationInstalled(opts.Version))
                    BrowserUtil.OpenTo("file://" + VersionManager.GetDocumentationPath(opts.Version) + "/index.html");
                else
                    Console.Error.WriteLine(Output.Red($"Documentation not installed, consider running \"darkrift pull --docs --version {opts.Version}\""));
            }
            else if (opts.Version != null)
            {
                BrowserUtil.OpenTo($"https://darkriftnetworking.com/DarkRift2/Docs/{opts.Version}");
            }

            return 0;
        }

        #region Package Management

        private static int Install(PackageOptions opts)
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
                Console.WriteLine(Output.Green($"{opts.PackageId} version {opts.PackageVersion} was installed successfully"));
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
                Console.Error.WriteLine($"Couldn't find package version with SDK version {Config.CurrentSdkVersion}");
            }
            else // PackageState.Failed
            {
                Console.Error.WriteLine(Output.Red($"Something went wrong"));
            }

            return 1;
        }

        private static int Uninstall(PackageOptions opts)
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

        private static int UpdatePackage(PackageOptions opts)
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
            else if (state == PackageState.Cancelled)
            {
                return 0;
            }
            else // PackageState.Failed
            {
                Console.Error.WriteLine(Output.Red($"Something went wrong"));
            }

            return 1;
        }

        private static int UpdatePackagesOrCli(PackageOptions opts)
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
                foreach (Package package in LocalPackageManager.PackageList)
                {
                    PackageState state = PackageManager.UpdateBySdkVersion(package.ID, out Package p, true);
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

        private static int Update(PackageOptions opts)
        {
            // if we have a package id we update that package
            // if we dont we just update them all or the cli if option is specified
            return !string.IsNullOrEmpty(opts.PackageId) ? UpdatePackage(opts) : UpdatePackagesOrCli(opts);
        }

        private static int Packages(PackageOptions opts)
        {
            if (!string.IsNullOrEmpty(opts.PackageId) && opts.PackageId.Contains('@'))
            {
                string[] arr = opts.PackageId.Split('@');
                opts.PackageId = arr[0];

                try
                {
                    opts.PackageVersion = new Version(arr[1]);
                }
                catch
                {
                    Console.Error.WriteLine(Output.Red("Invalid version format"));
                    return 1;
                }
            }
            
            // its simply not a project
            if (!Project.Loaded)
            {
                Console.Error.WriteLine(Output.Red("The current folder is not a project"));
                return 1;
            }

            Config.CurrentSdkVersion = new Version("0.0.1");
            Config.ManifestFileName = "manifest.json";
            Config.PackageInstallationFolder = ".packages";
            Config.RepositoryEndpoint = "http://localhost:3000/package";
            Config.IsDebugging = true;

            // this will always be necessary unless the option is update
            if (string.IsNullOrEmpty(opts.PackageId) && opts.PackageOperation != PackageOperation.Update)
            {
                Console.Error.WriteLine($"No package was specified, use -p or --package");
                return 1;
            }

            LocalPackageManager.Start();

            // This is to make sure LocalPackageManager.Stop() is called at the end of the method
            // This value is returned ater this method
            int returnValue = 0;

            if (opts.PackageOperation == PackageOperation.Install)
            {
                returnValue = Install(opts);
            }
            else if (opts.PackageOperation == PackageOperation.Uninstall)
            {
                returnValue = Uninstall(opts);
            }
            else if (opts.PackageOperation == PackageOperation.Update)
            {
                returnValue = Update(opts);
            }

            LocalPackageManager.Stop();
            return returnValue;
        }

        #endregion
    }
}
