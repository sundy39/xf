﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Element
{
    public class JsonSubmitter : ElementSubmitter<string>
    {
        public JsonSubmitter(ElementContext elementContext)
            : base(elementContext, new JsonConverter())
        {
        }
    }
}
