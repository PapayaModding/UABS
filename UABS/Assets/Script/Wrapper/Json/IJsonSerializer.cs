using System;
using System.Collections.Generic;

namespace UABS.Assets.Script.Wrapper.Json
{
    public interface IJsonSerializer
    {
        public string Serialize(object obj);
        public string Serialize(object obj, bool prettyPrint);
        public T Deserialize<T>(string json);
        public object Deserialize(string json, Type type);

        public IJsonObject DeserializeToObject(string json);
        public List<IJsonObject> DeserializeToArray(string json);
    }
}