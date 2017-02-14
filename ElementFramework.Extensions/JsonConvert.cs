using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public class JsonConvert
    {
        private static JsonConverter JsonConverter = new JsonConverter();

        /*
   [{
        "@Method": "Create",
        "@Resource": "Users",
        "Current": [
            {
                "UserName": "Carl",
                "Password": "carl"
            },
            {
                "UserName": "Cherry",
                "Password": "cherry"
            }
        ]
    },
    {
        "@Method": "Update",
        "@Resource": "Users",
        "@Schema": ...
        "@Referrer": ...
        "Config": {
            "@Version": "1.2.3",
            "@Name": "Staff"
        },
        "Current": {
            "Id": 2,
            "Password": "Cherry"
        },
        "Original": {
            "Id": 2,
            "Password": "cherry"
        }
    }]
         */
        public static XElement ToPacket(object value, Func<XElement, XElement, XElement> getSchema)
        {
            if (value is JObject)
            {
                return ToUnit((JToken)value, getSchema);
            }
            if (value is JArray)
            {
                XElement units = new XElement("Units");
                JArray jArray = value as JArray;
                foreach (JToken jToken in jArray)
                {
                    units.Add(ToUnit(jToken, getSchema));
                }
                return units;
            }
            throw new NotSupportedException(value.GetType().ToString());
        }

        private static XElement ToUnit(JToken obj, Func<XElement, XElement, XElement> getSchema)
        {
            JToken jCurrent = obj.First(t => (t is JProperty) && (t as JProperty).Name == "Current");
            jCurrent.Remove();

            JToken jOriginal = obj.FirstOrDefault(t => (t is JProperty) && (t as JProperty).Name == "Original");
            if (jOriginal != null) jOriginal.Remove();

            XElement unit = Newtonsoft.Json.JsonConvert.DeserializeXNode(obj.ToString(), "Unit").Root;
            string resource = unit.Attribute("Resource").Value;

            XElement config = unit.Element("Config");
            XElement schema = getSchema(unit, config);

            //
            JToken jValue = ((JProperty)jCurrent).Value;
            XElement current = ToElement(resource, jValue, schema);
            unit.Add(new XElement("Current", current));

            if (jOriginal != null)
            {
                jValue = ((JProperty)jOriginal).Value;
                XElement original = ToElement(resource, jValue, schema);
                unit.Add(new XElement("Original", original));
            }

            return unit;
        }

        public static string ToRawJson(XElement schema)
        {
            return Newtonsoft.Json.JsonConvert.SerializeXNode(schema);
        }

        public static string ToJson(XElement element)
        {
            return (string)JsonConverter.ToObject(element);
        }

        protected static DateTime BaseDate = new DateTime(1970, 1, 1);
        public static string DateTimeToJson(string name, object value)
        {
            if (value.GetType() == typeof(DateTime))
            {
                DateTime dateTime = (DateTime)value;
                double dValue = (dateTime - BaseDate).TotalMilliseconds;

                StringBuilder sb = new StringBuilder();
                sb.Append("{\"");
                sb.Append(name);
                sb.Append("\":");
                sb.Append("\"");
                sb.Append(@"\/Date(");
                sb.Append(dValue);
                sb.Append(@")\/");
                sb.Append("\"}");
                return sb.ToString();
            }

            throw new NotSupportedException(value.GetType().ToString());
        }

        public static XElement ToElement(string setName, object value, XElement schema)
        {
            if (value is JArray)
            {
                return JsonConverter.ToElement(setName, value, schema);
            }
            if (value is JObject)
            {
                string elementName = schema.GetElementSchemaBySetName(setName).Name.LocalName;
                return JsonConverter.ToElement(elementName, value, schema);
            }
            throw new NotSupportedException(value.GetType().ToString());
        }


    }
}