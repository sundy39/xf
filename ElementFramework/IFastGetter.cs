using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data
{
    public interface IFastGetter<T>
    {
        IEnumerable<T> ToObjects(DataTable dataTable, string objectName, XElement schema);
    }
}
