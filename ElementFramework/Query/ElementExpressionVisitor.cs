using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Xml.Linq;

namespace XData.Data.Query
{
    internal class ElementExpressionVisitor : ExpressionVisitor
    {
        public string GetFirstParameter(Expression node)
        {
            FirstParameter = null;
            FirstMember = null;
            Visit(node);
            return FirstParameter;
        }

        public string GetFirstParameter(Expression node, out string member)
        {        
            string result = GetFirstParameter(node);
            member = FirstMember;
            return result;
        }

        protected string FirstParameter;
        protected string FirstMember;

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Type == typeof(XElement))
            {
                Expression obj = this.Visit(node.Object);
                if (obj.NodeType == ExpressionType.Parameter)
                {
                    FirstParameter = obj.ToString();
                    IEnumerable<Expression> args = node.Arguments;
                    var first = (args as IEnumerable<Expression>).First();
                    UnaryExpression unaryExpression = first as UnaryExpression;
                    FirstMember = unaryExpression.Operand.ToString().TrimStart('"').TrimEnd('"');
                }
            }
            return base.VisitMethodCall(node);
        }
    }
}
