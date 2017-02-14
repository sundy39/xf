using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    // memberNames = xPaths
    // System.ComponentModel.DataAnnotations.ValidationResult(string errorMessage, IEnumerable<string> memberNames);
    public class ValidatingEventArgs : EventArgs
    {
        public XElement Element { get; private set; }
        public ElementState ElementState { get; private set; }
        public XElement Schema { get; private set; }

        public ICollection<ValidationResult> ValidationResults { get; private set; }

        public ValidatingEventArgs(XElement element, ElementState elementState, XElement schema)
        {
            Element = new XElement(element);
            ElementState = elementState;
            Schema = new XElement(schema);
            ValidationResults = new List<ValidationResult>();
        }
    }

    // System.ComponentModel.DataAnnotations.IValidatableObject
    public delegate void ValidatingEventHandler(object sender, ValidatingEventArgs args);

    public abstract class ChangeEventArgs : EventArgs
    {
        public XElement Node { get; private set; }
        public string XPath { get; private set; }
        public XElement Element { get; private set; }
        public XElement Schema { get; private set; }

        public ChangeEventArgs(XElement node, string xPath, XElement element, XElement schema)
        {
            Node = node;
            XPath = xPath;
            Element = element;
            Schema = schema;
        }
    }

    public class InsertingEventArgs : ChangeEventArgs
    {
        public InsertingEventArgs(XElement node, string xPath, XElement element, XElement schema)
            : base(node, xPath, element, schema)
        {
        }
    }

    public class InsertedEventArgs : ChangeEventArgs
    {
        // Sql
        public ICollection<string> After { get; private set; }

        public InsertedEventArgs(XElement node, string xPath, XElement element, XElement schema)
            : base(node, xPath, element, schema)
        {
            After = new List<string>();
        }
    }

    public class DeletingEventArgs : ChangeEventArgs
    {
        // Sql
        public ICollection<string> Before { get; private set; }

        public DeletingEventArgs(XElement node, string xPath, XElement element, XElement schema)
            : base(node, xPath, element, schema)
        {
            Before = new List<string>();
        }
    }

    public class UpdatingEventArgs : ChangeEventArgs
    {
        // Sql
        public ICollection<string> Before { get; private set; }
        // Sql
        public ICollection<string> After { get; private set; }

        public UpdatingEventArgs(XElement node, string xPath, XElement element, XElement schema)
            : base(node, xPath, element, schema)
        {
            Before = new List<string>();
            After = new List<string>();
        }
    }

    public delegate void InsertingEventHandler(object sender, InsertingEventArgs args);
    public delegate void InsertedEventHandler(object sender, InsertedEventArgs args);
    public delegate void DeletingEventHandler(object sender, DeletingEventArgs args);
    public delegate void UpdatingEventHandler(object sender, UpdatingEventArgs args);
}
