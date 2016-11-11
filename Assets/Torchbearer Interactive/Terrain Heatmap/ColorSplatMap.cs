//************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
// 
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// 
// Written by: Edward S Andrew - ed@tbinteractive.co.uk 2016
//************************************************************************
using UnityEngine;
using System.Collections;


namespace TerrainHeatmap
{
    public class ColorSplatMap : HeatmapSplatprototype
    {

        public Color color;

        public ColorSplatMap()
        {
            this.SplatMapType = "ColorSplatMap";
        }
    }
}
