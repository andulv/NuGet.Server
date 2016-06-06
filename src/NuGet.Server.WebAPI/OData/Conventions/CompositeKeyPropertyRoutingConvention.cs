using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Server.WebAPI.OData.Conventions
{
    class CompositeKeyPropertyRoutingConvention : PropertyRoutingConvention
    {
        public override string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            var action = base.SelectAction(odataPath, controllerContext, actionMap);

            if (action != null)
            {
                Utilities.DecomposeKey(controllerContext.RouteData);
            }

            return action;
        }
    }
}
