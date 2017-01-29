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
