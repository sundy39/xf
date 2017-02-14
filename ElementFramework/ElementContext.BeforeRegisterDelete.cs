using System;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        protected void BeforeRegisterDelete(XElement element, XElement schema)
        {
            Replace_BeforeRegisterDelete("/" + element.Name.LocalName, element, schema);
        }

        // "cascading deletes"
        protected void Replace_BeforeRegisterDelete(string xPath, XElement element, XElement schema)
        {
            foreach (XElement children in element.Elements())
            {
                if (!schema.IsSet(children)) continue;

                XElement childElementSchema = schema.GetElementSchemaBySetName(children.Name.LocalName);
                string childName = childElementSchema.Name.LocalName;
                if (children.Elements(childName).Any())
                {
                    int idx = 0;
                    bool isOnlyOne = children.Elements().Count() == 1;
                    foreach (XElement child in children.Elements())
                    {
                        idx++;
                        string path = isOnlyOne ? string.Format("{0}/{1}", xPath, child.Name.LocalName) : string.Format("{0}/{1}[{2}]", xPath, child.Name.LocalName, idx);

                        //
                        Replace_BeforeRegisterDelete(path, child, schema);
                    }
                }
                else
                {
                    string path = xPath + "/" + children.Name.LocalName;

                    if (childElementSchema.GetTimestampSchema() != null) throw new ConcurrencyCheckException(children, null, path, element, schema);
                    if (childElementSchema.GetConcurrencyCheckSchema() != null) throw new ConcurrencyCheckException(children, null, path, element, schema);

                    ReplaceChildren_BeforeRegisterDelete(children, element, schema, element, path);
                }
            }
        }

        protected void ReplaceChildren_BeforeRegisterDelete(XElement children, XElement parent, XElement schema, XElement element, string xPath)
        {
            XElement modifying = new XElement("Config");
            XElement parentSchema = new XElement(parent.Name);
            modifying.Add(parentSchema);
            XElement childrenSchema = new XElement(children.Name.LocalName);
            string childName = schema.GetElementSchemaBySetName(children.Name.LocalName).Name.LocalName;
            childrenSchema.SetAttributeValue(Glossary.Element, childName);
            parentSchema.Add(childrenSchema);
            FillModifying_BeforeRegisterDelete(children, childrenSchema, schema, element, xPath);

            //
            XElement parentElementSchema = schema.GetElementSchema(parent.Name.LocalName);
            XElement parentKeySchema = parentElementSchema.GetKeySchema();

            XElement query = new XElement(parent.Name);
            XElement select = new XElement("Select");
            foreach (XElement fieldSchema in parentKeySchema.Elements())
            {
                select.Add(new XElement(fieldSchema.Name));
            }
            query.Add(select);
            XElement where = CreateWhereByKey_BeforeRegisterDelete(parentKeySchema, parent);
            query.Add(where);
            XElement getSetSchema = new XElement(schema);
            getSetSchema.Modify(modifying);
            XElement result = Reader.GetSet(query, getSetSchema);
            result = result.Elements().First().Element(children.Name);

            //
            children.RemoveNodes();
            children.Add(result.Elements());
        }

        protected void FillModifying_BeforeRegisterDelete(XElement set, XElement parentSchema, XElement schema, XElement element, string xPath)
        {
            foreach (XElement children in set.Elements())
            {
                if (schema.IsSet(children))
                {
                    string path = xPath + "/" + children;

                    XElement childElementSchema = schema.GetElementSchemaBySetName(children.Name.LocalName);
                    if (childElementSchema.GetTimestampSchema() != null) throw new ConcurrencyCheckException(children, null, path, element, schema);
                    if (childElementSchema.GetConcurrencyCheckSchema() != null) throw new ConcurrencyCheckException(children, null, path, element, schema);

                    string childName = childElementSchema.Name.LocalName;
                    if (children.Elements(childName).Any())
                    {
                        throw new ArgumentException(set.ToString());
                    }
                    XElement childrenSchema = new XElement(children.Name.LocalName);
                    childrenSchema.SetAttributeValue(Glossary.Element, childName);
                    parentSchema.Add(childrenSchema);

                    //
                    FillModifying_BeforeRegisterDelete(children, childrenSchema, schema, element, path);
                }
            }
        }

        protected static XElement CreateWhereByKey_BeforeRegisterDelete(XElement KeySchema, XElement element)
        {
            return Where.CreateWhereByKey(KeySchema, element);
        }


    }
}
