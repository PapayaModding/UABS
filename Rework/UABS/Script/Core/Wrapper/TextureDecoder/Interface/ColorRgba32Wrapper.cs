using System;
using BCnEncoder.Shared;

namespace UABS.Wrapper
{
    public class ColorRgba32Wrapper : IColor, IEquatable<IColor>
    {
        private readonly ColorRgba32 _color;

        public ColorRgba32Wrapper(ColorRgba32 color)
        {
            _color = color;
        }

        public byte R => _color.r;
        public byte G => _color.g;
        public byte B => _color.b;
        public byte A => _color.a;

        public bool Equals(IColor? other)
        {
            if (other == null) return false;
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object? obj)
        {
            return obj is IColor color && Equals(color);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }
    }
}