// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System;
using System.IO;
using Bicep.Cli.Utils;
using Bicep.Core.Diagnostics;
using Bicep.Core.FileSystem;
using Bicep.Core.Parser;

namespace Bicep.Cli.FileSystem
{
    public class FileResolver : IFileResolver
    {
        public string GetNormalizedFileName(string fileName)
            => PathHelper.ResolvePath(fileName);

        public string? TryRead(string fileName, out string? failureMessage)
        {
            try
            {
                failureMessage = null;
                return File.ReadAllText(fileName);
            }
            catch (Exception exception)
            {
                // I/O classes typically throw a large variety of exceptions
                // instead of handling each one separately let's just trust the message we get

                failureMessage = exception.Message;
                return null;
            }
        }

        public string? TryResolveModulePath(string childFileName, string parentFileName)
        {
            if (Path.IsPathFullyQualified(childFileName))
            {
                return GetNormalizedFileName(childFileName);
            }

            var parentDirectoryName = Path.GetDirectoryName(parentFileName);
            if (parentDirectoryName == null)
            {
                return null;
            }

            return GetNormalizedFileName(Path.GetFullPath(childFileName, parentDirectoryName));
        }
    }
}

