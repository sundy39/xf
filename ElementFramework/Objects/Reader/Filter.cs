using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        protected abstract class Filter
        {
            protected static readonly Dictionary<string, string> Operators = new Dictionary<string, string>();
            protected static readonly string Quotes2 = "{" + Guid.NewGuid().ToString() + "}";

            // AntiSqlInjection
            protected static readonly List<string> Whitelist = new List<string>()
            {
                "null", "true", "false",
                "eq", "ne", "gt", "ge", "lt", "le", "and", "or", "not", "has", "add", "sub", "mul", "div", "mod",
                "contains", "startswith", "endswith", "length", "indexof", "substring", "tolower", "toupper", "trim", "concat",
                "year", "month", "day", "hour", "minute", "second", "fractionalseconds", "date", "time", "totaloffsetminutes",
                "now", "utcnow", "maxdatetime", "mindatetime", "totalseconds", "round", "floor", "ceiling", "isof", "cast"
            };

            protected List<string> Quotes = new List<string>();
            protected int QuotesCount = 0;

            // NotSupported
            // has
            static Filter()
            {
                Dictionary<string, string> words = new Dictionary<string, string>();

                words.Add("eq", "=");
                words.Add("ne", "!=");
                words.Add("gt", ">");
                words.Add("ge", ">=");
                words.Add("lt", "<");
                words.Add("le", "<=");
                words.Add("and", "AND");
                words.Add("or", "OR");
                words.Add("not", "NOT");

                // NotSupported
                words.Add("has", "has");

                //
                words.Add("add", "+");
                words.Add("sub", "-");
                words.Add("mul", "*");
                words.Add("div", "/");
                words.Add("mod", "%");

                //
                foreach (KeyValuePair<string, string> pair in words)
                {
                    string blank = ((char)32).ToString();
                    Operators.Add(blank + pair.Key + blank, blank + pair.Value + blank);
                }
            }

            protected XElement Query { get; private set; }
            protected XElement Schema { get; private set; }
            protected Database Database { get; private set; }

            public XElement Where { get; protected set; }
            public string Clause { get; protected set; }

            private readonly string _string;

            private readonly IEnumerable<string> _fieldNames;
            protected IEnumerable<string> FieldNames
            {
                get { return _fieldNames; }
            }

            private readonly string _argumentPattern = @"([A-Za-z_][A-Za-z0-9_\.]*|{\d+})";

            protected virtual string GetArgumentPattern()
            {
                return _argumentPattern;
            }

            protected string ArgumentPattern
            {
                get { return GetArgumentPattern(); }
            }

            public Filter(XElement query, XElement schema, Database database)
            {
                Query = query;
                Schema = schema;
                Database = database;

                _string = Query.Element("Filter").Elements("Value").First(x => !string.IsNullOrWhiteSpace(x.Value)).Value;
                _fieldNames = Query.Element("Filter").Element("Fields").Elements().Select(x => x.Name.LocalName);

                //
                AntiSqlInjection();

                //
                Where = CreateWhere();

                //
                string value = ToString();
                value = ReplaceQuotes(value);
                value = Regulate(value);
                value = Encode(value);
                value = ReplaceTrueFalse(value);
                value = ReplaceNull(value);
                value = ReplaceOperators(value);
                value = ReplaceFunctions(value);
                value = Decorate(value);
                value = Decode(value);

                value = CorrectLike(value);

                value = value.Replace(Quotes2, "''");
                Clause = value;
            }

            protected virtual string CorrectLike(string value)
            {
                // " LIKE ('%' + 'Alfreds' + '%')" // "LIKE ('%' + 'Futterkiste')" // " LIKE ('Alfr' + '%')"
                string pattern = @"\s+LIKE\s*\(\s*'.+?\)";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string s = m.Value;
                    int index = s.IndexOf("(");
                    s = s.Substring(index + 1);
                    index = s.IndexOf(")");
                    s = s.Substring(0, index).Trim();
                    if (s.StartsWith("'%'") && s.EndsWith("'%'"))
                    {
                        s = s.Substring("'%'".Length);
                        s = s.Substring(0, s.Length - "'%'".Length).Trim();
                        if (s.StartsWith("+") && s.EndsWith("+"))
                        {
                            s = s.TrimStart('+').TrimEnd('+');
                            s = s.Trim().TrimStart('\'').TrimEnd('\'');
                            return string.Format(" LIKE ('%{0}%')", s);
                        }
                    }
                    else if (s.StartsWith("'%'") && s.EndsWith("'"))
                    {
                        s = s.Substring("'%'".Length).Trim();
                        if (s.StartsWith("+"))
                        {
                            s = s.TrimStart('+');
                            s = s.Trim().TrimStart('\'').TrimEnd('\'');
                            return string.Format(" LIKE ('%{0}')", s);
                        }
                    }
                    else if (s.StartsWith("'") && s.EndsWith("'%'"))
                    {
                        s = s.Substring(0, s.Length - "'%'".Length).Trim();
                        if (s.EndsWith("+"))
                        {
                            s = s.TrimEnd('+');
                            s = s.Trim().TrimStart('\'').TrimEnd('\'');
                            return string.Format(" LIKE ('{0}%')", s);
                        }
                    }
                    return m.Value;
                }));

                return result;
            }

            protected virtual void AntiSqlInjection()
            {
                const string BLANK = " ";

                string value = ToString();
                value = ReplaceQuotes(value);
                string pattern = @"\bdatetime'.+?'";
                value = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return BLANK;
                }));
                pattern = @"'.*?'";
                value = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return BLANK;
                }));
                if (value.Contains("'")) throw new SqlInjectionException();

                value = value.Replace("(", BLANK).Replace(")", BLANK).Replace(",", BLANK);
                string[] words = value.Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];
                    if (Whitelist.Contains(word)) continue;

                    double result;
                    if (double.TryParse(word, out result)) continue;

                    int index = word.IndexOf(".");
                    string elementName;
                    string fieldName;
                    if (index == -1)
                    {
                        elementName = this.Query.Name.LocalName;
                        fieldName = word;
                    }
                    else
                    {
                        elementName = word.Substring(0, index);
                        fieldName = word.Substring(index + 1);
                    }
                    XElement elementSchema = this.Schema.GetElementSchema(elementName);
                    if (elementSchema.Element(fieldName) == null)
                    {
                        throw new NullReferenceException();
                    }
                }                
            }

            public override string ToString()
            {
                return _string;
            }

            protected static XElement CreateEqualNull(string fieldName)
            {
                return XData.Data.Objects.Where.CreateEqual(fieldName, "System.Object", null);
            }

            protected static XElement CreateAndAlso()
            {
                return XData.Data.Objects.Where.CreateAndAlso();
            }

            protected virtual XElement CreateWhere()
            {
                List<string> collectFields = CollectFields();
                string[] additional = FieldNames.Except(collectFields).ToArray();
                if (additional.Length == 0) return null;
                XElement element = CreateEqualNull(additional[0]);
                for (int i = 1; i < additional.Length; i++)
                {
                    XElement andAlso = CreateAndAlso();
                    andAlso.Add(element);
                    andAlso.Add(CreateEqualNull(additional[i]));
                    element = andAlso;
                }
                XElement where = new XElement("Where");
                where.Add(element);
                return where;
            }

            protected List<string> CollectFields()
            {
                List<string> list = new List<string>();
                var selectFields = Query.Element("Select").Elements().Select(x => x.Name.LocalName);
                list.AddRange(selectFields);

                if (Query.Element("OrderBy") != null)
                {
                    list.Add(Query.Element("OrderBy").Elements().First().Name.LocalName);
                }
                if (Query.Element("OrderByDescending") != null)
                {
                    list.Add(Query.Element("OrderByDescending").Elements().First().Name.LocalName);
                }
                if (Query.Element("ThenBy") != null)
                {
                    list.Add(Query.Element("ThenBy").Elements().First().Name.LocalName);
                }
                if (Query.Element("ThenByDescending") != null)
                {
                    list.Add(Query.Element("ThenByDescending").Elements().First().Name.LocalName);
                }
                return list.Distinct().ToList();
            }

            protected virtual string ReplaceQuotes(string value)
            {
                string result = value;
                result = result.Replace("'''", "'" + Quotes2); // Starts with ''
                return result.Replace("''", Quotes2);
            }

            protected virtual string Regulate(string value)
            {
                string pattern = @"\bdatetime'.+?'";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string replaced = m.Value.Replace("datetime", string.Empty).Trim();
                    replaced = replaced.TrimStart('\'').TrimEnd('\'');
                    replaced = DateTime.Parse(replaced).ToCanonicalString();
                    replaced = string.Format(" '{0}'", replaced);
                    return replaced;
                }));

                pattern = @"\bboolean'(true|false|True|False)'";
                result = Regex.Replace(result, pattern, new MatchEvaluator(m =>
                {
                    string replaced = m.Value.Replace("boolean", string.Empty).Trim();
                    replaced = replaced.TrimStart('\'').TrimEnd('\'');
                    replaced = replaced.ToLower();
                    return (replaced == "true") ? "1" : "0";
                }));
                return result;
            }

            protected virtual string ReplaceNull(string value)
            {
                string pattern = @"\s+(eq|ne)\s+null\b";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return m.Value.Trim().StartsWith("eq") ? " IS NULL" : " IS NOT NULL";
                }));
                return result;
            }

            protected virtual string ReplaceTrueFalse(string value)
            {
                string pattern = @"\b(true|false|True|False)\b";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return (m.Value.ToLower() == "true") ? "1" : "0";
                }));
                return result;
            }

            protected string Encode(string value)
            {
                string pattern = @"'.+?'";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string placeholder = "{" + QuotesCount.ToString() + "}";
                    Quotes.Add(m.Value);
                    QuotesCount++;
                    return placeholder;
                }));
                return result;
            }

            protected string Decode(string value)
            {
                if (Quotes.Count == 0) return value;
                return string.Format(value, Quotes.ToArray());
            }

            protected string Decorate(string value)
            {
                string pattern = @"[A-Za-z_][A-Za-z0-9_\.]*";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    if (FieldNames.Contains(m.Value))
                    {
                        return Database.DecorateColumnName(m.Value);
                    }
                    return m.Value;
                }));
                return result;
            }

            // Operators
            protected virtual string ReplaceOperators(string value)
            {
                string result = value;
                foreach (KeyValuePair<string, string> pair in Operators)
                {
                    result = result.Replace(pair.Key, pair.Value);
                }
                return result;
            }

            // Canonical Functions
            protected virtual string ReplaceFunctions(string value)
            {
                string replaced = value;
                replaced = Replace_contains_LIKE(replaced);
                replaced = Replace_contains_IN(replaced);
                replaced = Replace_endswith(replaced);
                replaced = Replace_startswith(replaced);
                replaced = Replace_length(replaced);
                replaced = Replace_indexof(replaced);
                replaced = Replace_substring(replaced);
                replaced = Replace_tolower(replaced);
                replaced = Replace_toupper(replaced);
                replaced = Replace_trim(replaced);
                replaced = Replace_concat(replaced);
                replaced = Replace_year(replaced);
                replaced = Replace_month(replaced);
                replaced = Replace_day(replaced);
                replaced = Replace_hour(replaced);
                replaced = Replace_minute(replaced);
                replaced = Replace_second(replaced);
                replaced = Replace_fractionalseconds(replaced);
                replaced = Replace_date(replaced);
                replaced = Replace_time(replaced);
                replaced = Replace_totaloffsetminutes(replaced);
                replaced = Replace_now(replaced);
                replaced = Replace_utcnow(replaced);
                replaced = Replace_maxdatetime(replaced);
                replaced = Replace_mindatetime(replaced);
                replaced = Replace_totalseconds(replaced);
                replaced = Replace_round(replaced);
                replaced = Replace_floor(replaced);
                replaced = Replace_ceiling(replaced);
                replaced = Replace_isof(replaced);
                replaced = Replace_cast(replaced);
                return replaced;
            }

            // contains((1,3,5),Id)
            // Id IN (1,3,5)
            protected virtual string Replace_contains_IN(string value)
            {
                string pattern = string.Format(@"\bcontains\s*\(\s*\(.+?\)\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("{0} IN {1}", args[1], args[0]);
                }));
                return result;
            }

            // contains(CompanyName,'Alfreds')
            // CompanyName LIKE ('%' + 'Alfreds' + '%')
            protected virtual string Replace_contains_LIKE(string value)
            {
                string pattern = string.Format(@"\bcontains\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("{0} LIKE ('%' + {1} + '%')", args[0], args[1]);
                }));
                return result;
            }

            // endswith(CompanyName,'Futterkiste')
            // CompanyName LIKE ('%' + 'Futterkiste')
            protected virtual string Replace_endswith(string value)
            {
                string pattern = string.Format(@"\bendswith\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("{0} LIKE ('%' + {1})", args[0], args[1]);
                }));
                return result;
            }

            // startswith(CompanyName,'Alfr')
            // CompanyName LIKE ('Alfr' + '%')
            protected virtual string Replace_startswith(string value)
            {
                string pattern = string.Format(@"\bstartswith\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("{0} LIKE ({1} + '%')", args[0], args[1]);

                }));
                return result;
            }

            // length(CompanyName)  
            // DATALENGTH(CompanyName)
            protected virtual string Replace_length(string value)
            {
                string pattern = string.Format(@"\blength\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATALENGTH({0})", args[0]);

                }));
                return result;
            }

            // indexof(CompanyName,'lfreds')
            // (CHARINDEX('lfreds', CompanyName) -1)
            protected virtual string Replace_indexof(string value)
            {
                string pattern = string.Format(@"\bindexof\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("(CHARINDEX({0}, {1}) -1)", args[1], args[0]);
                }));
                return result;
            }

            // substring(CompanyName, 1)
            // SUBSTRING(CompanyName, 1 + 1, int.MaxValue)
            // substring(CompanyName, 1, 2)
            // SUBSTRING(CompanyName, 1 + 1, 2)
            protected virtual string Replace_substring(string value)
            {
                // substring(CompanyName, 1)
                string pattern = string.Format(@"\bsubstring\s*\(\s*{0}\s*,\s*\d+\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("SUBSTRING({0}, {1} + 1, {2})", args[0], args[1], int.MaxValue.ToString());
                }));

                // substring(CompanyName, 1, 2)
                pattern = string.Format(@"\bsubstring\s*\(\s*{0}\s*,\s*\d+\s*,\s*\d+\s*\)", ArgumentPattern);
                result = Regex.Replace(result, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("SUBSTRING({0}, {1} + 1, {2})", args[0], args[1], args[2]);
                }));
                return result;
            }

            // tolower(CompanyName)
            // LOWER(CompanyName)
            protected virtual string Replace_tolower(string value)
            {
                string pattern = string.Format(@"\btolower\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("LOWER({0})", args[0]);

                }));
                return result;
            }

            // toupper(CompanyName)
            // UPPER(CompanyName)
            protected virtual string Replace_toupper(string value)
            {
                string pattern = string.Format(@"\btoupper\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("UPPER({0})", args[0]);

                }));
                return result;
            }

            // trim(CompanyName)
            // LTRIM(RTRIM(CompanyName))
            protected virtual string Replace_trim(string value)
            {
                string pattern = string.Format(@"\btrim\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("LTRIM(RTRIM({0}))", args[0]);

                }));
                return result;
            }

            // concat(City, ', ', Country)
            // (City + ', ' + Country)
            protected virtual string Replace_concat(string value)
            {
                string pattern = string.Format(@"\bconcat\s*\(\s*({0}\s*,\s*)+{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return "(" + string.Join(" + ", args) + ")";
                }));
                return result;
            }

            // year(BirthDate)
            // DATEPART(YEAR, BirthDate)
            protected virtual string Replace_year(string value)
            {
                string pattern = string.Format(@"\byear\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(YEAR, {0})", args[0]);
                }));
                return result;
            }

            // month(BirthDate)
            // DATEPART(MONTH, BirthDate)
            protected virtual string Replace_month(string value)
            {
                string pattern = string.Format(@"\bmonth\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(MONTH, {0})", args[0]);
                }));
                return result;
            }

            // day(BirthDate)
            // DATEPART(DAY, BirthDate)
            protected virtual string Replace_day(string value)
            {
                string pattern = string.Format(@"\bday\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(DAY, {0})", args[0]);
                }));
                return result;
            }

            // hour(BirthDate)
            // DATEPART(HOUR, BirthDate)
            protected virtual string Replace_hour(string value)
            {
                string pattern = string.Format(@"\bhour\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(HOUR, {0})", args[0]);
                }));
                return result;
            }

            // minute(BirthDate)
            // DATEPART(MINUTE, BirthDate)
            protected virtual string Replace_minute(string value)
            {
                string pattern = string.Format(@"\bminute\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(MINUTE, {0})", args[0]);
                }));
                return result;
            }

            // second(BirthDate)
            // DATEPART(SECOND, BirthDate)
            protected virtual string Replace_second(string value)
            {
                string pattern = string.Format(@"\bsecond\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("DATEPART(SECOND, {0})", args[0]);
                }));
                return result;
            }

            // fractionalseconds(BirthDate)
            // (DATEPART(MILLISECOND, BirthDate) /1000.0)
            protected virtual string Replace_fractionalseconds(string value)
            {
                string pattern = string.Format(@"\bfractionalseconds\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("(DATEPART(MILLISECOND, {0}) /1000.0)", args[0]);
                }));
                return result;
            }

            // date(BirthDate)
            // CAST(CONVERT(char(10), BirthDate,120) AS datetime)
            protected virtual string Replace_date(string value)
            {
                string pattern = string.Format(@"\bdate\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("CAST(CONVERT(char(10), {0}, 120) AS datetime)", args[0]);
                }));
                return result;
            }

            // NotSupported
            // time(BirthDate)
            protected virtual string Replace_time(string value)
            {
                string result = value;
                return result;
            }

            // NotSupported
            // totaloffsetminutes(datetime)
            protected virtual string Replace_totaloffsetminutes(string value)
            {
                string result = value;
                return result;
            }

            // now()
            // GETDATE()
            protected virtual string Replace_now(string value)
            {
                string pattern = @"\bnow()";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return "GETDATE()";
                }));
                return result;
            }

            // utcnow()
            // GETUTCDATE()
            protected virtual string Replace_utcnow(string value)
            {
                string pattern = @"\butcnow()";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return "GETUTCDATE()";
                }));
                return result;
            }

            // NotSupported
            // maxdatetime()
            protected virtual string Replace_maxdatetime(string value)
            {
                string result = value;
                return result;
            }

            // NotSupported
            // mindatetime()
            protected virtual string Replace_mindatetime(string value)
            {
                string result = value;
                return result;
            }

            // NotSupported
            // totalseconds
            protected virtual string Replace_totalseconds(string value)
            {
                string result = value;
                return result;
            }

            // round(Amount)
            // ROUND(Amount, 0)
            protected virtual string Replace_round(string value)
            {
                string pattern = string.Format(@"\bround\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("ROUND({0}, 0)", args[0]);
                }));
                return result;
            }

            // floor(Amount)
            // FLOOR(Amount)
            protected virtual string Replace_floor(string value)
            {
                string pattern = string.Format(@"\bfloor\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("FLOOR({0})", args[0]);
                }));
                return result;
            }

            // ceiling(Amount)
            // CEILING(Amount)
            protected virtual string Replace_ceiling(string value)
            {
                string pattern = string.Format(@"\bceiling\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("CEILING({0})", args[0]);
                }));
                return result;
            }

            // NotSupported
            // isof
            protected virtual string Replace_isof(string value)
            {
                string result = value;
                return result;
            }

            // NotSupported
            // cast
            protected virtual string Replace_cast(string value)
            {
                string result = value;
                return result;
            }

            //
            protected string[] Split(string value)
            {
                int index = value.IndexOf('(');
                int lastIndex = value.LastIndexOf(')');
                string str = value.Substring(index + 1, lastIndex - index - 1);
                str = str.Trim();
                string[] args = str.Split(',');
                for (int i = 0; i < args.Length; i++)
                {
                    string arg = args[i].Trim();
                    args[i] = arg;
                }
                return args;
            }


        }
    }
}
