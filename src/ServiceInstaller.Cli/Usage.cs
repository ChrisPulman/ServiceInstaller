// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Provides terminal help text.</summary>
internal static class Usage
{
    /// <summary>Terminal help text.</summary>
    internal const string Text = """
        ServiceInstaller CLI

        Usage:
          serviceinstaller create --name <name> --display-name <name> --binary <path> [options] [-- service arguments]
          serviceinstaller install --name <name> --display-name <name> --binary <path> [options] [-- service arguments]
          serviceinstaller configure --name <name> [options] [-- replacement service arguments]
          serviceinstaller status --name <name> [--json]
          serviceinstaller exists --name <name>
          serviceinstaller start|stop|pause|continue|resume --name <name> [--timeout <seconds>] [-- start arguments]
          serviceinstaller delete|uninstall --name <name> [--timeout <seconds>]

        Common options:
          --machine <name>           Control a remote Windows computer.
          --timeout <seconds>        Maximum state-transition duration; default 30.
          --description <text>       Set or clear the service description.
          --start-mode <mode>        automatic, manual, or disabled.
          --account <name>           Service account; omit to use LocalSystem when creating.
          --password-stdin           Read the service account password from standard input.
          --dependencies <a,b>       Comma-separated service dependencies.
          --start                    Start immediately after create.

        A terminal with administrator rights is normally required to create, configure, or delete services.
        """;
}
