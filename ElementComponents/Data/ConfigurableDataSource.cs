using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;
using XData.Data.Extensions;
using XData.Data.Schema;
using XData.JScript.NET;

namespace XData.Data.Components
{
    public class ConfigurableDataSource : DataSource
    {
        protected DataSourceConfigGetter DataSourceConfigGetter;

        public ConfigurableDataSource(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter, DataSourceConfigGetter dataSourceConfigGetter)
            : base(elementContext, specifiedConfigGetter)
        {
            DataSourceConfigGetter = dataSourceConfigGetter;
        }

        public override XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            if (config == null) return base.Get(nameValuePairs, accept);

            string setName = config.Attribute("Set").Value;
            XElement schema = GetSchema(nameValuePairs);
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);

            Dictionary<string, string> variables = GetVariables(nameValuePairs, config);
            string select = GetSelect(config, variables);
            string filter = GetFilter(config, variables);
            string orderby = GetOrderby(config, variables);
            string pageIndex = GetPageIndex(config, variables);
            string pageSize = GetPageSize(config, variables);

            if (config.Attribute("default") != null)
            {
                XElement result = GetDefault(elementSchema.Name.LocalName, null, schema, accept);
                ComputeFields(result, config);
                return result;
            }

            if (config.Elements("field").Any() || config.Attribute("sort") != null)
            {
                XElement result;
                if (string.IsNullOrWhiteSpace(pageSize))
                {
                    ElementQuerier elementQuerier = new ElementQuerierEx(ElementContext);
                    result = elementQuerier.GetSet(elementSchema.Name.LocalName, select, filter, orderby, schema);
                }
                else
                {
                    int index = int.Parse(pageIndex);
                    int size = int.Parse(pageSize);
                    int skip = index * size;
                    int take = size;
                    ElementQuerier elementQuerier = new ElementQuerierEx(ElementContext);
                    result = elementQuerier.GetPage(elementSchema.Name.LocalName, select, filter, orderby, skip, take, schema);
                }
                result.SetAttributeValue("Accept", accept);

                ComputeFields(result, config);
                return Sort(result, config);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(pageSize))
                {
                    return GetSet(elementSchema.Name.LocalName, select, filter, orderby, schema, accept);
                }
                else
                {
                    int index = int.Parse(pageIndex);
                    int size = int.Parse(pageSize);
                    int skip = index * size;
                    int take = size;
                    return GetPage(elementSchema.Name.LocalName, select, filter, orderby, skip, take, schema, accept);
                }
            }
        }

        protected virtual void ComputeFields(XElement element, XElement config)
        {
            // <field name="Name" expression="(RoleName=='')?DisplayName:RoleName" />
            foreach (XElement fieldConfig in config.Elements("field"))
            {
                string name = fieldConfig.Attribute("name").Value;
                string expression = fieldConfig.Attribute("expression").Value;
                string dataType = fieldConfig.Attribute("DataType").Value;

                foreach (XElement node in element.Elements())
                {
                    Dictionary<string, string> variables = new Dictionary<string, string>();
                    foreach (XElement field in node.Elements())
                    {
                        variables.Add(field.Name.LocalName, field.Value);
                    }
                    string value = Eval(expression, variables);
                    XElement computedField = new XElement(name, value);
                    computedField.SetAttributeValue("DataType", dataType);
                    node.Add(computedField);
                }
            }
        }

        protected XElement Sort(XElement element, XElement config)
        {
            XAttribute attr = config.Attribute("sort");
            if (attr == null) return element;

            string sort = attr.Value;
            string[] items = sort.Split(',');
            string first = items[0].Trim();
            IOrderedEnumerable<XElement> result;
            if (first.EndsWith(" desc"))
            {
                string firstField = first.Substring(0, first.Length - 5).Trim();
                result = element.Elements().OrderByDescending(x => x.Element(firstField).Value);
            }
            else
            {
                string firstField = first;
                if (first.EndsWith(" asc")) firstField = first.Substring(0, first.Length - 4).Trim();
                result = element.Elements().OrderBy(x => x.Element(firstField).Value);
            }
            for (int i = 1; i < items.Length; i++)
            {
                string item = items[i].Trim();
                if (item.EndsWith(" desc"))
                {
                    string field = item.Substring(0, item.Length - 5).Trim();
                    result = result.ThenByDescending(x => x.Element(field).Value);
                }
                else
                {
                    string field = item;
                    if (field.EndsWith(" asc")) field = item.Substring(0, item.Length - 4).Trim();
                    result = result.ThenBy(x => x.Element(field).Value);
                }
            }
            return new XElement(element.Name, result.ToList());
        }

        public override XElement GetCount(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            if (config == null) return base.Get(nameValuePairs, accept);

            string setName = config.Attribute("Set").Value;
            XElement schema = GetSchema(nameValuePairs);
            XElement elementSchema = schema.GetElementSchemaBySetName(setName);
            string elementName = elementSchema.Name.LocalName;

            Dictionary<string, string> variables = GetVariables(nameValuePairs, config);
            string filter = GetFilter(config, variables);

            return GetCount(elementName, filter, schema, accept);
        }

        protected virtual string GetSelect(XElement config, Dictionary<string, string> variables)
        {
            string select = GetAttribute("select", config, variables);
            if (select.Trim() == "*") select = null;

            return select;
        }

        protected virtual string GetFilter(XElement config, Dictionary<string, string> variables)
        {
            XElement filter = config.Element("filter");
            if (filter == null)
            {
                return GetAttribute("filter", config, variables);
            }

            List<string> items = new List<string>();
            foreach (XElement item in filter.Elements())
            {
                string s = Replace(item.Attribute("value").Value, variables);
                if (s != null) items.Add(s);
            }

            string result = string.Join(" and ", items);
            return (result == string.Empty) ? null : result;
        }

        protected virtual string GetOrderby(XElement config, Dictionary<string, string> variables)
        {
            XElement orderby = config.Element("orderby");
            if (orderby == null)
            {
                return GetAttribute("orderby", config, variables);
            }

            List<string> items = new List<string>();
            foreach (XElement item in orderby.Elements())
            {
                string s = Replace(item.Attribute("value").Value, variables);
                if (s != null) items.Add(s);
            }

            string result = string.Join(",", items);
            return (result == string.Empty) ? null : result;
        }

        protected virtual string GetPageIndex(XElement config, Dictionary<string, string> variables)
        {
            string pageIndex = GetAttribute("pageIndex", config, variables);

            return pageIndex;
        }

        protected virtual string GetPageSize(XElement config, Dictionary<string, string> variables)
        {
            string pageSize = GetAttribute("pageSize", config, variables);

            return pageSize;
        }

        protected string GetAttribute(string name, XElement config, Dictionary<string, string> variables)
        {
            XAttribute attr = config.Attribute(name);
            if (attr == null) return null;

            return Replace(attr.Value, variables);
        }

        protected string Replace(string value, Dictionary<string, string> variables)
        {
            if (!value.Contains("{{")) return value;

            bool anyEmpty = false;
            string pattern = @"{{.+?}}";
            string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
            {
                string s = m.Value;
                s = s.Substring(2, s.Length - 4);
                s = Eval(s, variables);
                s = Replace(s, variables);
                if (string.IsNullOrEmpty(s)) anyEmpty = true;
                return s;
            }));

            return anyEmpty ? null : result;
        }

        protected virtual string Eval(string value, Dictionary<string, string> variables)
        {
            if (variables.ContainsKey(value)) return variables[value];

            StringBuilder sb = new StringBuilder();
            foreach (KeyValuePair<string, string> pair in variables)
            {
                sb.Append(string.Format("var {0}=\"{1}\";", pair.Key, pair.Value));
            }
            sb.Append(value + ";");
            object obj = new Evaluator().Eval(sb.ToString());
            return obj.ToString();
        }

        protected Dictionary<string, string> GetVariables(IEnumerable<KeyValuePair<string, string>> nameValuePairs, XElement config)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();

            // .../Edit/{id}
            string referrer = nameValuePairs.GetValue("referrer");
            string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            string c_referrer = config.Attribute("referrer").Value;
            string[] c_rArray = c_referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < c_rArray.Length; i++)
            {
                string s = c_rArray[i];
                if (s.StartsWith("{") && s.EndsWith("}"))
                {
                    variables.Add(s.Substring(1, s.Length - 2), rArray[i]);
                }
            }

            // QueryString
            foreach (KeyValuePair<string, string> pair in nameValuePairs)
            {
                if (pair.Key == "schema" || pair.Key == "primes") continue;
                if (pair.Key == "referrer" || pair.Key == "name") continue;
                variables.Add(pair.Key, pair.Value);
            }

            // var
            IEnumerable<XElement> vars = config.Elements("var");
            foreach (XElement xVar in vars)
            {
                SetVariable(xVar, variables);
            }

            return variables;
        }

        protected virtual void SetVariable(XElement xVar, Dictionary<string, string> variables)
        {
            string name = xVar.Attribute("name").Value;
            string key = variables[name];
            string type = xVar.Attribute("type").Value;
            if (type == "dict")
            {
                XElement pair = xVar.Elements("pair").FirstOrDefault(x => x.Attribute("key").Value == key);
                if (pair == null && key == string.Empty)
                {
                    variables[name] = string.Empty;
                    return;
                }

                string value = pair.Attribute("value").Value;
                variables[name] = value;
                return;
            }

            throw new NotSupportedException(type);
        }

        protected override void AlignToSelect(XElement element, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            if (config == null) return;

            Dictionary<string, string> variables = GetVariables(nameValuePairs, config);
            string select = GetSelect(config, variables);
            if (string.IsNullOrWhiteSpace(select)) return;

            string[] fieldNames = select.Split(',');
            for (int i = 0; i < fieldNames.Length; i++)
            {
                fieldNames[i] = fieldNames[i].Trim();
            }

            foreach (XElement field in new XElement(element).Elements())
            {
                string fieldName = field.Name.LocalName;
                if (fieldNames.Contains(fieldName)) continue;
                element.Element(fieldName).Remove();
            }
        }

        public override XElement Create(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            if (value is XElement)
            {
                return base.Create(value, nameValuePairs);
            }
            string referrer = nameValuePairs.GetValue("referrer");
            if (referrer.TrimEnd('/').EndsWith("/Create"))
            {
                return base.Create(value, nameValuePairs);
            }

            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            string setName = config.Attribute("Set").Value;
            XElement schema = GetSchema(nameValuePairs);

            XElement element = ToElement(setName, value, schema);
            AttachAttributes(element, nameValuePairs);
            AlignToSelect(element, nameValuePairs);

            XElement master = GetMaster(value, nameValuePairs, schema);
            if (master != null)
            {
                schema.SetRelatedValue(element, master);
            }

            ElementContext.Create(element, schema);
            return element;
        }

        protected override XElement GetMaster(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs, XElement schema)
        {
            JProperty master = (JProperty)((JContainer)value).Last;
            if (master.Value is JObject)
            {
                string referrer = nameValuePairs.GetValue("referrer");

                string name = master.Name;
                List<KeyValuePair<string, string>> nameValPairs = new List<KeyValuePair<string, string>>();
                nameValPairs.Add(new KeyValuePair<string, string>("referrer", referrer));
                if (name != "undefined")
                {
                    nameValPairs.Add(new KeyValuePair<string, string>("name", name));
                }
                XElement config = DataSourceConfigGetter.Get(nameValPairs);
                string setName = config.Attribute("Set").Value;
                XElement element = ToElement(setName, master.Value, schema);
                return element;
            }
            return null;
        }

        public override XElement Update(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            if (value is XElement)
            {
                return base.Update(value, nameValuePairs);
            }
            string referrer = nameValuePairs.GetValue("referrer");
            string[] rArray = referrer.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            if (rArray.Length > 1 && rArray[rArray.Length - 2] == "Edit")
            {
                return base.Update(value, nameValuePairs);
            }

            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            string setName = config.Attribute("Set").Value;
            XElement schema = GetSchema(nameValuePairs);

            XElement element = ToElement(setName, value, schema);
            AttachAttributes(element, nameValuePairs);

            AlignToSelect(element, nameValuePairs);
            ElementContext.Update(element, schema);
            return element;
        }

        public override XElement Delete(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            if (value is XElement)
            {
                return base.Delete(value, nameValuePairs);
            }

            XElement config = DataSourceConfigGetter.Get(nameValuePairs);
            string setName = config.Attribute("Set").Value;
            XElement schema = GetSchema(nameValuePairs);

            XElement element = ToElement(setName, value, schema);
            AttachAttributes(element, nameValuePairs);

            AlignToSelect(element, nameValuePairs);
            ElementContext.Delete(element, schema);
            return element;
        }


    }
}
