// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Defines command-line option names.</summary>
internal static class CommandOptions
{
    /// <summary>The service account option.</summary>
    internal const string Account = "account";

    /// <summary>The executable path option.</summary>
    internal const string Binary = "binary";

    /// <summary>The dependency list option.</summary>
    internal const string Dependencies = "dependencies";

    /// <summary>The service description option.</summary>
    internal const string Description = "description";

    /// <summary>The display name option.</summary>
    internal const string DisplayName = "display-name";

    /// <summary>The JSON output flag.</summary>
    internal const string Json = "json";

    /// <summary>The remote computer option.</summary>
    internal const string Machine = "machine";

    /// <summary>The stable service name option.</summary>
    internal const string Name = "name";

    /// <summary>The standard-input password flag.</summary>
    internal const string PasswordStdin = "password-stdin";

    /// <summary>The start-after-create flag.</summary>
    internal const string Start = "start";

    /// <summary>The service start mode option.</summary>
    internal const string StartMode = "start-mode";

    /// <summary>The operation timeout option.</summary>
    internal const string Timeout = "timeout";
}
