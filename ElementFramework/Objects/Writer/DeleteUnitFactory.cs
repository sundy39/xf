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
        protected interface IDeleteUnitFactory
        {
            DeleteUnit Create(Database database, XElement element, XElement schema);
        }

        protected class DeleteUnitFactory : IDeleteUnitFactory
        {
            public DeleteUnit Create(Database database, XElement element, XElement schema)
            {
                return new DeleteUnit(database, element, schema);
            }
        }
    }
}
