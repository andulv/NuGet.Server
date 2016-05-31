// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information. 

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Configuration;
using NuGet.Server.Logging;

namespace NuGet.Server.Infrastructure
{
    /// <summary>
    /// ServerPackageRepository represents a folder of nupkgs on disk. All packages are cached during the first request in order
    /// to correctly determine attributes such as IsAbsoluteLatestVersion. Adding, removing, or making changes to packages on disk 
    /// will clear the cache.
    /// </summary>
    public class ServerPackageRepository
        : ServerPackageRepositoryBase
    {
        public ServerPackageRepository(string path, IHashProvider hashProvider, Logging.ILogger logger=null)
            :base(path, hashProvider, GetBooleanAppSetting, logger)
        {
        }
        
        internal ServerPackageRepository(IFileSystem fileSystem, bool runBackgroundTasks, ExpandedPackageRepository innerRepository, Logging.ILogger logger = null, Func<string, bool, bool> getSetting = null) 
            : base(fileSystem,runBackgroundTasks,innerRepository, getSetting ?? GetBooleanAppSetting, logger)
        {
        }

        
        private static bool GetBooleanAppSetting(string key, bool defaultValue)
        {
            var appSettings = WebConfigurationManager.AppSettings;
            bool value;
            return !Boolean.TryParse(appSettings[key], out value) ? defaultValue : value;
        }
    }
}