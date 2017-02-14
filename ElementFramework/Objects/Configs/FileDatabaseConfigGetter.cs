using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public class FileDatabaseConfigGetter : DatabaseConfigGetter
    {
        protected string FileName { get; private set; }

        public FileDatabaseConfigGetter(string fileName)
        {
            FileName = fileName;
        }

        public override XElement GetDatabaseConfig()
        {
            string fileName = FileName;
            if (!File.Exists(fileName))
            {
                fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            }
            XElement databaseConfig = XElement.Load(fileName);         
            return databaseConfig;
        }


    }
}
