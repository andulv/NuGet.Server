using NuGet.Server.WebAPI.OData.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;


namespace NuGet.Server.WebAPI.OData
{
    class NuGetODataControllerConfigurationAttribute : Attribute, IControllerConfiguration
    {
        public void Initialize(HttpControllerSettings controllerSettings, HttpControllerDescriptor controllerDescriptor)
        {
            var formatters = ODataMediaTypeFormatters.Create(new ODataPackageStreamAwareSerializerProvider(), new DefaultODataDeserializerProvider());
            controllerSettings.Formatters.Clear();
            controllerSettings.Formatters.InsertRange(0, formatters);
        }
    }
}
