// Copyright (c) Chris Pulman. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;
using Nuke.Common.Tooling;

namespace ServiceInstaller.Building;

[TypeConverter(typeof(TypeConverter<Configuration>))]
public sealed class Configuration : Enumeration
{
    public static readonly Configuration Debug = new() { Value = nameof(Debug) };

    public static readonly Configuration Release = new() { Value = nameof(Release) };

    public static implicit operator string?(Configuration? configuration) => configuration?.Value;

    public override string ToString() => Value;
}
