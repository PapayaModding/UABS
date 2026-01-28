using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UABS.Wrapper
{
    public class NewtonsoftJsonSerializer : IJsonSerializer
    {
        public string Serialize(object obj)
        {
            return JsonConvert.SerializeObject(obj);
        }

        public string Serialize(object obj, bool prettyPrint)
        {
            return JsonConvert.SerializeObject(obj, prettyPrint ? Formatting.Indented : Formatting.None);
        }

        public string Serialize(List<IJsonObject> obj)
        {
            return Serialize(obj, false);
        }

        public string Serialize(List<IJsonObject> obj, bool prettyPrint)
        {
            var jArray = new JArray(
                obj.Select(o =>
                    o is NewtonsoftJsonObject njo ? njo.InnerJObject : null
                )
            );

            return JsonConvert.SerializeObject(jArray,
                prettyPrint ? Formatting.Indented : Formatting.None);
        }

        public T? Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public object? Deserialize(string json, Type type)
        {
            return JsonConvert.DeserializeObject(json, type);
        }

        public IJsonObject? DeserializeToObject(string json)
        {
            var jObject = JsonConvert.DeserializeObject<JObject>(json);
            if (jObject == null) return null;
            
            return new NewtonsoftJsonObject(jObject);
        }

        public List<IJsonObject>? DeserializeToArray(string json)
        {
            var jArray = JsonConvert.DeserializeObject<JArray>(json);
            if (jArray == null) return null;

            return jArray
                .Children<JObject>()
                .Select(jObj => (IJsonObject)new NewtonsoftJsonObject(jObj))
                .ToList();
        }

        public string SerializeNoFirstLayer(object obj)
        {
            string serialized = Serialize(obj, true);
            int start = serialized.IndexOf("{", serialized.IndexOf("InnerJObject")) + 1;
            int end = serialized.LastIndexOf("}");
            string innerContent = serialized.Substring(start, end - start - 1).Trim();
            string flattenedJson = "{\n" + innerContent + "\n";
            return flattenedJson;
        }
    }
}