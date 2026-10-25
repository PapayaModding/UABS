using UnityEngine;

namespace UabsFixtures
{
    public sealed class FixtureData : ScriptableObject
    {
        public int Sequence;
        public string StableName = string.Empty;
        public string UnicodeText = string.Empty;
        public Object ReferencedAsset;
    }
}
