using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class DataSourceFactory
    {
        public virtual DataSource Create(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            return new DataSource(elementContext, specifiedConfigGetter);
        }
    }
}
