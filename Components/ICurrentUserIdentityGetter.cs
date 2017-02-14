using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;

namespace XData.Data.Components
{
    public interface ICurrentUserIdentityGetter
    {
        KeyValuePair<string, string> Get();
    }

}
