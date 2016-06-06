using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;

namespace NuGet.Server.WebAPI.OData.Conventions
{
    class NonBindableActionRoutingConvention : IODataRoutingConvention
    {
        private readonly string controllerName;

        public NonBindableActionRoutingConvention(string controllerName)
        {
            this.controllerName = controllerName;
        }

        public string SelectController(ODataPath odataPath, HttpRequestMessage request)
        {
            if (odataPath.PathTemplate == "~/action")
            {
                return controllerName;
            }
            return null;
        }

        // Route the action to a method with the same name as the action.
        public string SelectAction(ODataPath odataPath, HttpControllerContext controllerContext, ILookup<string, HttpActionDescriptor> actionMap)
        {
            if (odataPath.PathTemplate != "~/action")
            {
                return null;
            }

            var actionSegment = odataPath.Segments.OfType<ActionPathSegment>().Single();
            var action = actionSegment.Action;

            if (action.IsBindable)
            {
                return null;
            }

            if (actionMap.Contains(action.Name) && actionMap[action.Name].Any(desc => MatchHttpMethod(desc, controllerContext.Request.Method)))
            {
                return action.Name;
            }

            return null;
        }

        private bool MatchHttpMethod(HttpActionDescriptor desc, HttpMethod method)
        {
            var supportedMethods = desc.ActionBinding.ActionDescriptor.SupportedHttpMethods;
            return supportedMethods.Contains(method);
        }
    }

}
