using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Element;

namespace XData.Data.Components
{
    public abstract class AuthorizationConfigGetterFactory
    {
        public abstract AuthorizationConfigGetter Create(ElementContext elementContext);
    }
}
