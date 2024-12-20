using System;

namespace Garage61Data.Exceptions
{
    public class ApiClientException : Exception
    {
        public ApiClientException(string message) : base(message)
        {
        }
    }
}