using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public class ExpandCollection : IEnumerable<Expand>, IEnumerable
    {
        private XElement _schema;
        private ICollection<Expand> _expands;

        public IEnumerator<Expand> GetEnumerator()
        {
            return _expands.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public ExpandCollection(string expand, string parent, XElement schema)
        {
            _schema = schema;
            Expand expandObj = new Expand("fieldName", "element", "parent");
            GenerateExpand("$expand=" + expand, parent, expandObj);
            _expands = expandObj.Expands;
        }

        // $expand=Trips($filter=Name eq 'Trip' $select=Id,Name $orderby=Id,Name $expand=Hotels($expand=Addrs($select=Id,Name $expand=Details),Units)),Contacts($filter=Name eq 'Trip')
        protected void GenerateExpand(string expandStr, string parent, Expand expand)
        {
            int index = expandStr.IndexOf('=');
            string input = expandStr.Substring(index + 1).Trim();

            Dictionary<string, string> dict = new Dictionary<string, string>();
            string pattern = @"\([^\(\)]*(((?'Open'\()[^\(\)]*)+((?'-Open'\))[^\(\)]*)+)*(?(Open)(?!))\)";
            input = Regex.Replace(input, pattern, new MatchEvaluator(m =>
            {
                string guid = string.Format("{{{0}}}", Guid.NewGuid().ToString());
                dict.Add(guid, m.Value);
                return guid;
            }));
            string[] expandExprs = input.Split(',');
            for (int i = 0; i < expandExprs.Length; i++)
            {
                string expandExpr = expandExprs[i].Trim();
                expandExpr = Regex.Replace(expandExpr, @"{.*?}", new MatchEvaluator(m =>
                {
                    return dict[m.Value];
                }));
                string innerStr;
                Expand expandObj = CreateExpand(expandExpr, parent, out innerStr);
                expand.Expands.Add(expandObj);
                if (!string.IsNullOrWhiteSpace(innerStr))
                {
                    GenerateExpand(innerStr, expandObj.Element, expandObj);
                }
            }
        }

        // Trips($filter=contains(Name,'Trip''s') $select=Id,Name $orderby=Id,Name $expand=Hotels($expand=Addrs($select=Id,Name $expand=Details),Units))
        protected Expand CreateExpand(string expandExpr, string parent, out string innerStr)
        {
            innerStr = null;
            int index = expandExpr.IndexOf('(');
            if (index == -1)
            {
                return new Expand(expandExpr, GetElementName(expandExpr), parent);
            }

            //
            string name = expandExpr.Substring(0, index).Trim();
            Expand expand = new Expand(name, GetElementName(name), parent);
            string bracketContent = expandExpr.Substring(index).Trim();
            bracketContent = bracketContent.Substring(1);
            bracketContent = bracketContent.Substring(0, bracketContent.Length - 1);

            //
            string pattern;
            if (!bracketContent.Contains("$expand"))
            {
                string select = null;
                pattern = @"\$select\s*=(\s*[A-Za-z_]\w*\s*,*)+\b";
                bracketContent = Regex.Replace(bracketContent, pattern, new MatchEvaluator(m =>
                {
                    select = m.Value;
                    return string.Empty;
                }));
                if (select != null)
                {
                    int idx = select.IndexOf('=');
                    select = select.Substring(idx + 1).Trim();
                    expand.Select = select;
                }
                pattern = @"\$orderby\s*=\s*(\w|,|\s)+\b";
                string orderby = null;
                bracketContent = Regex.Replace(bracketContent, pattern, new MatchEvaluator(m =>
                {
                    orderby = m.Value;
                    return string.Empty;
                }));
                if (orderby != null)
                {
                    int idx = orderby.IndexOf('=');
                    orderby = orderby.Substring(idx + 1).Trim();
                    expand.OrderBy = orderby;
                }
                bracketContent = bracketContent.Trim();
                if (bracketContent != string.Empty)
                {
                    string filter = bracketContent;
                    int idx = filter.IndexOf('=');
                    filter = filter.Substring(idx + 1).Trim();
                    expand.Filter = filter;
                }
                return expand;
            }

            // 1st
            string quote = string.Format("{{{0}}}", Guid.NewGuid().ToString());
            bracketContent = bracketContent.Replace("''", quote);

            // 2nd
            Dictionary<string, string> dict2 = new Dictionary<string, string>();
            pattern = @"'.+?'";
            bracketContent = Regex.Replace(bracketContent, pattern, new MatchEvaluator(m =>
            {
                string guid = string.Format("{{{0}}}", Guid.NewGuid().ToString());
                dict2.Add(guid, m.Value);
                return guid;
            }));

            // 3rd
            Dictionary<string, string> dict3 = new Dictionary<string, string>();
            pattern = @"\([^\(\)]*(((?'Open'\()[^\(\)]*)+((?'-Open'\))[^\(\)]*)+)*(?(Open)(?!))\)";
            bracketContent = Regex.Replace(bracketContent, pattern, new MatchEvaluator(m =>
            {
                string guid = string.Format("{{{0}}}", Guid.NewGuid().ToString());
                dict3.Add(guid, m.Value);
                return guid;
            }));

            //
            string[] parts = bracketContent.Split('$');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Recover(parts[i], quote, dict2, dict3);
                if (parts[i].StartsWith("select"))
                {
                    int idx = parts[i].IndexOf('=');
                    parts[i] = parts[i].Substring(idx + 1).Trim();
                    expand.Select = parts[i].Trim();
                }
                else if (parts[i].StartsWith("filter"))
                {
                    int idx = parts[i].IndexOf('=');
                    parts[i] = parts[i].Substring(idx + 1).Trim();
                    expand.Filter = parts[i].Trim();
                }
                else if (parts[i].StartsWith("orderby"))
                {
                    int idx = parts[i].IndexOf('=');
                    parts[i] = parts[i].Substring(idx + 1).Trim();
                    expand.OrderBy = parts[i].Trim();
                }
                else if (parts[i].StartsWith("expand"))
                {
                    innerStr = parts[i];
                }
            }
            return expand;
        }

        private static string Recover(string str, string quote, Dictionary<string, string> dict2, Dictionary<string, string> dict3)
        {
            string s = str;
            foreach (KeyValuePair<string, string> pair in dict3)
            {
                s = s.Replace(pair.Key, pair.Value);
            }
            foreach (KeyValuePair<string, string> pair in dict2)
            {
                s = s.Replace(pair.Key, pair.Value);
            }
            return s.Replace(quote, "''");
        }

        protected string GetElementName(string name)
        {
            if (_schema.GetElementSchema(name) == null)
            {
                XElement elementSchema = _schema.GetElementSchemaBySetName(name);
                return elementSchema.Name.LocalName;
            }
            else
            {
                return name;
            }
        }


    }
}
