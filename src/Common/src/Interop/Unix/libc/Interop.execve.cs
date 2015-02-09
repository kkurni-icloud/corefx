// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class libc
    {
        internal static unsafe int execve(string filename, string[] argv, string[] envp)
        {
            byte** argvPtr = null, envpPtr = null;
            try
            {
                AllocNullTerminatedArray(argv, ref argvPtr);
                AllocNullTerminatedArray(envp, ref envpPtr);
                return execve(filename, argvPtr, envpPtr);
            }
            finally
            {
                // Should only execute if execve fails
                FreeArray(envpPtr, envp.Length);
                FreeArray(argvPtr, argv.Length);
            }
        }

        [DllImport(Libraries.Libc, SetLastError = true)]
        private static extern unsafe int execve(string filename, byte** argv, byte** envp);

        private static unsafe void AllocNullTerminatedArray(string[] arr, ref byte** arrPtr)
        {
            int arrLength = arr.Length + 1; // +1 is for null termination

            // Allocate the unmanaged array to hold each string pointer.
            // It needs to have an extra element to null terminate the array.
            arrPtr = (byte**)Marshal.AllocHGlobal(sizeof(IntPtr) * arrLength);
            Debug.Assert(arrPtr != null);

            // Zero the memory so that if any of the individual string allocations fails,
            // we can loop through the array to free any that succeeded.
            // The last element will remain null.
            for (int i = 0; i < arrLength; i++)
            {
                arrPtr[i] = null;
            }

            // Now copy each string to unmanaged memory referenced from the array
            for (int i = 0; i < arr.Length; i++)
            {
                arrPtr[i] = (byte*)Marshal.StringToHGlobalUni(arr[i]);
                Debug.Assert(arrPtr[i] != null);
            }
        }

        private static unsafe void FreeArray(byte** arr, int length)
        {
            if (arr != null)
            {
                // Free each element of the array
                for (int i = 0; i < length; i++)
                {
                    if (arr[i] != null)
                    {
                        Marshal.FreeHGlobal((IntPtr)arr[i]);
                        arr[i] = null;
                    }
                }

                // And then the array itself
                Marshal.FreeHGlobal((IntPtr)arr);
            }
        }
    }
}
