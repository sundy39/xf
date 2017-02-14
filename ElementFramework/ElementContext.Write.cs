using System;
using System.Linq;
using System.Xml.Linq;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        protected Writer Writer { get; private set; }

        public void Create(XElement element)
        {
            Writer.RegisterCreate(element, PrimarySchema);
            Writer.SaveChanges();
        }

        public void Create(XElement element, XElement schema)
        {
            Writer.RegisterCreate(element, schema);
            Writer.SaveChanges();
        }

        public void Create(XElement element, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            Writer.RegisterCreate(element, schema);
            Writer.SaveChanges();
        }

        public void Delete(XElement element)
        {
            BeforeRegisterDelete(element, PrimarySchema);
            Writer.RegisterDelete(element, PrimarySchema);
            Writer.SaveChanges();
        }

        public void Delete(XElement element, XElement schema)
        {
            BeforeRegisterDelete(element, schema);
            Writer.RegisterDelete(element, schema);
            Writer.SaveChanges();
        }

        public void Delete(XElement element, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            BeforeRegisterDelete(element, schema);
            Writer.RegisterDelete(element, schema);
            Writer.SaveChanges();
        }

        public void Update(XElement element)
        {
            Writer.RegisterUpdate(element, PrimarySchema);
            Writer.SaveChanges();
        }

        public void Update(XElement element, XElement schema)
        {
            Writer.RegisterUpdate(element, schema);
            Writer.SaveChanges();
        }

        public void Update(XElement element, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            Writer.RegisterUpdate(element, schema);
            Writer.SaveChanges();
        }

        public void UpdateWithOriginal(XElement element, XElement original)
        {
            Writer.RegisterUpdate(element, original, PrimarySchema);
            Writer.SaveChanges();
        }

        public void UpdateWithOriginal(XElement element, XElement original, XElement schema)
        {
            Writer.RegisterUpdate(element, original, schema);
            Writer.SaveChanges();
        }

        public void UpdateWithOriginal(XElement element, XElement original, string schemaName)
        {
            XElement schema = GetSchema(schemaName);
            Writer.RegisterUpdate(element, original, schema);
            Writer.SaveChanges();
        }

        public void SaveChanges(XElement packet)
        {
            RegisterInWriter(packet);
            Writer.SaveChanges();
            foreach (XElement unit in packet.Elements())
            {
                XAttribute attr = unit.Attribute("Method");
                attr.Remove();

                attr = unit.Attribute("Schema");
                if (attr != null) attr.Remove();

                attr = unit.Attribute("Config");
                if (attr != null)
                {
                    XElement config = unit.Elements("Config").FirstOrDefault(x => x.Attribute("Name") != null && x.Attribute("Name").Value == attr.Value);
                    if (config != null) config.Remove();
                    attr.Remove();
                }

                XElement original = unit.Elements().FirstOrDefault(x => x.Attribute("Original") != null && x.Attribute("Original").Value == true.ToString());
                if (original != null) original.Remove();
            }
        }

        //<Units> 
        //  <Unit Method="Create" ("@Resource":"Users" json) [Schema="Membership"]>
        //    [<Config [Version="1.2.3"] [Name="Staff"]>
        //      ...
        //    </Config>]  
        //   <Current>
        //     <Users>
        //       <User>
        //         ...
        //       </User>
        //     </Users>
        //   </Current>
        //  </Unit>
        //  <Unit Method="Delete" ("@Resource":"Users" json) [Schema="Membership"]>
        //    [<Config [Version="1.2.3"] [Name="Staff"]>
        //      ...
        //    </Config>]
        //    <Current>
        //      <User>
        //        ...
        //      </User>
        //    </Current>
        //  </Unit>
        //  <Unit Method="Update" ("@Resource":"Users" json) [Schema="Membership"]>  
        //    [<Config [Version="1.2.3"] [Name="Staff"]>
        //      ...
        //    </Config>]
        //    <Current>
        //      <User>
        //        ...
        //      </User>
        //    </Current>
        //    <Original>        
        //      <User>
        //        ...
        //      </User>
        //    </Original>
        //  </Unit>
        //</Units>
        protected void RegisterInWriter(XElement packet)
        {
            if (packet.Attribute("Method") != null)
            {
                RegisterUnitInWriter(packet);
            }
            else
            {
                foreach (XElement unit in packet.Elements())
                {
                    RegisterUnitInWriter(unit);
                }
            }
        }

        protected void RegisterUnitInWriter(XElement unit)
        {
            XAttribute attr = unit.Attribute("Method");
            string method = attr.Value;

            attr = unit.Attribute("Schema");
            string schemaName = (attr == null) ? null : attr.Value;
            XElement schema = GetSchema(schemaName);

            XElement modifying = unit.Element("Config");
            if (modifying != null)
            {
                XAttribute xVersion = modifying.Attribute("Version");
                if (xVersion != null)
                {
                    if (schema.Attribute("Version").Value != xVersion.Value)
                    {
                        throw new SchemaException(Messages.NamedConfig_Version_Not_Match_Config, modifying);
                    }
                }
                schema.Modify(modifying);

                XAttribute xNamePath = modifying.Attribute("NamePath");
                if (xNamePath == null)
                {
                    XAttribute xName = modifying.Attribute("Name");
                    if (xName != null)
                    {
                        schema.SetAttributeValue("NamePath", string.IsNullOrWhiteSpace(schemaName)
                            ? xName.Value : schemaName + "." + xName.Value);
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(schemaName))
                    {
                        schema.SetAttributeValue("NamePath", schemaName + "." + xNamePath.Value);
                    }
                }
            }

            XElement element = unit.Element("Current").Elements().First();
            XElement original = (unit.Element("Original") == null) ? null : unit.Element("Original").Elements().FirstOrDefault();

            switch (method)
            {
                case "Create":
                    Writer.RegisterCreate(element, schema);
                    break;
                case "Delete":
                    BeforeRegisterDelete(element, schema);
                    Writer.RegisterDelete(element, schema);
                    break;
                case "Update":
                    if (original == null)
                    {
                        Writer.RegisterUpdate(element, schema);
                    }
                    else
                    {
                        Writer.RegisterUpdate(element, original, schema);
                    }
                    break;
            }
        }


    }
}
