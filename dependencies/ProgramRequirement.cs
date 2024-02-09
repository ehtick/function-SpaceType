using Elements;
using System;
using System.Linq;
using System.Collections.Generic;
using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{
    public partial class ProgramRequirement : IProgramRequirement
    {
        [JsonProperty("Default Wall Type")]
        public string DefaultWallType { get; internal set; }

        public string QualifiedProgramName => String.IsNullOrWhiteSpace(this.ProgramGroup) ? this.ProgramName : $"{this.ProgramGroup} - {this.ProgramName}";

        public bool Enclosed { get; set; }

        public (string? catalogPath, string? configPath) WriteLayoutConfigs(Model programReqModel)
        {
            if (!this.SpaceConfig.HasValue || !this.Catalog.HasValue)
            {
                return (null, null);
            }

            var tempDir = Path.GetTempPath();
            var catalogPath = Path.Combine(tempDir, $"{this.Id}_catalog.json");
            var configPath = Path.Combine(tempDir, $"{this.Id}_config.json");

            var catalogWrapper = programReqModel.GetElementOfType<CatalogWrapper>(this.Catalog.Value);
            var catalogStringBase64 = catalogWrapper.CatalogString;
            var bytes = Convert.FromBase64String(catalogStringBase64);
            if (bytes != null)
            {
                File.WriteAllBytes(catalogPath, bytes);
            }
            var spaceConfig = programReqModel.GetElementOfType<SpaceConfigurationElement>(this.SpaceConfig.Value);
            var config = spaceConfig.SpaceConfiguration;
            File.WriteAllText(configPath, JsonConvert.SerializeObject(config));

            return (catalogPath, configPath);
        }
    }
}