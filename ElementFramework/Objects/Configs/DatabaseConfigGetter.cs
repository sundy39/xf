﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Objects
{
    public abstract class DatabaseConfigGetter
    {
        internal protected XElement DatabaseSchema { get; set; }

        public abstract XElement GetDatabaseConfig();

    }
}
