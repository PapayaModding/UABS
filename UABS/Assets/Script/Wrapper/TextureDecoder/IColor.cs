using System;

namespace UABS.Assets.Script.Wrapper.TextureDecoder
{
    public interface IColor : IEquatable<IColor>
    {
        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }
    }
}