// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Runtime.Serialization
{
    internal enum SerializationMode
    {
        SharedContract,
#if NET_NATIVE
        SharedType
#endif
    }
}
