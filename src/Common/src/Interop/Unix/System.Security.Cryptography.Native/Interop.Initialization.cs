// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Initialization of libcrypto threading support is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the libcrypto or System.Security.Cryptography.Native functions will trigger 
    // initialization of the threading support.

    internal static partial class libcrypto
    {
        static libcrypto()
        {
            CryptoInitializer.Initialize();
        }
    }

    internal static partial class Crypto
    {
        static Crypto()
        {
            CryptoInitializer.Initialize();
        }
    } 

    internal static class CryptoInitializer
    {
        static CryptoInitializer()
        {
            if (EnsureOpenSslInitialized() != 0)
            {
                // Ideally this would be a CryptographicException, but we use
                // OpenSSL in libraries lower than System.Security.Cryptography.
                // It's not a big deal, though: this will already be wrapped in a
                // TypeLoadException, and this failing means something is very
                // wrong with the system's configuration and any code using
                // these libraries will be unable to operate correctly.
                throw new InvalidOperationException();
            }

            // Load the SHA-2 hash algorithms, and anything else not in the default
            // support set.
            OPENSSL_add_all_algorithms_conf();

            // Ensure that the error message table is loaded.
            ERR_load_crypto_strings();
        }

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors.
        }

        [DllImport(Libraries.CryptoNative)]
        private static extern int EnsureOpenSslInitialized();

        [DllImport(Libraries.LibCrypto)]
        private static extern void ERR_load_crypto_strings();

        [DllImport(Libraries.LibCrypto)]
        private static extern void OPENSSL_add_all_algorithms_conf();
    }
}
