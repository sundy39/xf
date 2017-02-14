using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public class NameMap
    {
        protected class BidirectionalDictionary<TFirst, TSecond>
        {
            public Dictionary<TFirst, TSecond> FirstToSecondDictionary
            {
                get;
                set;
            }

            public Dictionary<TSecond, TFirst> SecondToFirstDictionary
            {
                get;
                set;
            }

            public BidirectionalDictionary()
            {
                this.FirstToSecondDictionary = new Dictionary<TFirst, TSecond>();
                this.SecondToFirstDictionary = new Dictionary<TSecond, TFirst>();
            }

            public BidirectionalDictionary(Dictionary<TFirst, TSecond> firstToSecondDictionary)
                : this()
            {
                foreach (TFirst current in firstToSecondDictionary.Keys)
                {
                    this.AddValue(current, firstToSecondDictionary[current]);
                }
            }

            public virtual bool ExistsInFirst(TFirst value)
            {
                return this.FirstToSecondDictionary.ContainsKey(value);
            }

            public virtual bool ExistsInSecond(TSecond value)
            {
                return this.SecondToFirstDictionary.ContainsKey(value);
            }

            public virtual TSecond GetSecondValue(TFirst value)
            {
                if (this.ExistsInFirst(value))
                {
                    return this.FirstToSecondDictionary[value];
                }
                return default(TSecond);
            }

            public virtual TFirst GetFirstValue(TSecond value)
            {
                if (this.ExistsInSecond(value))
                {
                    return this.SecondToFirstDictionary[value];
                }
                return default(TFirst);
            }

            public void AddValue(TFirst firstValue, TSecond secondValue)
            {
                this.FirstToSecondDictionary.Add(firstValue, secondValue);
                if (!this.SecondToFirstDictionary.ContainsKey(secondValue))
                {
                    this.SecondToFirstDictionary.Add(secondValue, firstValue);
                }
            }
        }

        protected BidirectionalDictionary<string, string> ElementTableDictionary;
        protected BidirectionalDictionary<string, string> FieldColumnDictionary;

        protected BidirectionalDictionary<string, string> ElementSetDictionary;

        protected string ElementFieldFormat
        {
            get { return "{0}.{1}"; }
        }

        protected string TableColumnFormat
        {
            get { return "{0}({1})"; }
        }

        internal protected virtual string GetTableName(string elementName)
        {
            if (ElementTableDictionary.ExistsInFirst(elementName)) return ElementTableDictionary.GetSecondValue(elementName);
            return elementName;
        }

        internal protected virtual string GetElementName(string tableName)
        {
            if (ElementTableDictionary.ExistsInSecond(tableName)) return ElementTableDictionary.GetFirstValue(tableName);
            return tableName;
        }

        internal protected virtual string GetColumnName(string elementName, string fieldName)
        {
            string key = string.Format(ElementFieldFormat, elementName, fieldName);
            if (FieldColumnDictionary.ExistsInFirst(key)) return FieldColumnDictionary.GetSecondValue(key);
            return fieldName;
        }

        internal protected virtual string GetFieldName(string tableName, string columnName)
        {
            string key = string.Format(TableColumnFormat, tableName, columnName);
            if (FieldColumnDictionary.ExistsInSecond(key)) return FieldColumnDictionary.GetFirstValue(key);
            return columnName;
        }

        internal protected virtual string GetSetName(string elementName)
        {
            if (ElementSetDictionary.ExistsInFirst(elementName)) return ElementSetDictionary.GetSecondValue(elementName);
            return Glossary.SetPrefix + elementName;
        }

        internal protected virtual string GetElementNameBySetName(string setName)
        {
            if (ElementSetDictionary.ExistsInSecond(setName)) return ElementSetDictionary.GetFirstValue(setName);
            if (setName.StartsWith(Glossary.SetPrefix))
            {
                return setName.Substring(Glossary.SetPrefix.Length);
            }
            else
            {
                return setName;
            }
        }

        internal protected string NameMapVersion
        {
            get;
            private set;
        }

        private XElement _config;
        public XElement Config { get { return new XElement(_config); } }

        public NameMap(XElement nameMapConfig)
            : this(nameMapConfig, null)
        {
        }

        public NameMap(XElement nameMapConfig, string nameMapVersion)
        {
            XAttribute attr = nameMapConfig.Attribute(Glossary.NameMapVersion);
            string version = (attr == null) ? null : attr.Value;
            if (string.IsNullOrWhiteSpace(nameMapVersion))
            {                
                if (string.IsNullOrWhiteSpace(version)) throw new SchemaException(Messages.NameMap_Version_IsNullOrEmpty, nameMapConfig);

                _config = nameMapConfig;
                NameMapVersion = version;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(version))
                {
                    _config = new XElement(nameMapConfig);
                    _config.SetAttributeValue(Glossary.NameMapVersion, nameMapVersion);
                }
                else
                {
                    if (version != nameMapVersion) throw new SchemaException(Messages.NameMap_NameMapVersion_Not_Match_Argument, nameMapConfig);
                    _config = nameMapConfig;
                }
                NameMapVersion = nameMapVersion;
            }

            ElementTableDictionary = new BidirectionalDictionary<string, string>();
            FieldColumnDictionary = new BidirectionalDictionary<string, string>();
            ElementSetDictionary = new BidirectionalDictionary<string, string>();
            foreach (XElement element in nameMapConfig.Elements())
            {
                if (element.Attribute(Glossary.Table) != null)
                {
                    ElementTableDictionary.AddValue(element.Name.LocalName, element.Attribute(Glossary.Table).Value);

                    foreach (XElement field in element.Elements())
                    {
                        if (field.Attribute(Glossary.Column) != null)
                        {
                            string key = string.Format(ElementFieldFormat, element.Name.LocalName, field.Name.LocalName);
                            FieldColumnDictionary.FirstToSecondDictionary.Add(key, field.Attribute(Glossary.Column).Value);
                            key = string.Format(TableColumnFormat, element.Attribute(Glossary.Table).Value, field.Attribute(Glossary.Column).Value);
                            FieldColumnDictionary.SecondToFirstDictionary.Add(key, field.Name.LocalName);
                        }
                    }
                }
                if (element.Attribute(Glossary.Set) != null)
                {
                    ElementSetDictionary.AddValue(element.Name.LocalName, element.Attribute(Glossary.Set).Value);
                }
            }
        }

    }
}
