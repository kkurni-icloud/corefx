// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#include "pal_rsa.h"
#include "pal_utilities.h"

extern "C" RSA* RsaCreate()
{
    return RSA_new();
}

extern "C" int32_t RsaUpRef(RSA* rsa)
{
    return RSA_up_ref(rsa);
}

extern "C" void RsaDestroy(RSA* rsa)
{
    if (rsa != nullptr)
    {
        RSA_free(rsa);
    }
}

extern "C" RSA* DecodeRsaPublicKey(const uint8_t* buf, int32_t len)
{
    if (!buf || !len)
    {
        return nullptr;
    }

    return d2i_RSAPublicKey(nullptr, &buf, len);
}

extern "C" int32_t RsaPublicEncrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, int32_t useOaepPadding)
{
    int padding = useOaepPadding ? RSA_PKCS1_OAEP_PADDING : RSA_PKCS1_PADDING;
    return RSA_public_encrypt(flen, from, to, rsa, padding);
}

extern "C" int32_t RsaPrivateDecrypt(int32_t flen, const uint8_t* from, uint8_t* to, RSA* rsa, int32_t useOaepPadding)
{
    int padding = useOaepPadding ? RSA_PKCS1_OAEP_PADDING : RSA_PKCS1_PADDING;
    return RSA_private_decrypt(flen, from, to, rsa, padding);
}

extern "C" int32_t RsaSize(RSA* rsa)
{
    return RSA_size(rsa);
}

extern "C" int32_t RsaGenerateKeyEx(RSA* rsa, int32_t bits, BIGNUM* e)
{
    return RSA_generate_key_ex(rsa, bits, e, nullptr);
}

extern "C" int32_t RsaSign(int32_t type, const uint8_t* m, int32_t m_len, uint8_t* sigret, int32_t* siglen, RSA* rsa)
{
    if (!siglen)
    {
        return 0;
    }

    unsigned int unsignedSigLen = 0;
    int32_t ret = RSA_sign(type, m, UnsignedCast(m_len), sigret, &unsignedSigLen, rsa);
    assert(unsignedSigLen <= INT32_MAX);
    *siglen = static_cast<int32_t>(unsignedSigLen);
    return ret;
}

extern "C" int32_t RsaVerify(int32_t type, const uint8_t* m, int32_t m_len, uint8_t* sigbuf, int32_t siglen, RSA* rsa)
{
    return RSA_verify(type, m, UnsignedCast(m_len), sigbuf, UnsignedCast(siglen), rsa);
}
