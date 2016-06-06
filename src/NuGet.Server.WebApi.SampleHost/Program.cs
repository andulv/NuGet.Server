using Microsoft.Owin.Hosting;
using NuGet.Server.Infrastructure;
using NuGet.Server.WebAPI;
using Owin;
using System;
using System.Collections.Generic;
using System.Data.Services;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Web.Http;
using System.Web.Http.OData.Extensions;

namespace NuGet.Server.WebApi.SampleHost
{
    class Program
    {
        internal static IServerPackageRepository NuGetPublicRepository { get; private set; }
        internal static IServerPackageRepository NuGetPrivateRepository { get; private set; }

        static void Main(string[] args)
        {
            var baseAddress = "http://localhost:9000/";

            //The repository needs to read settings from a callback function.
            //Fortunately, the caller provides a default value. So if the setting is unknown / not set we just return the default value.
            var settings = new Dictionary<string, bool>();
            settings.Add("enableDelisting", false);
            settings.Add("enableFrameworkFiltering", false);
            Func<string, bool, bool> getSettings = (key, defaultValue) =>
            {
                System.Diagnostics.Debug.WriteLine("getSetting: " + key);
                return settings.ContainsKey(key) ? settings[key] : defaultValue;
            };

            //Sets up two repositories, one for each feed we use.
            NuGetPublicRepository = NuGetWebApiEnabler.CreatePackageRepository(@"d:\omnishopcentraldata\Packages\Public\", getSettings);
            NuGetPrivateRepository = NuGetWebApiEnabler.CreatePackageRepository(@"d:\omnishopcentraldata\Packages\Private\", getSettings);

            // Start OWIN host, which in turn will create a new instance of Startup class, and execute its Configuration method.
            using (WebApp.Start<Startup>(url: baseAddress))
            {
                Console.WriteLine("Server listening at baseaddress: " + baseAddress);
                Console.WriteLine("[ENTER] to close server");
                Console.ReadLine();
            }
        }
    }

    public class Startup
    {
        public void Configuration(IAppBuilder appBuilder)
        {
            // Configure Web API for self-host. 
            HttpConfiguration config = new HttpConfiguration();
            appBuilder.UseWebApi(config);

            //Map route for ordinary controllers, this is not neccessary for the NuGet feed.
            //It is just included as an example of combining ordinary controllers with NuGet OData Controllers.
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );

            NuGetWebApiEnabler.UseNuGetWebApiFeed(config,
                routeName : "NuGetPublic", 
                routeUrlRoot : "NuGet/public",
                oDatacontrollerName: "NuGetPublicOData",            //NuGetPublicODataController.Cs, located in Controllers\ folders
                downloadControllerName: "NuGetPublicDownload");     //NuGetPublicDownloadController.Cs, located in Controllers\ folders

            NuGetWebApiEnabler.UseNuGetWebApiFeed(config,
                routeName: "NuGetPublic",
                routeUrlRoot: "NuGet/public",
                oDatacontrollerName: "NuGetPublicOData",            //NuGetPublicODataController.Cs, located in Controllers\ folders
                downloadControllerName: "NuGetPublicDownload");     //NuGetPublicDownloadController.Cs, located in Controllers\ folders

            NuGetWebApiEnabler.UseNuGetWebApiFeed(config, "NuGetAdmin", "NuGet/admin", "NuGetPrivateOData", "NuGetPrivateDownload");
        }
    }
}
