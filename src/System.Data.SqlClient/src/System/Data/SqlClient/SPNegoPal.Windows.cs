using System.Net;
using System.Security.Authentication.ExtendedProtection;

namespace System.Data.SqlClient
{
    internal class SPNegoPal
    {
        private NTAuthentication _context;

        public SPNegoPal(string spn)
        {
            bool isServer = false;
            var package = NegotiationInfoClass.Negotiate;
            var credential = (NetworkCredential)CredentialCache.DefaultCredentials;
            var servicePrincipalName = spn;

            Console.WriteLine("**** CREDENTIAL = {0} SPN:{1}", credential.UserName, servicePrincipalName);
            ChannelBinding channelBinding = null;
            var flags = Interop.SspiCli.ContextFlags.Connection;

            _context = new NTAuthentication(isServer, package, credential, servicePrincipalName, flags, channelBinding);
        }
        
        public byte[] GetOutgoingBlob(byte[] incomingBlob)
        {
            Interop.SecurityStatus statusCode;
            byte[] message = _context.GetOutgoingBlob(incomingBlob, false, out statusCode);

            if (((int)statusCode & unchecked((int)0x80000000)) != 0)
            {
                throw new System.ComponentModel.Win32Exception((int)statusCode);
            }
          
            Console.WriteLine("*** Is Handshake Complete : {0}", HandshakeComplete);  
            return message;
        }

        private bool HandshakeComplete
        {
            get
            {
                return _context.IsCompleted && _context.IsValidContext;
            }
        }
    }
}
