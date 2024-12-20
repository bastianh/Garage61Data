using System;

namespace Garage61Data.Exceptions
{
    public class OAuth2Exception : Exception
    {
        public OAuth2Exception(string message)
            : base(message)
        {
        }
    }
}