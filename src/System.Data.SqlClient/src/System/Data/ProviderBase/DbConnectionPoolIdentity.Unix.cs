// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


//------------------------------------------------------------------------------

using System.Net;
using System.Net.Security;

namespace System.Data.ProviderBase
{
    partial class DbConnectionPoolIdentity
    {
        static private DbConnectionPoolIdentity s_lastIdentity = null;

        static internal DbConnectionPoolIdentity GetCurrent()
        {
            DbConnectionPoolIdentity current;
            bool isRestricted = false; //TODO: Find how to determine this.
            bool isNetwork = false;//TODO: Find how to determine this.
            string sidString = CredentialCache.DefaultNetworkCredentials.UserName;
            //TODO: Remove this console write
            //TODO: why this is empty
            Console.WriteLine("**** DbConnectionPoolIdentity.UNIX - CredentialCache.DefaultNetworkCredentials username:{0} domain:{1} password:{2} ", CredentialCache.DefaultNetworkCredentials.UserName, CredentialCache.DefaultNetworkCredentials.Domain, CredentialCache.DefaultNetworkCredentials.Password);
            var lastIdentity = s_lastIdentity;
            if ((lastIdentity != null) && (lastIdentity._sidString == sidString) && (lastIdentity._isRestricted == isRestricted) && (lastIdentity._isNetwork == isNetwork))
            {
                current = lastIdentity;
            }
            else
            {
                current = new DbConnectionPoolIdentity(sidString, isRestricted, isNetwork);
            }
        
            return current;
        }
}
}

