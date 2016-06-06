using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using NuGet.Server.DataServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Routing;
using System.Web.Http.Routing;

namespace NuGet.Server.WebAPI.OData
{
    static class Utilities
    {
        internal static void DecomposeKey(IHttpRouteData routeData)
        {
            var routeValues = routeData.Values;
            object value;

            if (!routeValues.TryGetValue(ODataRouteConstants.Key, out value)) return;

            var compoundKeyPairs = ((string)value).Split(',');

            if (!compoundKeyPairs.Any())
            {
                return;
            }

            var keyValues = compoundKeyPairs.Select(kv => kv.Split('=')).Select(kv => new KeyValuePair<string, object>(kv[0], kv[1]));
            foreach (var key in keyValues)
                routeValues.Add(key);
        }
    }
}
