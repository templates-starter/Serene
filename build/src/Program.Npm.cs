﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Build
{
    partial class Program
    {
        public static string GetLatestNpmPackageVersion(string packageName)
        {
            using var httpClient = new HttpClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://registry.npmjs.org/{packageName}");
            using var registryContent = httpClient.Send(request).Content.ReadAsStream();
            using var sr = new StreamReader(registryContent);
            return JObject.Parse(sr.ReadToEnd())["dist-tags"]?["latest"]?.Value<string>();
        }

        public static void PatchPackageJsonCopy(string packageJsonCopy)
        {
            if (!File.Exists(packageJsonCopy))
                return;

            var content = File.ReadAllText(PackageJsonFile);
            var root = JObject.Parse(content);

            var dependencies = (JObject)root["dependencies"];
            var devDependencies = (JObject)root["devDependencies"];
            devDependencies["@serenity-is/tsbuild"] = GetLatestNpmPackageVersion("@serenity-is/tsbuild");
            dependencies["@serenity-is/sleekgrid"] = GetLatestNpmPackageVersion("@serenity-is/sleekgrid");
            File.WriteAllText(PackageJsonFile, root.ToString().Replace("\r", ""));
    
            dependencies["@serenity-is/corelib"] = GetLatestNpmPackageVersion("@serenity-is/corelib");
            content = root.ToString().Replace("\r", "");
            File.WriteAllText(packageJsonCopy, content);

            var packageLockCopy = Path.ChangeExtension(packageJsonCopy, null) + "-lock.json";
            if (File.Exists(packageLockCopy))
            {
                File.Delete(packageLockCopy);
                if (StartProcess("cmd", "/c npm i", Path.GetDirectoryName(packageJsonCopy)) != 0)
                {
                    Console.Error.WriteLine("Error while npm install at " + Path.GetDirectoryName(packageJsonCopy));
                    Environment.Exit(1);
                }

                Directory.Delete(Path.Combine(Path.GetDirectoryName(packageJsonCopy), "node_modules"), recursive: true);
            }
        }
    }
}