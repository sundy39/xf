using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    public class Expand
    {
        public string FieldName { get; private set; }
        public string Element { get; private set; }
        public string Parent { get; private set; }
        public ICollection<Expand> Expands { get; private set; }

        public string ElementAlias { get; set; }

        public bool IsReferencePath { get; set; }

        public string Path { get; set; }

        public string Select { get; set; }

        // false
        public string Filter { get; set; }

        // false
        public string OrderBy { get; set; }

        //<{parent}>
        //  <{fieldName} Element="{element}" ...
        //  ...
        //</{parent}>
        public Expand(string fieldName, string element, string parent)
        {
            FieldName = fieldName;
            Element = element;
            Parent = parent;
            Expands = new List<Expand>();
        }

        public XElement GenerateSchema(XElement schema)
        {
            XElement expandSchema = new XElement("Expand");
            GenerateSchema(this, expandSchema, schema);
            return expandSchema.Elements().First();
        }

        protected void GenerateSchema(Expand expand, XElement expandSchema, XElement schema)
        {
            XElement fieldSchema = new XElement(expand.FieldName);
            fieldSchema.SetAttributeValue(Glossary.Element, expand.Element);
            if (!string.IsNullOrWhiteSpace(expand.ElementAlias))
            {
                fieldSchema.SetAttributeValue("ElementAlias", expand.ElementAlias);
            }

            if (!string.IsNullOrWhiteSpace(expand.Path))
            {
                if (expand.IsReferencePath)
                {
                    fieldSchema.SetAttributeValue("ReferencePath.Content", expand.Path);
                }
                else                               
                {
                    fieldSchema.SetAttributeValue("RelationshipPath.Content", expand.Path);
                }
            }
          
            if (!string.IsNullOrWhiteSpace(expand.Select))
            {
                fieldSchema.SetAttributeValue("Select", expand.Select);
            }
            if (!string.IsNullOrWhiteSpace(expand.Filter))
            {
                fieldSchema.SetAttributeValue("Filter", expand.Filter);
            }
            if (!string.IsNullOrWhiteSpace(expand.OrderBy))
            {
                fieldSchema.SetAttributeValue("OrderBy", expand.OrderBy);
            }
            expandSchema.Add(fieldSchema);
            foreach (Expand expandObj in expand.Expands)
            {
                GenerateSchema(expandObj, fieldSchema, schema);
            }
        }


    }
}
