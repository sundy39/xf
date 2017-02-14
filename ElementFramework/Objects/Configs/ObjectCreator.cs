using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public class ObjectCreator
    {
        public XElement Config { get; private set; }
        protected Type Type;
        protected Func<object> CreateFunc;
        protected Func<object[], object> CreateFuncWithParams;
        protected object[] Arguments;

        public ObjectCreator(XElement config)
        {
            Config = config;
        }

        public object CreateInstance()
        {
            if (Type == null)
            {
                string type = Config.Attribute("type").Value;
                string[] ss = type.Split(',');
                string typeName = ss[0].Trim();
                string assemblyName = ss[1].Trim();
                Assembly assembly = Assembly.Load(assemblyName);
                Type = assembly.GetType(ss[0]);
            }
            if (CreateFunc == null && CreateFuncWithParams == null)
            {
                if (!Config.HasElements)
                {
                    CreateFunc = Create2Func(Type);
                }
                else
                {
                    List<Type> types = new List<Type>();
                    List<object> list = new List<object>();
                    foreach (XElement argument in Config.Elements())
                    {
                        string typeName = argument.Attribute("type").Value;
                        if (typeName == "System.Xml.Linq.XElement")
                        {
                            types.Add(typeof(XElement));
                            XElement value = argument.Elements().First();
                            list.Add(value);
                        }
                        else
                        {
                            Type type = Type.GetType(typeName);
                            types.Add(type);
                            string value = argument.Attribute("value").Value;
                            object obj = Convert.ChangeType(value, type);
                            list.Add(obj);
                        }
                    }
                    CreateFuncWithParams = Create2Func(Type, types.ToArray());
                    Arguments = list.ToArray();
                }
            }
            if (CreateFunc == null)
            {
                return CreateFuncWithParams(Arguments);
            }
            else
            {
                return CreateFunc();
            }
        }

        private static Func<object> Create2Func(Type type)
        {
            NewExpression newExpr = Expression.New(type);
            Expression<Func<object>> lambdaExpr = Expression.Lambda<Func<object>>(newExpr, null);
            return lambdaExpr.Compile();
        }

        private static Func<object[], object> Create2Func(Type type, Type[] paramTypes)
        {
            ConstructorInfo constructor = type.GetConstructor(paramTypes);

            ParameterExpression paramExpr = Expression.Parameter(typeof(object[]), "_args");
            Expression[] arguments = GetArguments(paramTypes, paramExpr);

            NewExpression newExpr = Expression.New(constructor, arguments);

            Expression<Func<object[], object>> lambdaExpr = Expression.Lambda<Func<object[], object>>(newExpr, paramExpr);
            return lambdaExpr.Compile();
        }

        private static Expression[] GetArguments(Type[] types, ParameterExpression paramExpr)
        {
            List<Expression> exprList = new List<Expression>();
            for (int i = 0; i < types.Length; i++)
            {
                var paramObj = Expression.ArrayIndex(paramExpr, Expression.Constant(i));
                var exprObj = Expression.Convert(paramObj, types[i]);
                exprList.Add(exprObj);
            }
            return exprList.ToArray();
        }


    }
}
