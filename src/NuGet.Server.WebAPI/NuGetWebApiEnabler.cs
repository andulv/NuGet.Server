using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using NuGet.Server.DataServices;
using NuGet.Server.WebAPI.OData.Conventions;
using NuGet.Server.WebAPI.OData.Serialization;
using NuGet.Server.WebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Dispatcher;
using System.Web.Http.OData;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Formatter;
using System.Web.Http.OData.Formatter.Deserialization;
using System.Web.Http.OData.Formatter.Serialization;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using System.Web.Http.Routing;
using System.Web.Http.OData.Builder;
using System.Web.Http.Controllers;
using System.Net.Http;
using NuGet.Server.Infrastructure;
using NuGet.Server.Logging;
using System.Net;

namespace NuGet.Server.WebAPI
{
    public static class NuGetWebApiEnabler
    {
        public static HttpConfiguration UseNuGetWebApiFeed(this HttpConfiguration config,
            string routeName,
            string routeUrlRoot, 
            string oDatacontrollerName,
            string downloadControllerName)
        {
            var conventions = new List<IODataRoutingConvention>
            {
                new CompositeKeyRoutingConvention(oDatacontrollerName),
                new CompositeKeyPropertyRoutingConvention(),
                new NonBindableActionRoutingConvention(oDatacontrollerName),
                new EntitySetCountRoutingConvention(),
            };
            conventions.AddRange(ODataRoutingConventions.CreateDefault());
            var oDataModel = BuildNuGetODataModel();
            config.Routes.MapODataServiceRoute(routeName, routeUrlRoot, oDataModel, new DefaultODataPathHandler(), conventions);

            var downloadRouteName = routeName + "_download";

            ODataPackageStreamAwareEntityTypeSerializer.RegisterDownloadLinkProvider(oDataModel, new DefaultDownloadLinkProvider(downloadRouteName));

            config.Routes.MapHttpRoute(downloadRouteName, routeUrlRoot + "/PackagesDownload(Id='{id}',Version='{version}')",
                new { controller = downloadControllerName, action = "DownloadPackage", version = RouteParameter.Optional });

            return config;
        }

        public static IServerPackageRepository CreatePackageRepository(string packagePath, Func<string, bool, bool> getSetting)
        {
            var hashProvider = new CryptoHashProvider(Constants.HashAlgorithm);
            return new ServerPackageRepositoryBase(packagePath, hashProvider, getSetting, new ConsoleLogger());          
        }

        internal static IEdmModel BuildNuGetODataModel()
        {
            var builder = new ODataConventionModelBuilder();
            var entity = builder.EntitySet<ODataPackage>("Packages");
            entity.EntityType.HasKey(pkg => pkg.Id);
            entity.EntityType.HasKey(pkg => pkg.Version);

            var searchAction = builder.Action("Search");
            searchAction.Parameter<string>("searchTerm");
            searchAction.Parameter<string>("targetFramework");
            searchAction.Parameter<bool>("includePrerelease");
            searchAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            var findPackagesAction = builder.Action("FindPackagesById");
            findPackagesAction.Parameter<string>("id");
            findPackagesAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            var getUpdatesAction = builder.Action("GetUpdates");
            getUpdatesAction.Parameter<string>("packageIds");
            getUpdatesAction.Parameter<bool>("includePrerelease");
            getUpdatesAction.Parameter<bool>("includeAllVersions");
            getUpdatesAction.Parameter<string>("targetFrameworks");
            getUpdatesAction.Parameter<string>("versionConstraints");
            getUpdatesAction.ReturnsCollectionFromEntitySet<ODataPackage>("Packages");

            var retValue = builder.GetEdmModel();
            retValue.SetHasDefaultStream(retValue.FindDeclaredType(typeof(ODataPackage).FullName) as IEdmEntityType, hasStream: true);
            return retValue;
        }

    }

    interface IDownloadLinkProvider
    {
        Uri GetDownloadUrl(ODataPackage package, EntityInstanceContext context);
    }

    class DefaultDownloadLinkProvider : IDownloadLinkProvider
    {
        string _downloadRouteName;

        public DefaultDownloadLinkProvider(string downloadRouteName)
        {
            _downloadRouteName = downloadRouteName;
        }

        public Uri GetDownloadUrl(ODataPackage package, EntityInstanceContext context)
        {
            var url = new UrlHelper(context.Request);
            var routeParams = new { package.Id, package.Version };
            var downloadLink = url.Link(_downloadRouteName, routeParams);
            return new Uri(downloadLink, UriKind.Absolute);
        }
    }
}
