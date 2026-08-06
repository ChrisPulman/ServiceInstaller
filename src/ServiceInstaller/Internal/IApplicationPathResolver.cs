// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Internal;

/// <summary>Resolves the current application executable path for legacy installation.</summary>
internal interface IApplicationPathResolver
{
    /// <summary>Resolves the current application executable path.</summary>
    /// <returns>The resolved executable path.</returns>
    string Resolve();
}
