using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Extensions
{
    public static class XElementExtensions
    {
        public static XElement ExtractTrees(this IEnumerable<XElement> elements, string[] parentFieldNames, XElement elementSchema, bool omittedSetNodes)
        {
            XElement value = new XElement(elementSchema.Name);
            foreach (string fieldName in parentFieldNames)
            {
                value.Add(new XElement(fieldName));
            }

            IEnumerable<XElement> roots = elements.Filter(value);
            foreach(XElement root in roots)
            {
                BuildTree(elements, root, parentFieldNames, elementSchema, omittedSetNodes);
            }
            string setName = elementSchema.Attribute("Set").Value;
            return new XElement(setName, roots);
        }

        public static void BuildTree(this IEnumerable<XElement> elements, XElement root, string[] parentFieldNames, XElement elementSchema, bool omittedSetNodes)
        {
            XElement keySchema = elementSchema.GetKeySchema();
            XElement parentSchema = GetParentSchema(elementSchema, parentFieldNames);
            if (omittedSetNodes)
            {
                BuildTree(root, keySchema, parentSchema, elements, null);
            }
            else
            {
                string setName = elementSchema.Attribute("Set").Value;
                BuildTree(root, keySchema, parentSchema, elements, setName);
            }
        }

        private static XElement GetParentSchema(XElement elementSchema, string[] parentFieldNames)
        {
            XElement parentSchema = new XElement(elementSchema.Name);
            foreach (string fieldName in parentFieldNames)
            {
                parentSchema.Add(elementSchema.Element(fieldName));
            }
            return parentSchema;
        }

        private static void BuildTree(XElement element, XElement keySchema, XElement parentSchema, IEnumerable<XElement> elements, string setName)
        {
            string[] keyValues = keySchema.ExtractKey(element).Elements().Select(x => x.Value).ToArray();
            XElement parent = new XElement(parentSchema);
            int i = 0;
            foreach (XElement field in parent.Elements())
            {
                field.Value = keyValues[i];
                i++;
            }

            //
            IEnumerable<XElement> children = elements.Filter(parent);
         
            foreach (XElement child in children)
            {
                BuildTree(child, keySchema, parentSchema, elements, setName);
            }

            //
            if (string.IsNullOrWhiteSpace(setName))
            {
                element.Add(children);
            }
            else
            {
                element.Add(new XElement(setName, children));
            }
        }


    }
}
