using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Objects
{
    public class SequenceFileConfigGetter : DatabaseConfigGetter
    {
        public string FileName { get; private set; }
        protected string Format { get; private set; }

        public SequenceFileConfigGetter(string fileName, string format)
        {
            FileName = fileName;
            Format = format;
        }

        public override XElement GetDatabaseConfig()
        {
            SequenceConfigGetter sg = new SequenceConfigGetter(Format);
            sg.DatabaseSchema = this.DatabaseSchema;
            XElement sc = sg.GetDatabaseConfig();

            FileDatabaseConfigGetter fg = new FileDatabaseConfigGetter(FileName);
            fg.DatabaseSchema = this.DatabaseSchema;
            XElement fc = fg.GetDatabaseConfig();

            sc.ModifyWithXAttributes(fc);
            return sc;
        }


    }
}
