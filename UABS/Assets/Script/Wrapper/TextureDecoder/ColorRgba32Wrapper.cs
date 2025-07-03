using System;
using BCnEncoder.Shared;

namespace UABS.Assets.Script.Wrapper.TextureDecoder
{
    public class ColorRgba32Wrapper : IColor
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

        public bool Equals(IColor other)
        {
            if (other == null) return false;
            return R == other.R && G == other.G && B == other.B && A == other.A;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as IColor);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(R, G, B, A);
        }
    }
}