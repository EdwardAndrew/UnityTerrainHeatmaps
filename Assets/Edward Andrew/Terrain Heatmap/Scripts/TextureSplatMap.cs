using UnityEngine;

namespace TerrainHeatmap
{
    /// <summary>
    /// A heatmap splatmap that uses a Texture.
    /// </summary>
    [System.Serializable]
    public class TextureSplatMap : HeatmapSplatprototype
    {
        /// <summary>
        /// The texture of the TextureSplatMap.
        /// </summary>
        public Texture2D texture;

        /// <summary>
        /// Create a new TextureSplatMap.
        /// </summary>
        public TextureSplatMap()
        {
            this.SplatMapType = "TextureSplatMap";
        }
    }
}
