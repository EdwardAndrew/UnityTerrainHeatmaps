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
    [System.Serializable]
    public class ColorSplatMap : HeatmapSplatprototype
    {

        public Color color;

        public ColorSplatMap()
        {
            this.SplatMapType = "ColorSplatMap";
        }
    }
}
