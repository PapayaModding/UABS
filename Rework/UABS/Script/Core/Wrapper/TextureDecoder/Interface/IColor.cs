using System;

namespace UABS.Wrapper
{
    public interface IColor : IEquatable<IColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }
    }
}