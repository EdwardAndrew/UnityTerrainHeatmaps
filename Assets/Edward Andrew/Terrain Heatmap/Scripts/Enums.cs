namespace TerrainHeatmap
{
    /// <summary>
    /// The Interpolation method used for the heatmap.
    /// </summary>
    public enum InterpolationMode
    {
        NearestNeighbor,
        Bilinear
    }


    /// <summary>
    /// The data source for the heatmap.
    /// </summary>
    public enum HeatmapData
    {
        HeightMap,
        Custom
    }

    /// <summary>
    /// The heatmap's splatprototype texture source.
    /// </summary>
    public enum TextureSource
    {
        DefaultColors,
        Custom,
    }
}