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
    public class BizSpecifiedConfigGetterFactory : SpecifiedConfigGetterFactory
    {
        protected static XElement Specified = null;

        public override SpecifiedConfigGetter Create(ElementContext elementContext)
        {
            if (Specified == null)
            {
                string fileName = ConfigurationManager.AppSettings["BizSpecifiedConfigGetterFactory.FileName"];
                if (!File.Exists(fileName))
                {
                    fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
                }
                Specified = XElement.Load(fileName);
            }
            return new BizSpecifiedConfigGetter(elementContext, Specified);
        }
    }
}
