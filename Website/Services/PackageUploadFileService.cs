﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NuGet;

namespace NuGetGallery
{
    public class PackageUploadFileService : IPackageUploadFileService
    {
        public void DeleteUploadedFile(User user)
        {
            throw new Exception();
        }

        public NuGet.ZipPackage GetUploadedFile(User user)
        {
            throw new Exception();
        }

        public void SaveUploadedFile(User user, IPackageMetadata package)
        {
            if (user == null)
                throw new ArgumentNullException("user");
            
            throw new Exception();
        }
    }
}