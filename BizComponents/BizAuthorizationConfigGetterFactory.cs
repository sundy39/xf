using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class BizAuthorizationConfigGetterFactory : AuthorizationConfigGetterFactory
    {
        protected static XElement AuthorizationConfig = null;

        public override AuthorizationConfigGetter Create(ElementContext elementContext)
        {
            if (AuthorizationConfig == null)
            {
                string fileName = ConfigurationManager.AppSettings["BizAuthorizationConfigGetterFactory.FileName"];
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                }
                AuthorizationConfig = XElement.Load(fileName);
            }
            return new BizAuthorizationConfigGetter(elementContext, AuthorizationConfig);
        }
    }
}
