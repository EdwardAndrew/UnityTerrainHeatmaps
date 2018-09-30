using UnityEngine;

namespace TerrainHeatmap
{
    /// <summary>
    /// A Heatmap splatmap that uses a color as it's texture instead of an image.
    /// </summary>
    [System.Serializable]
    public class ColorSplatMap : HeatmapSplatprototype
    {
        /// <summary>
        /// The color of the ColorSplatMap.
        /// </summary>
        public Color color;

        /// <summary>
        /// Create a new ColorSplatMap.
        /// </summary>
        public ColorSplatMap()
        {
            this.SplatMapType = "ColorSplatMap";
        }
    }
}
