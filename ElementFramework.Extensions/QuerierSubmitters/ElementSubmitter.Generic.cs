using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using XData.Data.Objects;

namespace XData.Data.Element
{
    public class ElementSubmitter<T>
    {
        protected ElementContext ElementContext;
        protected Converter Converter;
        protected ElementSubmitter Submitter;

        public ElementSubmitter(ElementContext elementContext, Converter converter)
        {
            ElementContext = elementContext;
            Converter = converter;
            Submitter = new ElementSubmitter(elementContext);
        }

        public T Create(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Create(element, schema, false);
            return (T)Converter.ToObject(result);
        }

        public T GetCreateValidationResults(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Create(element, schema, true);
            return (T)Converter.ToObject(result);
        }

        public T Delete(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Delete(element, schema, false);
            return (T)Converter.ToObject(result);
        }

        public T GetDeleteValidationResults(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Delete(element, schema, true);
            return (T)Converter.ToObject(result);
        }

        public T Update(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Update(element, schema, false);
            return (T)Converter.ToObject(result);
        }

        public T GetUpdateValidationResults(string name, T value, XElement schema)
        {
            XElement element = Converter.ToElement(name, value, schema);
            XElement result = Submitter.Update(element, schema, true);
            return (T)Converter.ToObject(result);
        }


    }
}
