using System;

namespace OFXSharp
{
    public class OFXParseException : OFXException
    {
        public OFXParseException()
        {
        }

        public OFXParseException(string message) : base(message)
        {
        }

        public OFXParseException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}