// Copyright (c) 2019-2026 Chris Pulman and contributors. All rights reserved.
// Chris Pulman and contributors licenses this file to you under the MIT license.
// See the LICENSE file in the project root for full license information.

namespace ServiceInstaller;

/// <summary>Represents a failed Windows Service Control Manager operation.</summary>
public sealed class ServiceOperationException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ServiceOperationException"/> class.</summary>
    public ServiceOperationException()
        : this("A Windows service operation failed.")
    {
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceOperationException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    public ServiceOperationException(string message)
        : base(message)
    {
        Operation = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceOperationException"/> class.</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ServiceOperationException(string message, Exception innerException)
        : base(message, innerException)
    {
        Operation = string.Empty;
    }

    /// <summary>Initializes a new instance of the <see cref="ServiceOperationException"/> class.</summary>
    /// <param name="operation">The operation that failed.</param>
    /// <param name="serviceName">The affected service name.</param>
    /// <param name="nativeErrorCode">The native Win32 error code.</param>
    /// <param name="message">A safe diagnostic message.</param>
    internal ServiceOperationException(string operation, string? serviceName, int nativeErrorCode, string message)
        : base(message)
    {
        Operation = operation;
        ServiceName = serviceName;
        NativeErrorCode = nativeErrorCode;
    }

    /// <summary>Gets the operation that failed.</summary>
    public string Operation { get; }

    /// <summary>Gets the affected service name, when available.</summary>
    public string? ServiceName { get; }

    /// <summary>Gets the native Win32 error code.</summary>
    public int NativeErrorCode { get; }
}
