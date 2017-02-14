using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class ExpandoObjectSubmitter : ElementSubmitter<ExpandoObject>
    {
        public ExpandoObjectSubmitter(ElementContext elementContext)
            : base(elementContext, new ExpandoObjectConverter())
        {
        }
    }
}
