using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using XData.Data;
using XData.Data.Configuration;
using XData.Data.Objects;
using XData.Data.Resources;
using XData.Data.Schema;

namespace XData.Data.Element
{
    public partial class ElementContext
    {
        public const string FileNameSuffix = ".config";
        public const int MinDelay = 1000;

        protected static readonly string Directory;
        protected static readonly bool Watch;
        protected static readonly int Delay; // Milliseconds
        protected static readonly FileSystemWatcher FileSystemWatcher;
        protected static readonly Dictionary<string, ConfigurationObject> Configurations = new Dictionary<string, ConfigurationObject>();
        protected static readonly Queue Queue = Queue.Synchronized(new Queue());
        protected static DateTime LastEventTime;

        static ElementContext()
        {
            ElementFrameworkConfigurationSection configurationSection = (ElementFrameworkConfigurationSection)ConfigurationManager.GetSection("elementFramework");
            Directory = configurationSection.Instances.Directory;
            if (!System.IO.Directory.Exists(Directory))
            {
                Directory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Directory);
            }
            if (!System.IO.Directory.Exists(Directory)) throw new DirectoryNotFoundException();

            Watch = configurationSection.Instances.Watch;
            Delay = configurationSection.Instances.Delay;
            Delay = (Delay < MinDelay) ? MinDelay : Delay;

            if (Watch)
            {
                FileSystemWatcher = new FileSystemWatcher();
                FileSystemWatcher.Path = Directory;
                FileSystemWatcher.EnableRaisingEvents = Watch;
                //FileSystemWatcher.IncludeSubdirectories = false;
                FileSystemWatcher.Changed += (object sender, FileSystemEventArgs e) =>
                    {
                        LastEventTime = DateTime.Now;
                        if (!e.Name.ToLower().EndsWith(FileNameSuffix)) return;
                        Queue.Enqueue(new FileSystemEventArgs(WatcherChangeTypes.Changed, Directory, e.Name));

                    };
                FileSystemWatcher.Created += (object sender, FileSystemEventArgs e) =>
                    {
                        LastEventTime = DateTime.Now;
                        if (!e.Name.ToLower().EndsWith(FileNameSuffix)) return;
                        Queue.Enqueue(new FileSystemEventArgs(WatcherChangeTypes.Created, Directory, e.Name));
                    };
                FileSystemWatcher.Deleted += (object sender, FileSystemEventArgs e) =>
                    {
                        LastEventTime = DateTime.Now;
                        if (!e.Name.ToLower().EndsWith(FileNameSuffix)) return;
                        Queue.Enqueue(new FileSystemEventArgs(WatcherChangeTypes.Deleted, Directory, e.Name));
                    };
                FileSystemWatcher.Renamed += (object sender, RenamedEventArgs e) =>
                    {
                        LastEventTime = DateTime.Now;
                        if (!e.Name.ToLower().EndsWith(FileNameSuffix)) return;
                        Queue.Enqueue(e);
                    };
            }
        }

        protected static void UpdateConfigurations()
        {
            lock (Configurations)
            {
                Dictionary<string, FileSystemEventArgs> dict = new Dictionary<string, FileSystemEventArgs>();
                for (int i = 0; i < Queue.Count; i++)
                {
                    FileSystemEventArgs args = Queue.Dequeue() as FileSystemEventArgs;
                    if (args is RenamedEventArgs)
                    {
                        RenamedEventArgs renamedEventArgs = args as RenamedEventArgs;
                        var created = new FileSystemEventArgs(WatcherChangeTypes.Created, Directory, args.Name);
                        dict[args.Name] = created;
                        var deleted = new FileSystemEventArgs(WatcherChangeTypes.Deleted, Directory, renamedEventArgs.OldName);
                        dict[args.Name] = deleted;
                    }
                    else
                    {
                        dict[args.Name] = args;
                    }
                }

                //
                foreach (KeyValuePair<string, FileSystemEventArgs> pair in dict)
                {
                    string key = pair.Key.Substring(0, pair.Key.Length - FileNameSuffix.Length);
                    if (!Configurations.ContainsKey(key)) continue;
                    switch (pair.Value.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                        case WatcherChangeTypes.Changed:
                            string fileName = pair.Value.FullPath;
                            XElement config = XElement.Load(fileName);
                            config = config.Element("instance");
                            Configurations[key] = new ConfigurationObject(config);
                            break;
                        case WatcherChangeTypes.Deleted:
                            break;

                    }
                }
            }
        }

        public ElementContext()
            : this("default")
        {
        }

        public ElementContext(string instanceName)
        {
            string key = instanceName.ToLower();
            if (Configurations.ContainsKey(key))
            {
                if (Queue.Count > 0 && (DateTime.Now - LastEventTime).TotalMilliseconds > Delay)
                {
                    UpdateConfigurations();
                }
            }
            else
            {
                lock (Configurations)
                {
                    string fileName = Path.Combine(Directory, key + FileNameSuffix);
                    XElement config = XElement.Load(fileName);
                    config = config.Element("instance");
                    Configurations[key] = new ConfigurationObject(config);
                }
            }

            //
            ConfigurationObject configuration = Configurations[key];

            //
            Database database = configuration.DatabaseCreator.CreateInstance() as Database;
            NameMap nameMap = configuration.NameMapCreator.CreateInstance() as NameMap;
            XElement primaryConfig = new XElement(configuration.PrimaryConfig);
            IEnumerable<XElement> namedConfigs = new XElement(configuration.NamedConfigs).Elements();

            //
            Database = database;
            DatabaseSchemaObject databaseSchemaObject = new DatabaseSchemaObject(database, nameMap);
            PrimarySchemaObject primarySchemaObject = new PrimarySchemaObject(databaseSchemaObject, primaryConfig);
            SchemaManager = new SchemaManager(primarySchemaObject, namedConfigs);
            Reader = new Reader(Database);
            Writer = new Writer(Database);
            Writer.Validating += (sender, args) => { OnValidating(args); };
        }


    }
}
