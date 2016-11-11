﻿//************************************************************************
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
    public abstract class HeatmapSplatprototype
    {
        public float metallic;
        public float smoothness;
        public Vector2 tileOffset;
        public Vector2 tileSizing;
        public Texture2D normalMap;

        public string SplatMapType;
    }
}