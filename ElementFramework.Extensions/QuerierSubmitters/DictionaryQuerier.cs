using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class DictionaryQuerier : ElementQuerier<Dictionary<string, object>>
    {
        public DictionaryQuerier(ElementContext elementContext)
            : base(elementContext, new DictionaryConverter())
        {
        }
    }
}
