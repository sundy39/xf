using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public abstract class SpecifiedConfigGetter
    {
        protected ElementContext ElementContext;

        private ElementQuerier _elementQuerier = null;
        protected ElementQuerier ElementQuerier
        {
            get
            {
                if (_elementQuerier == null)
                {
                    _elementQuerier = new ElementQuerier(ElementContext);
                }
                return _elementQuerier;
            }
        }

        public SpecifiedConfigGetter(ElementContext elementContext)
        {
            ElementContext = elementContext;
        }

        public abstract XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs);


    }
}
