// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Provides the terminal process entry point.</summary>
internal static class Program
{
    /// <summary>Runs the terminal command.</summary>
    /// <param name="args">Command-line arguments.</param>
    /// <returns>The process exit code.</returns>
    internal static int Main(string[] args) =>
        new ServiceCommandLine().Run(args, Console.In, Console.Out, Console.Error);
}
