using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace XData.Data.Element
{
    public class ViewDataDictionaryQuerier : ElementQuerier<ViewDataDictionary>
    {
        public ViewDataDictionaryQuerier(ElementContext elementContext)
            : base(elementContext, new ViewDataDictionaryConverter())
        {
        }
    }
}
