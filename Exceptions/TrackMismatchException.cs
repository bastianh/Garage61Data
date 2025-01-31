using System;

namespace Garage61Data.Exceptions
{
    public class TrackMismatchException : Exception
    {
        public TrackMismatchException(string message) : base(message)
        {
        }
    }
}