﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal partial class Interop
{
    internal partial class mincore
    {
        [DllImport(Libraries.ProcessThread_L1_1, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeProcessHandle OpenProcess(int access, bool inherit, int processId);
    }
}
