using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public partial class SqlDatabase : Database
    {
        protected class SqlFilter : Filter
        {
            public SqlFilter(XElement query, XElement schema, Database database)
                : base(query, schema, database)
            {
            }


        }
    }
}
