﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.Registry_L1, CharSet = CharSet.Unicode, BestFitMapping = false, EntryPoint = "RegDeleteKeyExW")]
        internal static extern int RegDeleteKeyEx(SafeRegistryHandle hKey, String lpSubKey, int samDesired, int Reserved);
    }
}
