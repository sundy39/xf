using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects.Oracle;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public partial class OracleDatabase
    {
        protected class OracleFilter : Filter
        {
            public OracleFilter(XElement query, XElement schema, Database database)
                : base(query, schema, database)
            {
            }

            protected override string Regulate(string value)
            {
                string pattern = @"[\w|\.]+\s+[eq|ne|gt|ge|lt|le]+\s+datetime'.+?'";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return GetDateTimeExpr(m.Value);
                }));

                pattern = @"\bdatetime'.+?'\s+[eq|ne|gt|ge|lt|le]+\s+[\w|\.]+";
                result = Regex.Replace(result, pattern, new MatchEvaluator(m =>
                {
                    return GetDateTimeExpr(m.Value);
                }));

                return result;
            }

            private string GetDateTimeExpr(string mValue)
            {
                DateTime? datetime = null;
                string pattern = @"\bdatetime'.+?'";
                string format = Regex.Replace(mValue, pattern, new MatchEvaluator(m =>
                {
                    string replaced = m.Value.Replace("datetime", string.Empty).Trim();
                    replaced = replaced.TrimStart('\'').TrimEnd('\'');
                    datetime = DateTime.Parse(replaced);

                    return "{0}";
                }));
                if (datetime == null) return mValue;

                string[] ss = format.Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                string sqlDbType;
                if (ss[0] == "{0}")
                {
                    sqlDbType = GetSqlDbType(ss[2]);
                }
                else
                {
                    sqlDbType = GetSqlDbType(ss[0]);
                }
                if (string.IsNullOrWhiteSpace(sqlDbType)) return mValue;

                string dateExpr = ((DateTime)datetime).DecorateDateTimeValue(sqlDbType);
                return string.Format(format, dateExpr);
            }

            protected string GetSqlDbType(string fieldName)
            {
                int index = fieldName.IndexOf(".");
                XElement elementSchema;
                string field;
                if (index == -1)
                {
                    elementSchema = Schema.GetElementSchema(Query.Name.LocalName);
                    field = fieldName;
                }
                else
                {
                    elementSchema = Schema.GetElementSchema(fieldName.Substring(0, index));
                    if (elementSchema == null) return null;
                    field = fieldName.Substring(index + 1);
                }
                XElement fieldSchema = elementSchema.Element(field);
                if (fieldSchema == null) return null;
                return fieldSchema.Attribute("SqlDbType").Value;
            }

            protected string GetDateTimeExpr(string datetime, string sqlDbType)
            {
                string replaced = datetime.Replace("datetime", string.Empty).Trim();
                replaced = replaced.TrimStart('\'').TrimEnd('\'');
                return DateTime.Parse(replaced).DecorateDateTimeValue(sqlDbType);
            }

            // Operators
            protected override string ReplaceOperators(string value)
            {
                // Rating mod 5
                string pattern = string.Format(@"[\w|\.]+\s+mod\s+[\w|\.]+", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    int index = m.Value.IndexOf("mod");
                    string arg1 = m.Value.Substring(0, index).Trim();
                    string arg2 = m.Value.Substring(index + "mod".Length).Trim();
                    return string.Format("MOD({0}, {1})", arg1, arg2);
                }));

                return base.ReplaceOperators(result);
            }

            // length(CompanyName)  
            // LENGTH(CompanyName)
            protected override string Replace_length(string value)
            {
                string pattern = string.Format(@"\blength\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("LENGTH({0})", args[0]);
                }));
                return result;
            }

            // indexof(CompanyName,'lfreds')
            // INSTR(CompanyName,'lfreds')
            protected override string Replace_indexof(string value)
            {
                string pattern = string.Format(@"\bindexof\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("(INSTR({0}, {1}) -1)", args[0], args[1]);
                }));
                return result;
            }

            // concat(City, ', ', Country)
            // (City || ', ' || Country)
            protected override string Replace_concat(string value)
            {
                string pattern = string.Format(@"\bconcat\s*\(\s*({0}\s*,\s*)+{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return "(" + string.Join(" || ", args) + ")";
                }));
                return result;
            }

            // year(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'YYYY'))
            protected override string Replace_year(string value)
            {
                string pattern = string.Format(@"\byear\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'YYYY'))", args[0]);
                }));
                return result;
            }

            // month(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'MM'))
            protected override string Replace_month(string value)
            {
                string pattern = string.Format(@"\bmonth\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'MM'))", args[0]);
                }));
                return result;
            }

            // day(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'DD'))
            protected override string Replace_day(string value)
            {
                string pattern = string.Format(@"\bday\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'DD'))", args[0]);
                }));
                return result;
            }

            // hour(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'HH24'))
            protected override string Replace_hour(string value)
            {
                string pattern = string.Format(@"\bhour\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'HH24'))", args[0]);
                }));
                return result;
            }

            // minute(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'MI'))
            protected override string Replace_minute(string value)
            {
                string pattern = string.Format(@"\bminute\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'MI'))", args[0]);
                }));
                return result;
            }

            // second(BirthDate)
            // TO_NUMBER(TO_CHAR(BirthDate,'SS'))
            protected override string Replace_second(string value)
            {
                string pattern = string.Format(@"\bsecond\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'SS'))", args[0]);
                }));
                return result;
            }

            // fractionalseconds(BirthDate)
            // BirthDate is TIMESTAMP
            // TO_NUMBER(TO_CHAR(BirthDate,'FF9')) / 1000.0)
            protected override string Replace_fractionalseconds(string value)
            {
                string pattern = string.Format(@"\bfractionalseconds\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("TO_NUMBER(TO_CHAR({0},'FF9')) / 1000000000.0)", args[0]);
                }));
                return result;
            }

            // date(BirthDate)
            // TO_TIMESTAMP(TO_CHAR(BirthDate, 'YYYY-MM-DD'),'YYYY-MM-DD')
            protected override string Replace_date(string value)
            {
                string pattern = string.Format(@"\bdate\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    string sqlDbType = GetSqlDbType(args[0]);
                    if (string.IsNullOrWhiteSpace(sqlDbType)) return m.Value;

                    if (sqlDbType == "date")
                    {
                        return string.Format("TO_DATE(TO_CHAR({0}, 'YYYY-MM-DD'),'YYYY-MM-DD')", args[0]);
                    }

                    return string.Format("TO_TIMESTAMP(TO_CHAR({0}, 'YYYY-MM-DD'),'YYYY-MM-DD')", args[0]);
                }));

                return result;
            }

            // now()
            // CURRENT_TIMESTAMP
            protected override string Replace_now(string value)
            {
                string pattern = @"[\w|\.]+\s+[eq|ne|gt|ge|lt|le]+\s+now\(\)";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] ss = m.Value.Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                    string sqlDbType = GetSqlDbType(ss[0]);
                    if (string.IsNullOrWhiteSpace(sqlDbType)) return m.Value;
                    string now;
                    if (sqlDbType == "date")
                    {
                        now = "SYSDATE";
                    }
                    else
                    {
                        now = "CURRENT_TIMESTAMP";
                    }
                    return string.Format("{0} {1} {2}", ss[0], ss[1], now);
                }));

                pattern = @"now\(\)\s+[eq|ne|gt|ge|lt|le|add|sub]+\s+[\w|\.]+";
                result = Regex.Replace(result, pattern, new MatchEvaluator(m =>
                {
                    string[] ss = m.Value.Split(new char[] { (char)32 }, StringSplitOptions.RemoveEmptyEntries);
                    string sqlDbType = GetSqlDbType(ss[2]);
                    if (string.IsNullOrWhiteSpace(sqlDbType)) return m.Value;
                    string now;
                    if (sqlDbType == "date")
                    {
                        now = "SYSDATE";
                    }
                    else
                    {
                        now = "CURRENT_TIMESTAMP";
                    }
                    return string.Format("{0} {1} {2}", now, ss[1], ss[2]);
                }));

                return result;
            }

            // NotSupported
            // utcnow()
            protected override string Replace_utcnow(string value)
            {
                string result = value;
                return result;
            }

            // ceiling(Amount)
            // CEIL(Amount)
            protected override string Replace_ceiling(string value)
            {
                string pattern = string.Format(@"\bceiling\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("CEIL({0})", args[0]);
                }));
                return result;
            }


        }
    }
}
