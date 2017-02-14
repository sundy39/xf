using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Objects
{
    public class DbForeignKey
    {
        public string Name { get; private set; }

        public string Table { get; private set; }
        public string[] Columns { get; private set; }
        public string ReferencedTable { get; private set; }
        public string[] ReferencedColumns { get; private set; }

        public DbForeignKey(string name, string table, string[] columns, string referencedTable, string[] referencedColumns)
        {
            Name = name;
            Table = table;
            Columns = columns;
            ReferencedTable = referencedTable;
            ReferencedColumns = referencedColumns;
        }
    }
}
