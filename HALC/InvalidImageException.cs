using System;

namespace HALC
{
    public class InvalidImageException : Exception
    {
        public InvalidImageException(string errorMessage)
            : base(errorMessage)
        {
        }
    }
}