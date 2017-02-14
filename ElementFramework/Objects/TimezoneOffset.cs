using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Resources;

namespace XData.Data.Objects
{
    public class TimezoneOffset
    {
        public int Minutes { get; private set; }
        public int Milliseconds { get; private set; }
        public string Suffix { get; private set; }
        private string _string;

        public TimezoneOffset(int minutes)
        {
            Minutes = minutes;

            if (Minutes == 0)            
            {
                Milliseconds = 0;
                Suffix = "+0000";
                _string = "+00:00";
                return;
            }
            
            Milliseconds = Minutes * 60 * 1000;

            string sign = (Minutes > 0) ? "+" : "-";
            int abs_minutes = Math.Abs(Minutes);
            int hours = abs_minutes / 60;
            int mins = abs_minutes % 60;
            string s_hours = (hours < 10) ? "0" + hours.ToString() : hours.ToString();
            string s_mins = (mins < 10) ? "0" + mins.ToString() : mins.ToString();
            _string = sign + s_hours + ":" + s_mins;
            Suffix = sign + s_hours + s_mins;
        }

        // +1200
        public TimezoneOffset(string suffix)
        {
            Suffix = suffix;

            if (Suffix.Length != 5) throw new ArgumentException(Suffix);
            string sign = Suffix.Substring(0, 1);
            string s_hours = Suffix.Substring(1, 2);
            string s_mins = Suffix.Substring(3, 2);

            _string = sign + s_hours + ":" + s_mins;

            Minutes = int.Parse(s_hours) * 60 + int.Parse(s_mins);
            if (sign == "-")
            {
                Minutes = -Minutes;
            }
            Milliseconds = Minutes * 60 * 1000;

            if (sign != "+") throw new ArgumentException(Suffix);
        }

        public override string ToString()
        {
            return _string;
        }

        // +12:00
        public static TimezoneOffset Parse(string s)
        {
            if (s.Length != 6) throw new ArgumentException(s);

            return new TimezoneOffset(s.Replace(":", string.Empty));
        }

        private static TimezoneOffset _zero = new TimezoneOffset(0); 
        public static TimezoneOffset Zero
        {
            get { return _zero; }
        }
    }

    public static class TimezoneOffsetExtensions
    {
        public static TimezoneOffset CreateTimezoneOffset(this XElement schema)
        {
            return TimezoneOffset.Parse(schema.Attribute(Glossary.TimezoneOffset).Value);
        }

    }

}
