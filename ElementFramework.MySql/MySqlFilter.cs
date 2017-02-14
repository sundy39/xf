using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public partial class MySqlDatabase
    {
        protected class MySqlFilter : Filter
        {
            public MySqlFilter(XElement query, XElement schema, Database database)
                : base(query, schema, database)
            {
            }

            // length(CompanyName)  
            // length(CompanyName)
            protected override string Replace_length(string value)
            {
                return value;
            }

            // indexof(CompanyName,'lfreds')
            // (locate(substr,str) - 1)           
            protected override string Replace_indexof(string value)
            {
                string pattern = string.Format(@"\bindexof\s*\(\s*{0}\s*,\s*{0}\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("(locate({1}, {0}) -1)", args[1], args[0]);
                }));
                return result;
            }

            // substring(CompanyName, 1)
            // substring(CompanyName, 1 + 1, int.MaxValue)
            // substring(CompanyName, 1, 2)
            // substring(CompanyName, 1 + 1, 2)
            protected override string Replace_substring(string value)
            {
                // substring(CompanyName, 1)
                string pattern = string.Format(@"\bsubstring\s*\(\s*{0}\s*,\s*\d+\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("substring({0}, {1} + 1, {2})", args[0], args[1], int.MaxValue.ToString());
                }));

                // substring(CompanyName, 1, 2)
                pattern = string.Format(@"\bsubstring\s*\(\s*{0}\s*,\s*\d+\s*,\s*\d+\s*\)", ArgumentPattern);
                result = Regex.Replace(result, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("substring({0}, {1} + 1, {2})", args[0], args[1], args[2]);
                }));
                return result;
            }

            // tolower(CompanyName)
            // lower(CompanyName)
            protected override string Replace_tolower(string value)
            {
                string pattern = string.Format(@"\btolower\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("lower({0})", args[0]);

                }));
                return result;
            }

            // toupper(CompanyName)
            // upper(CompanyName)
            protected override string Replace_toupper(string value)
            {
                string pattern = string.Format(@"\btoupper\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("upper({0})", args[0]);

                }));
                return result;
            }

            // trim(CompanyName)
            // trim(CompanyName))
            protected override string Replace_trim(string value)
            {
                return value;
            }

            // concat(City, ', ', Country)
            // concat(City, ', ', Country)
            protected override string Replace_concat(string value)
            {
                return value;
            }

            // year(BirthDate)
            // year(BirthDate)
            protected override string Replace_year(string value)
            {
                return value;
            }

            // month(BirthDate)
            // month(BirthDate)
            protected override string Replace_month(string value)
            {
                return value;
            }

            // day(BirthDate)
            // day(BirthDate)
            protected override string Replace_day(string value)
            {
                return value;
            }

            // hour(BirthDate)
            // hour(BirthDate)
            protected override string Replace_hour(string value)
            {
                return value;
            }

            // minute(BirthDate)
            // minute(BirthDate)
            protected override string Replace_minute(string value)
            {
                return value;
            }

            // second(BirthDate)
            // second(BirthDate)
            protected override string Replace_second(string value)
            {
                return value;
            }

            // NotSupported
            // fractionalseconds(BirthDate)           
            protected override string Replace_fractionalseconds(string value)
            {
                string result = value;
                return result;
            }

            // date(BirthDate)
            // str_to_date(date_format(BirthDate, '%Y-%m-%d'), '%Y-%m-%d') 
            protected override string Replace_date(string value)
            {
                string pattern = string.Format(@"\bdate\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("str_to_date(date_format({0}, '%Y-%m-%d'), '%Y-%m-%d')", args[0]);
                }));
                return result;
            }

            // time(BirthDate)
            // maketime(hour(BirthDate), minute(BirthDate), second(BirthDate)) 
            protected override string Replace_time(string value)
            {
                string pattern = string.Format(@"\btime\s*\(\s*{0}\s*\)", ArgumentPattern);
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    string[] args = Split(m.Value);
                    return string.Format("maketime(hour({0}), minute({0}), second({0}))", args[0]);
                }));
                return result;
            }

            // now()
            // now()
            protected override string Replace_now(string value)
            {
                return value;
            }

            // utcnow()
            // utc_timestamp()
            protected override string Replace_utcnow(string value)
            {
                string pattern = @"\butcnow()";
                string result = Regex.Replace(value, pattern, new MatchEvaluator(m =>
                {
                    return "utc_timestamp()";
                }));
                return result;
            }       

            // round(Amount)
            // round(Amount)
            protected override string Replace_round(string value)
            {
                return value;
            }

            // floor(Amount)
            // floor(Amount)
            protected override string Replace_floor(string value)
            {
                return value;
            }

            // ceiling(Amount)
            // CEILING(Amount)
            protected override string Replace_ceiling(string value)
            {
                return value;
            }


        }
    }
}
