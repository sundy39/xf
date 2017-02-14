using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class DynamicDictionaryQuerier : ElementQuerier<DynamicDictionary>
    {
        public DynamicDictionaryQuerier(ElementContext elementContext)
            : base(elementContext, new DynamicDictionaryConverter())
        {
        }
    }
}
