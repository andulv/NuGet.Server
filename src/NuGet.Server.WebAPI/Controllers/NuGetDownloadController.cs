using NuGet;
using NuGet.Server.DataServices;
using NuGet.Server.Infrastructure;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace NuGet.Server.WebAPI.Controllers
{
    public abstract class NuGetDownloadController : ApiController
    {
        //- Download fra OSCServer + "/pos/choco/Packages
        //Modifisere install.ps1:"
        //- Ta OSCServer, OSCUser, OSCPassword som argumenter
        //- Kjør choco source add -name oscpos -source %oscserver/pos/choco -u %oscuser -p %oscpassword

        readonly IServerPackageRepository _repository;

        public NuGetDownloadController(IServerPackageRepository repository)
        {
            _repository = repository;
        }

        [HttpGet, HttpHead]
        public HttpResponseMessage DownloadPackage(string id, string version = "")
        {
            IPackage requestedPackage = string.IsNullOrEmpty(version) ?
                                        _repository.FindPackage(id) :
                                        _repository.FindPackage(id, new SemanticVersion(version));

            if (requestedPackage == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, string.Format("'Package {0} {1}' Not found.", id, version));

            //var result = EvaluateCacheHeaders(packageSpec, package, dsPackage);

            //if (result != null)
            //{
            //    return result;
            //}

            var result = Request.CreateResponse(HttpStatusCode.OK);
            var serverPackage = requestedPackage as ServerPackage;

            if (Request.Method == HttpMethod.Get)
            {
                if (serverPackage != null)
                    result.Content = new StreamContent(File.OpenRead(serverPackage.FullPath));
                else
                    result.Content = new StreamContent(requestedPackage.GetStream());
            }
            else
            {
                result.Content = new StringContent(string.Empty);
            }


            result.Content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("binary/octet-stream");
            if (serverPackage != null)
            {
                result.Content.Headers.LastModified = serverPackage.LastUpdated;
                result.Headers.ETag = new EntityTagHeaderValue('"' + serverPackage.PackageHash + '"');
            }

            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue(DispositionTypeNames.Attachment)
            {
                FileName = string.Format("{0}.{1}{2}", requestedPackage.Id, requestedPackage.Version, NuGet.Constants.PackageExtension),
                Size = serverPackage != null ? (long?)serverPackage.PackageSize : null,
                CreationDate = requestedPackage.Published,
                ModificationDate = result.Content.Headers.LastModified,
            };

            return result;
        }

        //private HttpResponseMessage EvaluateCacheHeaders(IPackage package, ODataPackage dsPackage)
        //{
        //    if (package == null)
        //    {
        //        return Request.CreateErrorResponse(HttpStatusCode.NotFound,
        //                                             string.Format("Package {0} version {1} not found.", packageSpec.Id,
        //                                                           packageSpec.Version));
        //    }

        //    var etagMatch = Request.Headers.IfMatch.Any(etag => !etag.IsWeak && etag.Tag == '"' + dsPackage.PackageHash + '"');
        //    var notModifiedSince = Request.Headers.IfModifiedSince.HasValue &&
        //                           Request.Headers.IfModifiedSince >= dsPackage.LastUpdated;

        //    if (etagMatch || notModifiedSince)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.NotModified);
        //    }

        //    return null;
        //}

        //private IPackage FindPackage(PackageSpec packageSpec)
        //{
        //    IPackage foundPackage = null;
        //    if (packageSpec.Version == null)
        //        foundPackage = FindNewestReleasePackage(packageSpec.Id);
        //    else
        //        foundPackage = _repository.FindPackage(packageSpec.Id, packageSpec.Version);

        //    return foundPackage;

        //}

        //private IPackage FindNewestReleasePackage(string packageId)
        //{
        //    return _repository
        //            .FindPackagesById(packageId)
        //            .Where(p => p.IsReleaseVersion())
        //            .OrderByDescending(p => p.Version)
        //            .FirstOrDefault();
        //}
    }
}
