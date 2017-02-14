using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Configuration;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class AuthorizorFactory
    {
        public virtual Authorizor Create(ElementContext elementContext)
        {
            AuthorizationConfigGetter authorizationConfigGetter = ConfigurationCreator.CreateAuthorizationConfigGetter(elementContext);
            return new Authorizor(elementContext, authorizationConfigGetter);
        }
    }
}
