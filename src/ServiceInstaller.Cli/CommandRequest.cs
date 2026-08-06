// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller.Cli;

/// <summary>Represents a parsed terminal request.</summary>
internal sealed class CommandRequest
{
    /// <summary>The number of characters in a long-option prefix.</summary>
    private const int LongOptionPrefixLength = 2;

    /// <summary>Known terminal command names.</summary>
    private static readonly Dictionary<string, ServiceCommandVerb> KnownVerbs = new(StringComparer.OrdinalIgnoreCase)
    {
        ["configure"] = ServiceCommandVerb.Configure,
        ["continue"] = ServiceCommandVerb.Continue,
        ["create"] = ServiceCommandVerb.Create,
        ["delete"] = ServiceCommandVerb.Delete,
        ["exists"] = ServiceCommandVerb.Exists,
        ["install"] = ServiceCommandVerb.Install,
        ["pause"] = ServiceCommandVerb.Pause,
        ["query"] = ServiceCommandVerb.Query,
        ["resume"] = ServiceCommandVerb.Resume,
        ["start"] = ServiceCommandVerb.Start,
        ["status"] = ServiceCommandVerb.Status,
        ["stop"] = ServiceCommandVerb.Stop,
        ["uninstall"] = ServiceCommandVerb.Uninstall,
    };

    /// <summary>The supplied flags.</summary>
    private readonly HashSet<string> _flags;

    /// <summary>The supplied valued options.</summary>
    private readonly Dictionary<string, string> _options;

    /// <summary>Initializes a new instance of the <see cref="CommandRequest"/> class.</summary>
    /// <param name="verb">The command verb.</param>
    /// <param name="options">The valued options.</param>
    /// <param name="flags">The flags.</param>
    /// <param name="serviceArguments">Additional service arguments.</param>
    private CommandRequest(
        string verb,
        Dictionary<string, string> options,
        HashSet<string> flags,
        string[] serviceArguments)
    {
        Verb = verb;
        Command = ParseVerb(verb);
        _options = options;
        _flags = flags;
        ServiceArguments = serviceArguments;
    }

    /// <summary>Gets additional arguments passed to the service process.</summary>
    internal string[] ServiceArguments { get; }

    /// <summary>Gets the parsed command.</summary>
    internal ServiceCommandVerb Command { get; }

    /// <summary>Gets the requested command.</summary>
    internal string Verb { get; }

    /// <summary>Parses terminal arguments.</summary>
    /// <param name="args">The terminal arguments.</param>
    /// <returns>The parsed request.</returns>
    internal static CommandRequest Parse(IReadOnlyList<string> args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var flags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var serviceArguments = new List<string>();
        for (var index = 1; index < args.Count; index++)
        {
            var token = args[index];
            if (token == "--")
            {
                AddServiceArguments(args, serviceArguments, index + 1);
                break;
            }

            var name = GetOptionName(token);
            if (index + 1 >= args.Count || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                AddFlag(name, options, flags);
                continue;
            }

            index++;
            AddOption(name, args[index], options, flags);
        }

        return new(args[0].ToLowerInvariant(), options, flags, serviceArguments.ToArray());
    }

    /// <summary>Validates that only the supplied option names were used.</summary>
    /// <param name="names">Allowed option names.</param>
    internal void EnsureAllowed(params string[] names)
    {
        var allowed = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);
        foreach (var name in _options.Keys)
        {
            EnsureAllowed(name, allowed);
        }

        foreach (var name in _flags)
        {
            EnsureAllowed(name, allowed);
        }
    }

    /// <summary>Gets an option value.</summary>
    /// <param name="name">The option name.</param>
    /// <returns>The value, or <see langword="null"/> when absent.</returns>
    internal string? Get(string name) => _options.TryGetValue(name, out var value) ? value : null;

    /// <summary>Gets a required option value.</summary>
    /// <param name="name">The option name.</param>
    /// <returns>The option value.</returns>
    internal string GetRequired(string name) => Get(name)
        ?? throw new ArgumentException($"Missing required option '--{name}'.");

    /// <summary>Determines whether an option has a value.</summary>
    /// <param name="name">The option name.</param>
    /// <returns><see langword="true"/> when present.</returns>
    internal bool Has(string name) => _options.ContainsKey(name);

    /// <summary>Determines whether a flag is present.</summary>
    /// <param name="name">The flag name.</param>
    /// <returns><see langword="true"/> when present.</returns>
    internal bool HasFlag(string name) => _flags.Contains(name);

    /// <summary>Adds the remaining service arguments.</summary>
    /// <param name="args">All command arguments.</param>
    /// <param name="serviceArguments">The destination collection.</param>
    /// <param name="startIndex">The first service argument index.</param>
    private static void AddServiceArguments(
        IReadOnlyList<string> args,
        List<string> serviceArguments,
        int startIndex)
    {
        for (var index = startIndex; index < args.Count; index++)
        {
            serviceArguments.Add(args[index]);
        }
    }

    /// <summary>Adds a flag after checking for duplicates.</summary>
    /// <param name="name">The flag name.</param>
    /// <param name="options">Existing valued options.</param>
    /// <param name="flags">Existing flags.</param>
    private static void AddFlag(
        string name,
        Dictionary<string, string> options,
        HashSet<string> flags)
    {
        if (options.ContainsKey(name) || !flags.Add(name))
        {
            throw DuplicateOption(name);
        }
    }

    /// <summary>Adds an option after checking for duplicates.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="value">The option value.</param>
    /// <param name="options">Existing valued options.</param>
    /// <param name="flags">Existing flags.</param>
    private static void AddOption(
        string name,
        string value,
        Dictionary<string, string> options,
        HashSet<string> flags)
    {
        if (flags.Contains(name) || !options.TryAdd(name, value))
        {
            throw DuplicateOption(name);
        }
    }

    /// <summary>Creates the duplicate-option exception.</summary>
    /// <param name="name">The duplicated option name.</param>
    /// <returns>The argument exception.</returns>
    private static ArgumentException DuplicateOption(string name) =>
        new($"Option '--{name}' was specified more than once.");

    /// <summary>Extracts a long option name.</summary>
    /// <param name="token">The option token.</param>
    /// <returns>The option name.</returns>
    private static string GetOptionName(string token)
    {
        if (!token.StartsWith("--", StringComparison.Ordinal))
        {
            throw new ArgumentException($"Unexpected argument '{token}'. Options must start with --.");
        }

        return token[LongOptionPrefixLength..];
    }

    /// <summary>Parses a terminal command name.</summary>
    /// <param name="verb">The command name.</param>
    /// <returns>The parsed command.</returns>
    private static ServiceCommandVerb ParseVerb(string verb) =>
        KnownVerbs.TryGetValue(verb, out var command) ? command : ServiceCommandVerb.Unknown;

    /// <summary>Checks one option name against the allowed set.</summary>
    /// <param name="name">The option name.</param>
    /// <param name="allowed">The allowed option names.</param>
    private void EnsureAllowed(string name, HashSet<string> allowed)
    {
        if (!allowed.Contains(name))
        {
            throw new ArgumentException($"Option '--{name}' is not valid for '{Verb}'.");
        }
    }
}
