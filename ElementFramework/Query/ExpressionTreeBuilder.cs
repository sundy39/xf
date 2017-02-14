using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace XData.Data.Query
{
    internal class ExpressionTreeBuilder
    {
        public XElement ToElement(Expression expression)
        {
            XElement element = new XElement("Expression");
            TextureElement(expression, element);
            return element;
        }

        private void Build(Expression expr, XElement exprElement, params XAttribute[] attributes)
        {
            if (expr is ConstantExpression)
            {
                ConstantExpression constExpr = expr as ConstantExpression;
                TextureElement(constExpr, exprElement, attributes);
            }
            else
            {
                var visitor = new ElementExpressionVisitor();
                string parameter = visitor.GetFirstParameter(expr);
                if (string.IsNullOrWhiteSpace(parameter))
                {
                    if (expr is UnaryExpression)
                    {
                        TextureElement(expr, exprElement, attributes);
                        return;
                    }

                    if (expr.Type == typeof(bool) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(bool))
                    {
                        Expression<Func<bool>> lambda = Expression.Lambda<Func<bool>>(expr);
                        object value = (bool)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(int) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(int))
                    {
                        Expression<Func<int>> lambda = Expression.Lambda<Func<int>>(expr);
                        object value = (int)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(long) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(long))
                    {
                        Expression<Func<long>> lambda = Expression.Lambda<Func<long>>(expr);
                        object value = (long)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(byte) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(byte))
                    {
                        Expression<Func<byte>> lambda = Expression.Lambda<Func<byte>>(expr);
                        object value = (byte)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(decimal) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(decimal))
                    {
                        Expression<Func<decimal>> lambda = Expression.Lambda<Func<decimal>>(expr);
                        object value = (decimal)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(float) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(float))
                    {
                        Expression<Func<float>> lambda = Expression.Lambda<Func<float>>(expr);
                        object value = (float)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(double) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(double))
                    {
                        Expression<Func<double>> lambda = Expression.Lambda<Func<double>>(expr);
                        object value = (double)lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                        return;
                    }
                    if (expr.Type == typeof(DateTime) ||
                        expr is MethodCallExpression && (expr as MethodCallExpression).Method.ReturnType == typeof(DateTime))
                    {
                        Expression<Func<DateTime>> lambda = Expression.Lambda<Func<DateTime>>(expr);
                        DateTime value = (DateTime)lambda.Compile().Invoke();

                        //
                        List<XAttribute> attrList = new List<XAttribute>(attributes);
                        attrList.Add(new XAttribute("DataType", typeof(DateTime).FullName));
                        attributes = attrList.ToArray();

                        // 
                        TextureElement(Expression.Constant(value.ToCanonicalString()), exprElement, attributes);
                    }
                    else
                    {
                        Expression<Func<object>> lambda = Expression.Lambda<Func<object>>(expr);
                        object value = lambda.Compile().Invoke();
                        TextureElement(Expression.Constant(value), exprElement, attributes);
                    }
                }
                else
                {
                    TextureElement(expr, exprElement, attributes);
                }
            }
        }

        private void TextureElement(Expression expression, XElement nodeElement, params XAttribute[] attributes)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Negate:
                case ExpressionType.NegateChecked:
                case ExpressionType.Not:
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.ArrayLength:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    {
                        var expr = expression as UnaryExpression;
                        XElement exprElement = new XElement("UnaryExpression");
                        //exprElement.SetAttributeValue("Type", expr.GetType().Name);
                        exprElement.SetAttributeValue("NodeType", expr.NodeType.ToString());
                        foreach (var attr in attributes)
                        {
                            exprElement.SetAttributeValue(attr.Name, attr.Value);
                        }
                        nodeElement.Add(exprElement);

                        Build(expr.Operand, exprElement);
                    }
                    return;
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                case ExpressionType.Divide:
                case ExpressionType.Modulo:
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                case ExpressionType.LessThan:
                case ExpressionType.LessThanOrEqual:
                case ExpressionType.GreaterThan:
                case ExpressionType.GreaterThanOrEqual:
                case ExpressionType.Equal:
                case ExpressionType.NotEqual:
                case ExpressionType.Coalesce:
                case ExpressionType.ArrayIndex:
                case ExpressionType.RightShift:
                case ExpressionType.LeftShift:
                case ExpressionType.ExclusiveOr:
                    {
                        var expr = expression as BinaryExpression;
                        XElement exprElement = new XElement("BinaryExpression");
                        //exprElement.SetAttributeValue("Type", expr.GetType().Name);
                        exprElement.SetAttributeValue("NodeType", expr.NodeType.ToString());
                        foreach (var attr in attributes)
                        {
                            exprElement.SetAttributeValue(attr.Name, attr.Value);
                        }
                        nodeElement.Add(exprElement);

                        Build(expr.Left, exprElement);
                        Build(expr.Right, exprElement);
                    }
                    return;
                case ExpressionType.TypeIs:
                    {
                        var expr = expression as TypeBinaryExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.Conditional:
                    {
                        var expr = expression as ConditionalExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.Constant:
                    {
                        var expr = expression as ConstantExpression;
                        XElement exprElement = new XElement("ConstantExpression");
                        //exprElement.SetAttributeValue("Type", expr.GetType().Name);
                        exprElement.SetAttributeValue("NodeType", expr.NodeType.ToString());
                        if (expr.Value is IList)
                        {
                            string s = string.Empty;
                            foreach (object obj in (expr.Value as IList))
                            {
                                s += obj.ToString() + ",";
                            }
                            s = s.TrimEnd(',');
                            exprElement.SetAttributeValue("Value", s);
                            exprElement.SetAttributeValue("DataType", typeof(IList).FullName);
                        }
                        else
                        {
                            exprElement.SetAttributeValue("Value", expr.Value);
                            exprElement.SetAttributeValue("DataType", expr.Type.FullName);
                        }
                        foreach (var attr in attributes)
                        {
                            exprElement.SetAttributeValue(attr.Name, attr.Value);
                        }
                        nodeElement.Add(exprElement);
                    }
                    return;
                case ExpressionType.Parameter:
                    {
                        var expr = expression as ParameterExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.MemberAccess:
                    {
                        var expr = expression as MemberExpression;
                        TextureElement(expr.Expression, nodeElement);
                        return;
                    }
                    throw new NotSupportedException(expression.ToString());
                case ExpressionType.Call:
                    {
                        var expr = expression as MethodCallExpression;

                        if (expr.Method.DeclaringType == typeof(System.Convert))
                        {
                            TextureElement(expr.Arguments[0], nodeElement);
                            return;
                        }
                        if (expr.Method.Name == "Parse")
                        {
                            TextureElement(expr.Arguments[0], nodeElement);
                            return;
                        }
                        if (expr.Method.Name == "Contains")
                        {
                            Type type = expr.Object.Type.GetInterface("IList");
                            if (type != null)
                            {
                                Expression<Func<IList>> lambda = Expression.Lambda<Func<IList>>(expr.Object);
                                IList value = lambda.Compile().Invoke();

                                ConstantExpression left;
                                Expression right;
                                if (expr.Arguments[0] is MethodCallExpression)
                                {
                                    List<string> strList = new List<string>();
                                    foreach (var obj in value)
                                    {
                                        if (obj.GetType() == typeof(DateTime))
                                        {
                                            strList.Add(((DateTime)obj).ToCanonicalString());
                                        }
                                        else
                                        {
                                            strList.Add(obj.ToString());
                                        }
                                    }
                                    left = Expression.Constant(strList, typeof(IList));
                                    right = (expr.Arguments[0] as MethodCallExpression).Arguments[0];
                                }
                                else
                                {
                                    left = Expression.Constant(value, typeof(IList));
                                    right = expr.Arguments[0];
                                }

                                BinaryExpression BinaryExpression = Expression.MakeBinary(ExpressionType.Equal, left, right);
                                TextureElement(BinaryExpression, nodeElement, new XAttribute("NodeType", expr.Method.Name));
                                return;
                            }
                        }
                        if (expr.Method.Name == "Contains" || expr.Method.Name == "StartsWith" || expr.Method.Name == "EndsWith")
                        {
                            BinaryExpression BinaryExpression = Expression.MakeBinary(ExpressionType.Equal, expr.Object, expr.Arguments[0]);
                            TextureElement(BinaryExpression, nodeElement, new XAttribute("NodeType", expr.Method.Name));
                            return;
                        }
                        if (expr.Type == typeof(XElement))
                        {
                            var visitor = new ElementExpressionVisitor();
                            string member;
                            string parameter = visitor.GetFirstParameter(expr, out member);

                            if (!string.IsNullOrWhiteSpace(parameter))
                            {
                                XElement exprElement = new XElement("MemberExpression");
                                //exprElement.SetAttributeValue("Type", expr.GetType().Name);
                                //exprElement.SetAttributeValue("NodeType", expr.NodeType.ToString());
                                exprElement.SetAttributeValue("NodeType", "MemberAccess");
                                exprElement.SetAttributeValue("Member", member);
                                foreach (var attr in attributes)
                                {
                                    exprElement.SetAttributeValue(attr.Name, attr.Value);
                                }
                                nodeElement.Add(exprElement);
                                return;
                            }
                        }
                    }
                    throw new NotImplementedException();
                case ExpressionType.Lambda:
                    {
                        var expr = expression as LambdaExpression;
                        TextureElement(expr.Body, nodeElement);
                    }
                    return;
                case ExpressionType.New:
                    {
                        var expr = expression as NewExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.NewArrayInit:
                case ExpressionType.NewArrayBounds:
                    {
                        var expr = expression as NewArrayExpression;
                        foreach (Expression expre in expr.Expressions)
                        {
                            TextureElement(expre, nodeElement);
                        }
                    }
                    return;
                case ExpressionType.Invoke:
                    {
                        var expr = expression as InvocationExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.MemberInit:
                    {
                        var expr = expression as MemberInitExpression;
                    }
                    throw new NotImplementedException();
                case ExpressionType.ListInit:
                    {
                        var expr = expression as ListInitExpression;
                    }
                    throw new NotImplementedException();
                default:
                    throw new NotSupportedException(expression.NodeType.ToString());
            }
        }
    }
}
