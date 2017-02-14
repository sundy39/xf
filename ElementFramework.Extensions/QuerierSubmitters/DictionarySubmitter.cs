using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class DictionarySubmitter : ElementSubmitter<Dictionary<string, object>>
    {
        public DictionarySubmitter(ElementContext elementContext)
            : base(elementContext, new DictionaryConverter())
        {
        }
    }
}
