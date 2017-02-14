using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Schema;
using XData.Data.Resources;
using System.Diagnostics;
using System.ComponentModel;

namespace XData.Data.Objects
{
    public abstract partial class Database
    {
        protected interface ICreateUnitFactory
        {
            CreateUnit Create(Database database, XElement element, XElement schema);
        }

        protected class CreateUnitFactory : ICreateUnitFactory
        {
            public CreateUnit Create(Database database, XElement element, XElement schema)
            {
                return new CreateUnit(database, element, schema);
            }
        }
    }
}
