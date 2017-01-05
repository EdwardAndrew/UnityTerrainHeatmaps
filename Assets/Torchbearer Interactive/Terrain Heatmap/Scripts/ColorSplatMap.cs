//************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
// 
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// 
// Written by: Edward S Andrew - ed@tbinteractive.co.uk 2017
//************************************************************************
using UnityEngine;
using System.Collections;


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
