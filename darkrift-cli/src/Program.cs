using System.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using CommandLine;
using Crayon;
using System.Collections.Generic;

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
            PMF.PMF.OnPackageMessage += PMF_OnPackageMessage;

            Directory.CreateDirectory(Config.USER_DR_DIR);

            Profile.Load();
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

        private static void PMF_OnPackageMessage(string message)
        {
            Console.WriteLine(message);
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
            string version = opts.Version ?? VersionManager.GetLatestDarkRiftVersion();
            var tier = opts.Pro ? ServerTier.Pro : ServerTier.Free;

            // Executes the command to download the version if it doesn't exist
            if (!VersionManager.IsVersionInstalled(version, tier, opts.Platform))
                VersionManager.DownloadVersion(version, tier, opts.Platform);

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
            // Its simply not a project
            if (!Project.Loaded)
            {
                Console.WriteLine(Output.Red($"The current folder is not a project"));
                return 1;
            }

            // Executes the command to download the version if it doesn't exist
            if (!VersionManager.IsVersionInstalled(Project.Runtime.Version, Project.Runtime.Tier, Project.Runtime.Platform))
                VersionManager.DownloadVersion(Project.Runtime.Version, Project.Runtime.Tier, Project.Runtime.Platform);

            string path = VersionManager.GetInstallationPath(Project.Runtime.Version, Project.Runtime.Tier, Project.Runtime.Platform);

            // Calculate the executable file to run
            string fullPath;
            IEnumerable<string> args;
            if (Project.Runtime.Platform == ServerPlatform.Framework)
            {
                fullPath = Path.Combine(path, Config.DR_EXECUTABLE_NAME);
                args = opts.Values;
            }
            else
            {
                fullPath = "dotnet";
                args = opts.Values.Prepend(Path.Combine(path, "Lib", Config.DR_DLL_NAME));
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
            if (opts.Version == "latest")
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
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to install. To download latest version use \"latest\" as the version"));
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
                    Console.WriteLine(Output.Green($"Documentation for DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use darkrift pull docs {opts.Version} -f"));
                    success = true;
                }
                else
                    success = VersionManager.DownloadDocumentation(opts.Version);
            }
            else
            {
                bool versionInstalled = VersionManager.IsVersionInstalled(opts.Version, actualTier, opts.Platform);
                if (versionInstalled && !opts.Force)
                {
                    Console.WriteLine(Output.Green($"DarkRift {opts.Version} - {actualTier} (.NET {opts.Platform}) already installed! To force a reinstall use darkrift pull {opts.Version} -f"));
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
            if (opts.Version == "latest")
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
                    Console.Error.WriteLine(Output.Red($"Couldn't find a version to download documentation. To download latest version 'latest'"));
                    return 2;
                }
            }

            if (opts.Local)
            {
                if (VersionManager.IsDocumentationInstalled(opts.Version))
                    BrowserUtil.OpenTo("file://" + VersionManager.GetDocumentationPath(opts.Version) + "/index.html");
                else
                    Console.Error.WriteLine(Output.Red($"Documentation not installed, consider running \"darkrift pull {opts.Version} --docs\""));
            }
            else if (opts.Version != null)
            {
                BrowserUtil.OpenTo($"https://darkriftnetworking.com/DarkRift2/Docs/{opts.Version}");
            }

            return 0;
        }

        private static int Packages(PackageOptions opts)
        {
            if (!string.IsNullOrEmpty(opts.PackageId) && opts.PackageId.Contains('@'))
            {
                string[] arr = opts.PackageId.Split('@');
                opts.PackageId = arr[0];
                opts.PackageVersion = arr[1];
            }
            
            // its simply not a project
            if (!Project.Loaded)
            {
                Console.Error.WriteLine(Output.Red("The current folder is not a project"));
                return 1;
            }

            // This needs to be properly set
            PMF.Config.CurrentSdkVersion = "0.0.1";
            PMF.Config.ManifestFileName = "manifest.json";
            PMF.Config.PackageInstallationFolder = ".packages";
            PMF.Config.RepositoryEndpoint = "http://localhost:3000/package";
            PMF.Config.IsDebugging = true;

            // this will always be necessary unless the option is update
            if (string.IsNullOrEmpty(opts.PackageId) && opts.PackageOperation != PackageOperation.Update)
            {
                Console.Error.WriteLine($"No package was specified");
                return 1;
            }

            PMF.PMF.Start();

            // This is to make sure PMF.PMF.Stop() is called at the end of the method
            // This value is returned ater this method
            int returnValue = 0;

            if (opts.PackageOperation == PackageOperation.Install)
            {
                returnValue = DarkRiftPackageManager.Install(opts);
            }
            else if (opts.PackageOperation == PackageOperation.Uninstall)
            {
                returnValue = DarkRiftPackageManager.Uninstall(opts);
            }
            else if (opts.PackageOperation == PackageOperation.Update)
            {
                returnValue = DarkRiftPackageManager.Update(opts);
            }

            PMF.PMF.Stop();
            return returnValue;
        }
    }
}
