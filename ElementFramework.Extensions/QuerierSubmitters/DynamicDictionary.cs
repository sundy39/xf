using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class DynamicDictionary : DynamicObject, IDictionary<string, object>
    {
        protected Dictionary<string, object> Dictionary = new Dictionary<string, object>();

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            string name = binder.Name.ToLower();
            return Dictionary.TryGetValue(name, out result);
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            Dictionary[binder.Name.ToLower()] = value;
            return true;
        }

        #region IDictionary<string, object>

        public void Add(string key, object value)
        {
            Dictionary.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return Dictionary.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return Dictionary.Keys; }
        }

        public bool Remove(string key)
        {
            return Dictionary.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return Dictionary.TryGetValue(key, out  value);
        }

        public ICollection<object> Values
        {
            get { return Dictionary.Values; }
        }

        public object this[string key]
        {
            get
            {
                return Dictionary[key];
            }
            set
            {
                Dictionary[key] = value;
            }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            Dictionary.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            Dictionary.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return Dictionary.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            Dictionary.ToArray().CopyTo(array, arrayIndex);
        }

        public int Count
        {
            get { return Dictionary.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return Dictionary.Remove(item.Key);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return Dictionary.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion


    }
}
