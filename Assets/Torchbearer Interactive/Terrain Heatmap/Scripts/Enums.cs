//************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
// 
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// 
// Written by: Edward S Andrew - ed@tbinteractive.co.uk 2017
//************************************************************************
namespace TerrainHeatmap
{
    // Enum for the Interpolation Mode.
    public enum InterpolationMode
    {
        NearestNeighbor,
        Bilinear
    }


    // The data used to modify the textures.
    public enum HeatmapData
    {
        HeightMap,
        Custom
    }

    // What textures are going to be used to represent the data.
    public enum TextureSource
    {
        DefaultColors,
        Custom,
    }

    public enum HeatmapSplatPrototypeType
    {
        Color,
        Texture
    }
}