﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace DarkRift.Cli
{
    [Verb("new", HelpText = "Create a new DarkRift project.")]
    public class NewOptions
    {
        [Option('f', Default = false, HelpText = "Force creation overwriting any files that already exist in the directory.")]
        public bool Force { get; set; }

        [Value(0, HelpText = "The name of the template to unpack.", Required = true)]
        public string Type { get; set; }

        [Value(1, HelpText = "The directory to unpack the template in.")]
        public string TargetDirectory { get; set; }

        [Option("version", HelpText = "Specify the DarkRift version to use.")]
        public Version Version { get; set; }

        [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
        public bool Pro { get; set; }

        [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Specify the .NET platform of the server to use.")]
        public ServerPlatform Platform { get; set; }
    }

    [Verb("run", HelpText = "Run a DarkRift project.")]
    public class RunOptions
    {
        [Value(0)]
        public IEnumerable<string> Values { get; set; }
    }

    [Verb("pull", HelpText = "Pulls the specified version of DarkRift locally.")]
    public class PullOptions
    {
        [Value(0, Required = false, HelpText = "Version of DarkRift to be installed")]
        public Version Version { get; set; }

        [Option('p', "pro", Default = false, HelpText = "Use the pro version.")]
        public bool Pro { get; set; }

        [Option('s', "platform", Default = ServerPlatform.Framework, HelpText = "Use the .NET platform of the server to use.")]
        public ServerPlatform Platform { get; set; }

        [Option('d', "docs", Default = false, HelpText = "Download the documentation for this version instead.")]
        public bool Docs { get; set; }

        [Option('l', "list", Default = false, HelpText = "List installed versions of DarkRift")]
        public bool List { get; set; }

        [Option('f', "force", Default = false, HelpText = "Forces downloading of a DarkRift version or documentation")]
        public bool Force { get; set; }

        [Option("latest", Default = false, HelpText = "Specifies latest version")]
        public bool Latest { get; set; }
    }

    [Verb("docs", HelpText = "Opens the documentation for DarkRift.")]
    public class DocsOptions
    {
        [Value(0, Required = false)]
        public Version Version { get; set; }

        [Option('l', "local", Default = false, HelpText = "Opens a local copy of the documentation.")]
        public bool Local { get; set; }

        [Option("latest", Default = false, HelpText = "Specifies latest version")]
        public bool Latest { get; set; }
    }

    public enum PackageOperation
    {
        Install,
        Update,
        Uninstall,
    }

    [Verb("package", HelpText = "Manages packages in a given project.")]
    public class PackageOptions
    {
        [Value(0, Required = true, HelpText = "Specifies the option you want to execute - Install, Uninstall, Update")]
        public PackageOperation PackageOperation { get; set; }

        [Option('l', "latest", Default = false, HelpText = "Installs or updates a package to the latest version")]
        public bool Latest { get; set; }

        [Option('c', "cli", Default = false, HelpText = "Specifies that the CLI will be updated")]
        public bool UpgradeCli { get; set; }

        [Value(1, Required = false, HelpText = "ID of a package, if you need a specific version use \"@x.x.x\"")]
        public string PackageId { get; set; }

        // This is here for convinience
        public Version PackageVersion { get; set; }
    }
}
