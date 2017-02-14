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
    public class XDataService
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

        private XDataQuerier _xDataQuerier = null;
        protected XDataQuerier XDataQuerier
        {
            get
            {
                if (_xDataQuerier == null)
                {
                    _xDataQuerier = new XDataQuerier(ElementContext, SpecifiedConfigGetter);
                }
                return _xDataQuerier;
            }
        }

        private XDataSubmitter _xDataSubmitter = null;
        protected XDataSubmitter XDataSubmitter
        {
            get
            {
                if (_xDataSubmitter == null)
                {
                    _xDataSubmitter = new XDataSubmitter(ElementContext, SpecifiedConfigGetter);
                }
                return _xDataSubmitter;
            }
        }

        public XElement GetNow(string accept)
        {
            return XDataQuerier.GetNow(accept);
        }

        public XElement GetUtcNow(string accept)
        {
            return XDataQuerier.GetUtcNow(accept);
        }

        public XElement GetDefault(string elementName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return XDataQuerier.GetDefault(elementName, nameValuePairs, accept);
        }

        public XElement GetCount(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return XDataQuerier.GetCount(setName, nameValuePairs, accept);
        }

        public XElement GetSet(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return XDataQuerier.GetSet(setName, nameValuePairs, accept);
        }

        public XElement Find(string setNameWithKey, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept)
        {
            return XDataQuerier.Find(setNameWithKey, nameValuePairs, accept);
        }

        public XElement GetDefault(string elementName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            return XDataQuerier.GetDefault(elementName, nameValuePairs, accept, config);
        }

        public XElement GetCount(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            return XDataQuerier.GetCount(setName, nameValuePairs, accept, config);
        }

        public XElement GetSet(string setName, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            return XDataQuerier.GetSet(setName, nameValuePairs, accept, config);
        }

        public XElement Find(string setNameWithKey, IEnumerable<KeyValuePair<string, string>> nameValuePairs, string accept, XElement config)
        {
            return XDataQuerier.Find(setNameWithKey, nameValuePairs, accept, config);
        }

        public XElement Submit(XElement packet, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Submit(packet, nameValuePairs);
        }

        public XElement Create(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Create(value, nameValuePairs);
        }

        public XElement Update(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Update(value, nameValuePairs);
        }

        public XElement Delete(XElement value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Delete(value, nameValuePairs);
        }

        public XElement Submit(object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Submit(value, nameValuePairs);
        }

        public XElement Create(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Create(set, value, nameValuePairs);
        }

        public XElement Update(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Update(set, value, nameValuePairs);
        }

        public XElement Delete(string set, object value, IEnumerable<KeyValuePair<string, string>> nameValuePairs)
        {
            return XDataSubmitter.Delete(set, value, nameValuePairs);
        }


    }
}
