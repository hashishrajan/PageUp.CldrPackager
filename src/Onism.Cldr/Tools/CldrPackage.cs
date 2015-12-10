﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using Newtonsoft.Json;

namespace Onism.Cldr.Tools
{
    /// <summary>
    /// Represents one of the packages the CLDR data has been grouped into. This is a "smart enum" type.
    /// </summary>
    public abstract partial class CldrPackage
    {
        private const string Extension = ".cldrpkg";

        /// <summary>
        /// Gets or sets the name of this package.
        /// </summary>
        public string Name { get; protected set; }

        protected CldrPackage(string name)
        {
            Name = $"cldr-{name}";
        }

        internal abstract CldrJson TryParseFile(string path);

        /// <summary>
        /// Downloads this CLDR package from GitHub to a local directory.
        /// </summary>
        /// <param name="destinationDirectoryName">The path to the directory in which to place the extracted files.</param>
        public void Download(string destinationDirectoryName)
        {
            using (var client = new WebClient())
            {
                // download the information (zip)
                // and extract the data
                var uri = $"https://github.com/unicode-cldr/{Name}/archive/master.zip";
                var tempPath = Path.GetTempPath();
                var zipPath = Path.Combine(tempPath, $"{Name}.zip");
                var packageDirectoryName = Path.Combine(tempPath, $"{Name}");

                client.DownloadFile(uri, zipPath);
                ZipFile.ExtractToDirectory(zipPath, packageDirectoryName);
                File.Delete(zipPath);

                // parse the package
                var cldrJsons = CldrPackagePathExtractor
                    .ExtractPaths(packageDirectoryName)
                    .Select(TryParseFile)
                    .ToArray();

                // cleanup and serialization
                Directory.Delete(packageDirectoryName, true);
                var resultPath = Path.Combine(destinationDirectoryName, Name + Extension);
                var result = JsonConvert.SerializeObject(cldrJsons, Formatting.Indented);
                File.WriteAllText(resultPath, result);
            }
        }
    }
}
