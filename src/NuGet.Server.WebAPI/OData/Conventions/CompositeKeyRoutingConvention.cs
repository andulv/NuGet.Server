using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Server.WebAPI.OData.Conventions
{
    /// <summary>
    /// Enables OData entities to be retrieved by URIs that use composite keys
    /// as in <c>~/odata/Packages(Id='Foo',Version='1.0')</c>.
    /// </summary>
    class CompositeKeyRoutingConvention : IODataRoutingConvention
    {
        private readonly EntityRoutingConvention entityRoutingConvention = new EntityRoutingConvention();

        readonly string _mapPackagesToControllerName;

        public CompositeKeyRoutingConvention(string mapPackagesToControllName)
        {
            _mapPackagesToControllerName = mapPackagesToControllName;
        }

        public virtual string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            var retValue = entityRoutingConvention.SelectController(odataPath, request);
            if (retValue!=null && retValue.ToUpper() == "PACKAGES")
                retValue = _mapPackagesToControllerName;
            return retValue;
        }

        public virtual string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            var action = entityRoutingConvention.SelectAction(odataPath, controllerContext, actionMap);
            if (action == null)
            {
                return null;
            }

            Utilities.DecomposeKey(controllerContext.RouteData);

            return action;
        }
    }
}
