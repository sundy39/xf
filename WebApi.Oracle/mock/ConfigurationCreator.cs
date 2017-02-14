using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Components;
using XData.Data.Element;

namespace XData.Data.Configuration
{
    public static class ConfigurationCreator
    {
        public static ElementContext CreateElementContext()
        {
            return new ElementContext();
        }

        public static SpecifiedConfigGetter CreateSpecifiedConfigGetter(ElementContext elementContext)
        {
            return null;
        }


    }
}
