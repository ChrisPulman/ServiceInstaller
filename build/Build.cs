// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tools.DotNet;
using Serilog;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

namespace ServiceInstaller.Building;

/// <summary>
/// Defines the repository build pipeline.
/// </summary>
public sealed class Build : NukeBuild
{
    private static readonly AbsolutePath SolutionFile = RootDirectory / "src" / "ServiceInstaller.slnx";

    /// <summary>
    /// Gets or sets the selected build configuration.
    /// </summary>
    [Parameter("Configuration to build - Default is 'Debug' locally and 'Release' on a build server")]
    public Configuration Configuration { get; set; } =
        IsLocalBuild ? Configuration.Debug : Configuration.Release;

    private static AbsolutePath PackagesDirectory => RootDirectory / "output";

    private Target Print => target => target.Executes(() =>
    {
        Log.Information("Configuration = {Configuration}", Configuration);
        Log.Information(
            "MinVerVersionOverride = {Value}",
            Environment.GetEnvironmentVariable("MinVerVersionOverride") ?? "<auto>");
    });

    private Target Clean => target => target.Before(Restore).Executes(() =>
    {
        if (!IsLocalBuild)
        {
            _ = PackagesDirectory.CreateOrCleanDirectory();
        }
    });

    private Target Restore => target => target
        .DependsOn(Clean)
        .Executes(() => DotNetRestore(settings => settings.SetProjectFile(SolutionFile)));

    private Target Compile => target => target
        .DependsOn(Restore, Print)
        .Executes(() => DotNetBuild(settings => settings
            .SetProjectFile(SolutionFile)
            .SetConfiguration(Configuration)
            .SetNoRestore(true)));

    /// <summary>
    /// Runs the requested Nuke targets.
    /// </summary>
    /// <returns>The process exit code.</returns>
    public static int Main() => Execute<Build>(build => build.Compile);
}
