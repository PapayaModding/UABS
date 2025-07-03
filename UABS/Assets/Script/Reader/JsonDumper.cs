using System;
using AssetsTools.NET;
using Newtonsoft.Json.Linq;

namespace UABS.Assets.Script.Reader
{
    public class JsonDumper
    {
        public static JToken RecurseJsonDump(AssetTypeValueField field, bool uabeFlavor)
        {
            AssetTypeTemplateField template = field.TemplateField;

            bool isArray = template.IsArray;

            if (isArray)
            {
                JArray jArray = new();

                if (template.ValueType != AssetValueType.ByteArray)
                {
                    for (int i = 0; i < field.Children.Count; i++)
                    {
                        jArray.Add(RecurseJsonDump(field.Children[i], uabeFlavor));
                    }
                }
                else
                {
                    byte[] byteArrayData = field.AsByteArray;
                    for (int i = 0; i < byteArrayData.Length; i++)
                    {
                        jArray.Add(byteArrayData[i]);
                    }
                }

                return jArray;
            }
            else
            {
                if (field.Value != null)
                {
                    AssetValueType evt = field.Value.ValueType;
                    
                    if (field.Value.ValueType != AssetValueType.ManagedReferencesRegistry)
                    {
                        object value = evt switch
                        {
                            AssetValueType.Bool => field.AsBool,
                            AssetValueType.Int8 or
                            AssetValueType.Int16 or
                            AssetValueType.Int32 => field.AsInt,
                            AssetValueType.Int64 => field.AsLong,
                            AssetValueType.UInt8 or
                            AssetValueType.UInt16 or
                            AssetValueType.UInt32 => field.AsUInt,
                            AssetValueType.UInt64 => field.AsULong,
                            AssetValueType.String => field.AsString,
                            AssetValueType.Float => field.AsFloat,
                            AssetValueType.Double => field.AsDouble,
                            _ => "invalid value"
                        };

                        return (JValue)JToken.FromObject(value);
                    }
                    else
                    {
                        // todo separate method
                        ManagedReferencesRegistry registry = field.Value.AsManagedReferencesRegistry;

                        if (registry.version == 1 || registry.version == 2)
                        {
                            JArray jArrayRefs = new JArray();

                            foreach (AssetTypeReferencedObject refObj in registry.references)
                            {
                                AssetTypeReference typeRef = refObj.type;

                                JObject jObjManagedType = new JObject
                                {
                                    { "class", typeRef.ClassName },
                                    { "ns", typeRef.Namespace },
                                    { "asm", typeRef.AsmName }
                                };

                                JObject jObjData = new JObject();

                                foreach (AssetTypeValueField child in refObj.data)
                                {
                                    jObjData.Add(child.FieldName, RecurseJsonDump(child, uabeFlavor));
                                }

                                JObject jObjRefObject;

                                if (registry.version == 1)
                                {
                                    jObjRefObject = new JObject
                                    {
                                        { "type", jObjManagedType },
                                        { "data", jObjData }
                                    };
                                }
                                else
                                {
                                    jObjRefObject = new JObject
                                    {
                                        { "rid", refObj.rid },
                                        { "type", jObjManagedType },
                                        { "data", jObjData }
                                    };
                                }

                                jArrayRefs.Add(jObjRefObject);
                            }

                            JObject jObjReferences = new JObject
                            {
                                { "version", registry.version },
                                { "RefIds", jArrayRefs }
                            };

                            return jObjReferences;
                        }
                        else
                        {
                            throw new NotSupportedException($"Registry version {registry.version} not supported!");
                        }
                    }
                }
                else
                {
                    JObject jObject = new JObject();

                    foreach (AssetTypeValueField child in field)
                    {
                        jObject.Add(child.FieldName, RecurseJsonDump(child, uabeFlavor));
                    }

                    return jObject;
                }
            }
        }
    }
}