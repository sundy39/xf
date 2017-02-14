using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class ExpandoObjectQuerier : ElementQuerier<ExpandoObject>
    {
        public ExpandoObjectQuerier(ElementContext elementContext)
            : base(elementContext, new ExpandoObjectConverter())
        {
        }
    }
}
