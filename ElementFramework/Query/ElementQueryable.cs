using System;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace XData.Data.Query
{
    public static class ElementQueryable
    {
        public static IQueryable<XElement> Select(this IQueryable<XElement> source, Expression<Func<XElement, XElement>> selector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(selector);

            XElement element = new XElement("Select");
            element.Add(new XElement(result.Elements().First().Attribute("Member").Value));
            query.InnerQuery.Add(element);

            return source;
        }

        public static IQueryable<XElement> Select(this IQueryable<XElement> source, Expression<Func<XElement, XElement[]>> selector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(selector);

            XElement element = new XElement("Select");
            foreach (XElement elem in result.Elements())
            {
                element.Add(new XElement(elem.Attribute("Member").Value));
            }
            query.InnerQuery.Add(element);

            return source;
        }

        public static IQueryable<XElement> Where(this IQueryable<XElement> source, Expression<Func<XElement, bool>> predicate)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(predicate);

            XElement element = new XElement("Where");
            element.Add(result.Elements());

            query.InnerQuery.Add(element);

            return source;
        }

        public static IOrderedQueryable<XElement> OrderBy(this IQueryable<XElement> source, Expression<Func<XElement, XElement>> keySelector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(keySelector);

            XElement element = new XElement("OrderBy");
            element.Add(new XElement(result.Elements().First().Attribute("Member").Value));

            query.InnerQuery.Add(element);

            return source as IOrderedQueryable<XElement>;
        }

        public static IOrderedQueryable<XElement> OrderByDescending(this IQueryable<XElement> source, Expression<Func<XElement, XElement>> keySelector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(keySelector);

            XElement element = new XElement("OrderByDescending");
            element.Add(new XElement(result.Elements().First().Attribute("Member").Value));

            query.InnerQuery.Add(element);

            return source as IOrderedQueryable<XElement>;
        }

        public static IOrderedQueryable<XElement> ThenBy(this IOrderedQueryable<XElement> source, Expression<Func<XElement, XElement>> keySelector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(keySelector);

            XElement element = new XElement("ThenBy");
            element.Add(new XElement(result.Elements().First().Attribute("Member").Value));

            query.InnerQuery.Add(element);

            return source;
        }

        public static IOrderedQueryable<XElement> ThenByDescending(this IOrderedQueryable<XElement> source, Expression<Func<XElement, XElement>> keySelector)
        {
            var query = source as ElementQuery;

            ExpressionTreeBuilder builder = new ExpressionTreeBuilder();
            XElement result = builder.ToElement(keySelector);

            XElement element = new XElement("ThenByDescending");
            element.Add(new XElement(result.Elements().First().Attribute("Member").Value));

            query.InnerQuery.Add(element);

            return source;
        }

        public static IQueryable<XElement> Skip(this IQueryable<XElement> source, int count)
        {
            var query = source as ElementQuery;

            XElement element = new XElement("Skip");
            element.Add(new XElement("Count", count));

            query.InnerQuery.Add(element);

            return source;
        }

        public static IQueryable<XElement> Take(this IQueryable<XElement> source, int count)
        {
            var query = source as ElementQuery;

            XElement element = new XElement("Take");
            element.Add(new XElement("Count", count));

            query.InnerQuery.Add(element);

            return source;
        }


    }
}
