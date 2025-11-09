using System;

namespace Xpand.Extensions.ExceptionExtensions{
    public static partial class ExceptionExtensions{
        public class CircularDependencyException(string message) : Exception(message);
    }
}