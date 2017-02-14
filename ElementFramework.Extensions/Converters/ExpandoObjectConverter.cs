using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using XData.Data.Schema;

namespace XData.Data.Element
{
    // MVVM
    public class ExpandoObjectConverter : DictionaryConverter<ExpandoObject>
    {
        protected override object ToObj(XElement element)
        {
            if (element.Elements().All(x => x.HasElements))
            {
                ObservableCollection<ExpandoObject> results = new ObservableCollection<ExpandoObject>();
                foreach (XElement elmt in element.Elements())
                {
                    results.Add(ElementToObject(elmt));
                }
                return results;
            }
            else
            {
                return ElementToObject(element);
            }
        }

        protected override ExpandoObject ElementToObject(XElement element)
        {
            ExpandoObject exObj = new ExpandoObject();
            IDictionary<String, Object> dict = (IDictionary<String, Object>)exObj;

            List<XElement> childrenList = new List<XElement>();

            FillValues(dict, childrenList, element);

            //
            foreach (XElement children in childrenList)
            {
                ObservableCollection<ExpandoObject> childList = new ObservableCollection<ExpandoObject>();
                foreach (XElement child in children.Elements())
                {
                    childList.Add(ElementToObject(child));
                }
                dict.Add(children.Name.LocalName, childList);
            }

            return exObj;
        }


    }
}
