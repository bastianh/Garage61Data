using System;

namespace Garage61Data.Exceptions
{
    public class CarMismatchException : Exception
    {
        public CarMismatchException(string message) : base(message)
        {
        }
    }
}