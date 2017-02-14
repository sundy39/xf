using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Components;
using XData.Data.Configuration;
using XData.Data.Element;

namespace XData.Data.Services
{
    public class DataService
    {
        protected ElementContext ElementContext = ConfigurationCreator.CreateElementContext();

        private SpecifiedConfigGetter _specifiedConfigGetter = null;
        protected SpecifiedConfigGetter SpecifiedConfigGetter
        {
            get
            {
                if (_specifiedConfigGetter == null)
                {
                    _specifiedConfigGetter = ConfigurationCreator.CreateSpecifiedConfigGetter(ElementContext);
                }
                return _specifiedConfigGetter;
            }
        }

        private DataSource _dataSource = null;
        protected DataSource DataSource
        {
            get
            {
                if (_dataSource == null)
                {
                    _dataSource = ConfigurationCreator.CreateDataSource(ElementContext, SpecifiedConfigGetter);
                }
                return _dataSource;
            }
        }

        public XElement Get(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return DataSource.Get(nameValuePairs, accept);
        }

        public XElement GetCount(IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return DataSource.GetCount(nameValuePairs, accept);
        }

        public XElement Create(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return DataSource.Create(value, nameValuePairs);
        }

        public XElement Update(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return DataSource.Update(value, nameValuePairs);
        }

        public XElement Delete(IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return DataSource.Delete(nameValuePairs);
        }

        public XElement Delete(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return DataSource.Delete(value, nameValuePairs);
        }


    }
}
