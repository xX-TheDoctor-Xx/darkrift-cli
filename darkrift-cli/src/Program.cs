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
using DarkRift.PMF.Managers;
using DarkRift.PMF;

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
            return new Parser(SetupParser).ParseArguments<NewOptions, RunOptions, GetOptions, PullOptions, DocsOptions, PackageOptions>(args)
                .MapResult(
                    (NewOptions opts) => New(opts),
                    (RunOptions opts) => Run(opts),
                    (GetOptions opts) => Get(opts),
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

        private static int Get(GetOptions opts)
        {
            if (!Uri.TryCreate(opts.Url, UriKind.RelativeOrAbsolute, out Uri uri))
            {
                Console.Error.WriteLine(Output.Red("Invalid URL passed."));
                Console.Error.WriteLine("\t" + Environment.GetCommandLineArgs()[0] + " " + CommandLine.Parser.Default.FormatCommandLine(new GetOptions { Url = "https://your-download-url/file.zip" }));
                return 1;
            }

            string stagingDirectory = Path.Combine(".", ".darkrift", "temp");
            string stagingPath = Path.Combine(stagingDirectory, "Download.zip");
            Directory.CreateDirectory(stagingDirectory);

            Console.WriteLine($"Downloading package...");

            using (WebClient myWebClient = new WebClient())
            {
                myWebClient.DownloadFile(uri, stagingPath);
            }

            Console.WriteLine($"Extracting package...");

            // TODO find a better place for this
            string targetDirectory = Path.Combine(".", "plugins");
            Directory.CreateDirectory(targetDirectory);

            ZipFile.ExtractToDirectory(stagingPath, targetDirectory, true);

            Console.WriteLine(Output.Green($"Sucessfully downloaded package into plugins directory."));

            return 0;
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
            // If version provided is "latest", it is being replaced with currently most recent one
            if (opts.Latest)
            {
                opts.Version = VersionManager.GetLatestDarkRiftVersion();
            }

            if (opts.Version == null)
            {
                // If version info was omitted, overwrite any parameters with current project settings
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

        private static int Packages(PackageOptions opts)
        {
            // its simply not a project
            if (!Project.Loaded)
            {
                Console.WriteLine(Output.Red($"The current folder is not a project"));
                return 1;
            }

            Config.CurrentSdkVersion = new Version("0.0.1");
            Config.ManifestFileName = "manifest.json";
            Config.PackageInstallationFolder = ".packages";
            Config.RepositoryEndpoint = "http://localhost:3000/package";
            Config.IsDebugging = true;

            // This is to make sure that only one option is selected when the command is executed
            if ((opts.Install && (opts.Uninstall || opts.Update)) ||
                (opts.Uninstall && (opts.Install || opts.Update)) ||
                (opts.Update && (opts.Install || opts.Uninstall)) ||
                (opts.Upgrade && (opts.Install || opts.Uninstall || opts.Update)))
            {
                Console.Error.WriteLine(Output.Red($"More than one option was selected, try \"darkrift help package\""));
                return 1;
            }
            else if (!(opts.Install || opts.Uninstall || opts.Update || opts.Upgrade))
            {
                Console.Error.WriteLine(Output.Red($"No option was selected, try \"darkrift help package\""));
                return 1;
            }

            // this will always be necessary unless the option is update
            if (string.IsNullOrEmpty(opts.PackageId) && !opts.Upgrade)
            {
                Console.Error.WriteLine(Output.Red($"No package was specified, use -p or --package"));
                return 1;
            }

            LocalPackageManager.Start();

            // This is to make sure LocalPackageManager.Stop() is called at the end of the method
            // This value is returned ater this method
            int returnValue = 0;


            if (opts.Install)
            {
                PackageState state;

                // If PackageVersion is null we just install the latest version for the sdk
                if (opts.PackageVersion != null)
                    state = PackageManager.Install(opts.PackageId, opts.PackageVersion, out Package package);
                else
                    state = PackageManager.InstallBySdkVersion(opts.PackageId, out Package package);

                if (state == PackageState.Installed)
                {
                    Console.WriteLine(Output.Green($"{opts.PackageId} version {opts.PackageVersion} was installed successfully"));
                }
                else if (state == PackageState.AlreadyInstalled)
                {
                    Console.WriteLine($"{opts.PackageId} is already install to update use -u");
                }
                else if (state == PackageState.NotExisting)
                {
                    Console.WriteLine($"Package {opts.PackageId} doesn't exist");
                }
                else if (state == PackageState.VersionNotFound)
                {
                    Console.WriteLine($"Package {opts.PackageId} with SDK version {Config.CurrentSdkVersion} doesn't exist");
                }
                else // PackageState.Failed
                {
                    Console.WriteLine(Output.Red($"Something went wrong"));
                }
            }
            else if (opts.Uninstall)
            {
                // if true uninstall success, if false, package was not even installed
                if (PackageManager.Uninstall(opts.PackageId))
                {
                    Console.WriteLine(Output.Green($"{opts.PackageId} was uninstalled succesfully"));
                }
                else
                {
                    Console.WriteLine(Output.Red($"{opts.PackageId} is not installed"));
                }
            }
            else if (opts.Update)
            {
                // If PackageVersion is null we just install the latest version for the sdk
                if (opts.PackageVersion != null)
                {
                    PackageState state = PackageManager.UpdateLatest(opts.PackageId, out Package package);
                    // check if success
                    if (state == PackageState.Installed)
                    {
                        Console.WriteLine($"Package {opts.PackageId} was updated to version {opts.PackageVersion}");
                    }
                    else if (state == PackageState.NotInstalled)
                    {
                        Console.WriteLine($"Package {opts.PackageId} is not install, so it can't be updated");
                    }
                    else if (state == PackageState.NotExisting)
                    {
                        Console.WriteLine($"Package {opts.PackageId} doesn't exist");
                    }
                    else if (state == PackageState.UpToDate)
                    {
                        Console.WriteLine($"Package {opts.PackageId} is up to date");
                    }
                    else if (state == PackageState.Cancelled)
                    {
                    }
                    else // PackageState.Failed
                    {
                        Console.WriteLine(Output.Red($"Something went wrong"));
                    }
                }
                else
                {
                    PackageState state = PackageManager.UpdateBySdkVersion(opts.PackageId, out Package package, false);
                    // check if success
                    if (state == PackageState.Installed)
                    {
                        Console.WriteLine($"Package {opts.PackageId} was updated to version {package.Assets[0].Version}");
                    }
                    else if (state == PackageState.NotExisting)
                    {
                        Console.WriteLine($"Package {opts.PackageId} doesn't exist");
                    }
                    else if (state == PackageState.VersionNotFound)
                    {
                        Console.WriteLine($"Package {opts.PackageId} with SDK version {Config.CurrentSdkVersion} doesn't exist");
                    }
                    else if (state == PackageState.UpToDate)
                    {
                        Console.WriteLine($"Package {opts.PackageId} is up to date");
                    }
                    else if (state == PackageState.Cancelled)
                    {
                    }
                    else // PackageState.Failed
                    {
                        Console.WriteLine(Output.Red($"Something went wrong"));
                    }
                }
            }
            else if (opts.Upgrade)
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
                            Console.WriteLine($"Package {opts.PackageId} was updated to version {package.Assets[0].Version}");
                        }
                        else if (state == PackageState.UpToDate)
                        {
                            Console.WriteLine($"Package {opts.PackageId} is up to date");
                        }
                        else // PackageState.Failed
                        {
                            Console.WriteLine(Output.Red($"Something went wrong updating {package.ID}"));
                        }
                    }
                }
            }

            LocalPackageManager.Stop();
            return returnValue;
        }
    }
}
