using System;

namespace Sigma
{

    [Serializable]
    public class SigmaException : Exception
    {
        public SigmaException() { }
        public SigmaException(string message) : base(message) { }
        public SigmaException(string message, Exception inner) : base(message, inner) { }
        protected SigmaException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
