using Elements.Geometry;

namespace Elements
{
    public partial class SpaceBoundary : ISpaceBoundary
    {
        public Vector3? ParentCentroid { get; set; }
        
        public string ConfigId { get; set; }
    }
}