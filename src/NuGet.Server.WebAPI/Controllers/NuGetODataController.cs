using NuGet.Server.DataServices;
using NuGet.Server.Infrastructure;
using NuGet.Server.WebAPI.OData;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.OData;
using System.Web.Http.OData.Query;

namespace NuGet.Server.WebAPI.Controllers
{
    [NuGetODataControllerConfiguration]
    public abstract class NuGetODataController : ODataController
    {
        private const int DefaultSearchPageSize = 30;

        static readonly ODataQuerySettings SearchQuerySettings = new ODataQuerySettings
        {
            HandleNullPropagation = HandleNullPropagationOption.False,
            EnsureStableOrdering = true
        };

        readonly IServerPackageRepository _repository;

        public NuGetODataController(IServerPackageRepository repository)
        {
            _repository = repository;
        }

        [HttpGet]
        [HttpPost]
        [EnableQuery(PageSize = 100, HandleNullPropagation = HandleNullPropagationOption.False)]
        public IQueryable<ODataPackage> Get()
        {
            var queryable = _repository.GetPackages();

            var retValue = TransformPackages(queryable);
            return retValue;
        }

        [HttpGet]
        public ODataPackage Get([FromODataUri] string id, [FromODataUri] string version)
        {
            var semVersion = new SemanticVersion(version);
            var package = _repository.FindPackage(id, semVersion);
            if (package == null)
                throw new HttpResponseException(HttpStatusCode.NotFound);

            return package.AsODataPackage();
        }



        [HttpGet]
        [EnableQuery(PageSize = 100, HandleNullPropagation = HandleNullPropagationOption.False)]
        public IQueryable<ODataPackage> Search([FromODataUri] string searchTerm, [FromODataUri] string targetFramework, [FromODataUri] bool includePrerelease)
        {
            var targetFrameworks = String.IsNullOrEmpty(targetFramework) ? Enumerable.Empty<string>() : targetFramework.Split('|');

            return _repository
                .Search(searchTerm, targetFrameworks, includePrerelease)
                .Select(package => package.AsODataPackage())
                .AsQueryable()
                .InterceptWith(new NormalizeVersionInterceptor());
        }

        [HttpGet]
        [EnableQuery(PageSize = 100, HandleNullPropagation = HandleNullPropagationOption.False)]
        public IEnumerable<ODataPackage> FindPackagesById([FromODataUri] string id)
        {
            var packages = _repository.FindPackagesById(id);
            var retValue = TransformPackages(packages);

            return retValue;
        }


        [HttpPost]
        [HttpGet]
        [EnableQuery(PageSize = 100, HandleNullPropagation = HandleNullPropagationOption.False)]
        public IQueryable<ODataPackage> GetUpdates(
            [FromODataUri] string packageIds,
            [FromODataUri] string versions,
            [FromODataUri] bool includePrerelease,
            [FromODataUri] bool includeAllVersions,
            [FromODataUri] string targetFrameworks,
            [FromODataUri] string versionConstraints)
        {
            if (String.IsNullOrEmpty(packageIds) || String.IsNullOrEmpty(versions))
            {
                return Enumerable.Empty<ODataPackage>().AsQueryable();
            }

            var idValues = packageIds.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var versionValues = versions.Trim().Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var targetFrameworkValues = String.IsNullOrEmpty(targetFrameworks) ? null :
                                                                                 targetFrameworks.Split('|').Select(VersionUtility.ParseFrameworkName).ToList();
            var versionConstraintValues = String.IsNullOrEmpty(versionConstraints)
                                            ? new string[idValues.Length]
                                            : versionConstraints.Split('|');

            if (idValues.Length == 0 || idValues.Length != versionValues.Length || idValues.Length != versionConstraintValues.Length)
            {
                // Exit early if the request looks invalid
                return Enumerable.Empty<ODataPackage>().AsQueryable();
            }

            var packagesToUpdate = new List<IPackageMetadata>();
            for (var i = 0; i < idValues.Length; i++)
            {
                packagesToUpdate.Add(new PackageBuilder { Id = idValues[i], Version = new SemanticVersion(versionValues[i]) });
            }

            var versionConstraintsList = new IVersionSpec[versionConstraintValues.Length];
            for (var i = 0; i < versionConstraintsList.Length; i++)
            {
                if (!String.IsNullOrEmpty(versionConstraintValues[i]))
                {
                    VersionUtility.TryParseVersionSpec(versionConstraintValues[i], out versionConstraintsList[i]);
                }
            }

            return _repository
                .GetUpdatesCore(packagesToUpdate, includePrerelease, includeAllVersions, targetFrameworkValues, versionConstraintsList)
                .Select(package => package.AsODataPackage())
                .AsQueryable()
                .InterceptWith(new NormalizeVersionInterceptor());
        }

        IQueryable<ODataPackage> TransformPackages(IEnumerable<IPackage> packages)
        {
            var retValue = packages.Select(x => x.AsODataPackage()).AsQueryable();
            return retValue;
        }

    }
}
