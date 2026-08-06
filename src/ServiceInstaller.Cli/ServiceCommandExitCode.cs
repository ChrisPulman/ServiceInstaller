// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Defines stable process exit codes returned by the service CLI.</summary>
public enum ServiceCommandExitCode
{
    /// <summary>The command completed successfully.</summary>
    Success = 0,

    /// <summary>The command-line arguments were invalid.</summary>
    InvalidArguments = 2,

    /// <summary>The requested service operation failed.</summary>
    OperationFailed = 3,

    /// <summary>The current platform or access level cannot perform the operation.</summary>
    PlatformOrAccessDenied = 4,
}
