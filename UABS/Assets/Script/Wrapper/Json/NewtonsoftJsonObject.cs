using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UABS.Assets.Script.Wrapper.Json
{
    public class NewtonsoftJsonObject : IJsonObject
    {
        private readonly JObject _jObject;

        public NewtonsoftJsonObject(JObject jObject)
        {
            _jObject = jObject;
        }

        public NewtonsoftJsonObject(JToken jToken)
        {
            _jObject = (JObject) jToken;
        }

        public string GetString(string key) => _jObject[key]?.ToString();
        public int GetInt(string key) => _jObject[key]?.ToObject<int>() ?? 0;
        public uint GetUInt(string key) => _jObject[key]?.ToObject<uint>() ?? 0;
        public long GetLong(string key) => _jObject[key]?.ToObject<long>() ?? 0;
        public float GetFloat(string key) => _jObject[key]?.ToObject<float>() ?? 0;
        public bool ContainsKey(string key) => _jObject.ContainsKey(key);

        public IJsonObject GetObject(string key)
        {
            return _jObject[key] is JObject token ? new NewtonsoftJsonObject(token) : null;
        }

        public List<IJsonObject> GetArray(string key)
        {
            var array = _jObject[key] as JArray;

            return array
                .OfType<JObject>()
                .Select(jObj => (IJsonObject)new NewtonsoftJsonObject(jObj))
                .ToList();
        }

        public List<int> GetIntArray(string key)
        {
            if (_jObject[key] is not JArray array) return new List<int>();
            return array.Select(x => x.ToObject<int>()).ToList();
        }

        public List<uint> GetUIntArray(string key)
        {
            if (_jObject[key] is not JArray array) return new List<uint>();
            return array.Select(x => x.ToObject<uint>()).ToList();
        }

        public List<long> GetLongArray(string key)
        {
            if (_jObject[key] is not JArray array) return new List<long>();
            return array.Select(x => x.ToObject<long>()).ToList();
        }

        public List<string> GetStringArray(string key)
        {
            if (_jObject[key] is not JArray array) return new List<string>();
            return array.Select(x => x.ToObject<string>()).ToList();
        }
    }
}
