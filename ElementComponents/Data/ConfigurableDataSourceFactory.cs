﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Configuration;
using XData.Data.Element;

namespace XData.Data.Components
{
    public class ConfigurableDataSourceFactory : DataSourceFactory
    {
        public override DataSource Create(ElementContext elementContext, SpecifiedConfigGetter specifiedConfigGetter)
        {
            DataSourceConfigGetter dataSourceConfigGetter = ConfigurationCreator.CreateDataSourceConfigGetter(elementContext);
            return new ConfigurableDataSource(elementContext, specifiedConfigGetter, dataSourceConfigGetter);
        }
    }
}
