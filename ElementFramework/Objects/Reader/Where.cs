using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    internal abstract class Where
    {
        internal static XElement CreateWhereByKey(XElement keySchema, XElement element)
        {
            List<XElement> fieldList = new List<XElement>();
            foreach (XElement fieldSchema in keySchema.Elements())
            {
                XElement field = new XElement(fieldSchema.Name);
                field.SetAttributeValue("DataType", fieldSchema.Attribute("DataType").Value);
                field.Value = element.Element(fieldSchema.Name).Value;
                fieldList.Add(field); 
            }

            //
            XElement[] fields = fieldList.ToArray();
            XElement first = CreateEqual(fields[0]);
            for (int i = 1; i < fields.Length; i++)
            {
                XElement andAlso = CreateAndAlso();
                andAlso.Add(first);
                andAlso.Add(CreateEqual(fields[i]));
                first = andAlso;
            }
            XElement where = new XElement("Where");
            where.Add(first);
            return where;
        }

        private static XElement CreateEqual(XElement field)
        {
            return CreateEqual(field.Name.LocalName, field.Attribute("DataType").Value, field.Value);
        }

        internal static XElement CreateEqual(string fieldName, string dataType, string value)
        {
            XElement binaryExpression = new XElement("BinaryExpression");
            binaryExpression.SetAttributeValue("NodeType", "Equal");
            XElement memberExpression = new XElement("MemberExpression");
            memberExpression.SetAttributeValue("NodeType", "MemberAccess");
            memberExpression.SetAttributeValue("Member", fieldName);
            binaryExpression.Add(memberExpression);
            XElement constantExpression = new XElement("ConstantExpression");
            constantExpression.SetAttributeValue("NodeType", "Constant");
            constantExpression.SetAttributeValue("DataType", dataType);
            constantExpression.SetAttributeValue("Value", value);
            binaryExpression.Add(constantExpression);
            return binaryExpression;
        }

        internal static XElement CreateAndAlso()
        {
            XElement binaryExpression = new XElement("BinaryExpression");
            binaryExpression.SetAttributeValue("NodeType", "AndAlso");
            return binaryExpression;
        }


    }
}
