using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace UABS.Assets.Script.Wrapper.Json
{
    public class NewtonsoftJsonObject : IJsonObject
    {
        private readonly JObject _jObject;
        public JObject InnerJObject => _jObject;

        public NewtonsoftJsonObject(JObject jObject)
        {
            _jObject = jObject;
        }

        public NewtonsoftJsonObject(JToken jToken)
        {
            _jObject = (JObject)jToken;
        }

        public string GetString(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            JToken token = _jObject[key];

            if (token == null || token.Type == JTokenType.Null)
                return null;

            return token.ToString();
        }
        public int GetInt(string key) => _jObject[key]?.ToObject<int>() ?? 0;
        public uint GetUInt(string key) => _jObject[key]?.ToObject<uint>() ?? 0;
        public long GetLong(string key) => _jObject[key]?.ToObject<long>() ?? 0;
        public float GetFloat(string key) => _jObject[key]?.ToObject<float>() ?? 0;
        public bool ContainsKey(string key) => _jObject.ContainsKey(key);

        public bool SetString(string key, string val)
        {
            try
            {
                _jObject[key] = val;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public IJsonObject GetObject(string key)
        {
            return _jObject[key] is JObject token ? new NewtonsoftJsonObject(token) : null;
        }

        public bool SetObject(string key, IJsonObject value)
        {
            try
            {
                _jObject[key] = ((NewtonsoftJsonObject) value).InnerJObject;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public List<IJsonObject> GetArray(string key)
        {
            if (_jObject == null || key == null)
                return new List<IJsonObject>();

            if (!_jObject.TryGetValue(key, out JToken token) || token.Type != JTokenType.Array)
                return new List<IJsonObject>();

            var array = (JArray)token;

            return array
                .OfType<JObject>()
                .Select(jObj => (IJsonObject)new NewtonsoftJsonObject(jObj))
                .ToList();
        }

        public bool SetArray(string key, List<IJsonObject> values)
        {
            try
            {
                // Convert each IJsonObject to JObject
                var jArray = new JArray(
                    values.Select(val =>
                        (val is NewtonsoftJsonObject njo) ? njo._jObject : null
                    )
                );

                _jObject[key] = jArray;
                return true;
            }
            catch
            {
                return false;
            }
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
