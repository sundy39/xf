using System;
using System.Data.Entity.ModelConfiguration.Design.PluralizationServices;
using System.IO;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    // TableName is a plural
    public class SingularPluralNameMap : NameMap
    {
        internal static EnglishPluralizationService pluralizationService = new EnglishPluralizationService();

        public SingularPluralNameMap(XElement nameMapConfig)
            : base(nameMapConfig)
        {
        }

        public SingularPluralNameMap(XElement nameMapConfig, string nameMapVersion)
            : base(nameMapConfig, nameMapVersion)
        {
        }

        internal protected override string GetElementName(string tableName)
        {
            if (ElementTableDictionary.ExistsInSecond(tableName)) return ElementTableDictionary.GetFirstValue(tableName);
            return pluralizationService.Singularize(tableName);
        }

        internal protected override string GetTableName(string elementName)
        {
            if (ElementTableDictionary.ExistsInFirst(elementName)) return ElementTableDictionary.GetSecondValue(elementName);
            return pluralizationService.Pluralize(elementName);
        }

        internal protected override string GetSetName(string elementName)
        {
            if (ElementSetDictionary.ExistsInFirst(elementName)) return ElementSetDictionary.GetSecondValue(elementName);
            return pluralizationService.Pluralize(elementName);
        }

        internal protected override string GetElementNameBySetName(string setName)
        {
            if (ElementSetDictionary.ExistsInSecond(setName)) return ElementSetDictionary.GetFirstValue(setName);
            return GetElementName(setName);
        }

    }
}
