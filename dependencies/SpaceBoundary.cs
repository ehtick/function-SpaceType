using Elements.Geometry;
using Newtonsoft.Json;

namespace Elements
{
    public partial class SpaceBoundary : ISpaceBoundary
    {
        public Vector3? ParentCentroid { get; set; }

        [JsonProperty("Config Id")]
        public string ConfigId { get; set; }
    }
}