using System;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public class PrefixSuffixNameMap : NameMap
    {
        protected string Prefix = "SetOf";
        protected string Suffix = string.Empty;

        public PrefixSuffixNameMap(XElement nameMapConfig, string prefix, string suffix)
            : base(nameMapConfig)
        {
            Prefix = prefix;
            Suffix = suffix;
        }

        public PrefixSuffixNameMap(XElement nameMapConfig, string nameMapVersion, string prefix, string suffix)
            : base(nameMapConfig, nameMapVersion)
        {
            Prefix = prefix;
            Suffix = suffix;
        }

        public PrefixSuffixNameMap(XElement nameMapConfig)
            : base(nameMapConfig)
        {
        }

        public PrefixSuffixNameMap(XElement nameMapConfig, string nameMapVersion)
            : base(nameMapConfig, nameMapVersion)
        {
        }

        internal protected override string GetElementName(string tableName)
        {
            if (ElementTableDictionary.ExistsInSecond(tableName)) return ElementTableDictionary.GetFirstValue(tableName);
            return tableName;
        }

        internal protected override string GetTableName(string elementName)
        {
            if (ElementTableDictionary.ExistsInFirst(elementName)) return ElementTableDictionary.GetSecondValue(elementName);
            return elementName;
        }

        internal protected override string GetSetName(string elementName)
        {
            if (ElementSetDictionary.ExistsInFirst(elementName)) return ElementSetDictionary.GetSecondValue(elementName);
            return Prefix + elementName + Suffix;
        }

        internal protected override string GetElementNameBySetName(string setName)
        {
            if (ElementSetDictionary.ExistsInSecond(setName)) return ElementSetDictionary.GetFirstValue(setName);
            if (setName.StartsWith(Prefix) && setName.EndsWith(Suffix))
            {
                return setName.Substring(Prefix.Length, setName.Length - Prefix.Length - Suffix.Length);
            }
            throw new ArgumentException();
        }
    }
}
