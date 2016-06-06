using NuGet.Server.WebAPI.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NuGet.Server.WebApi.SampleHost.Controllers
{
    public class NuGetPublicDownloadController : NuGetDownloadController
    {
        public NuGetPublicDownloadController()
            : base(Program.NuGetPublicRepository)
        {

        }
    }
}
