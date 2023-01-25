using System.Collections.Generic;

namespace VTS_NDIToArtMesh
{
    public class NDIToArtMeshConfig
    {
        public string NDIName;
        public bool HorizontalFlip = true;
        public bool VerticalFlip;
        public List<string> ArtMeshNames = new List<string>();
    }
}
