// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Memory_L1_0, EntryPoint = "MapViewOfFile", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeMemoryMappedViewHandle MapViewOfFile(
            SafeMemoryMappedFileHandle hFileMappingObject,
            int dwDesiredAccess,
            int dwFileOffsetHigh,
            int dwFileOffsetLow,
            UIntPtr dwNumberOfBytesToMap);
    }
}
