using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public abstract class NamedConfigsGetter
    {
        internal protected XElement PrimarySchema { get; set; }

        public abstract IEnumerable<XElement> GetNamedConfigs();

    }
}
