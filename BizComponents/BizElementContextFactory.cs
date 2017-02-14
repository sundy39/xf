using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Objects;

namespace XData.Data.Components
{
    public class BizElementContextFactory : ElementContextFactory
    {
        protected override void Database_Inserting(object sender, InsertingEventArgs args)
        {
            if (args.Node.Name.LocalName == "Role")
            {
                if (args.Node.Element("RoleName") != null)
                {
                    args.Node.SetElementValue("LoweredRoleName", args.Node.Element("RoleName").Value.ToLower());
                }
            }

            base.Database_Inserting(sender, args);
        }

        protected override void Database_Updating(object sender, UpdatingEventArgs args)
        {
            if (args.Node.Name.LocalName == "Role")
            {
                if (args.Node.Element("RoleName") != null)
                {
                    args.Node.SetElementValue("LoweredRoleName", args.Node.Element("RoleName").Value.ToLower());
                }
            }

            base.Database_Updating(sender, args);
        }


    }
}
