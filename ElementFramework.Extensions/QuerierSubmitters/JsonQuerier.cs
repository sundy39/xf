using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class JsonQuerier : ElementQuerier<string>
    {
        public JsonQuerier(ElementContext elementContext)
            : base(elementContext, new JsonConverter())
        {
        }
    }
}
