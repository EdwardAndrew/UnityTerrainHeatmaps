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
using TBUnityLib.Generic;

namespace TerrainHeatmap
{
    /// <summary>
    /// Heatmap data point,for storing the heatmap data.
    /// </summary>
    public class HeatmapDatum
    {

        public Vector3 nodePosition;
        public float value;
        public Pair<int> terrainCoords;

        public void Init(Vector3 nodePos, float val, Pair<int> terrainCoords)
        {
            this.value = val;
            this.nodePosition = nodePos;
            this.terrainCoords = terrainCoords;
        }

        public void Init(Vector3 nodePos, float val, Vector2 terrainCoords)
        {
            this.value = val;
            this.nodePosition = nodePos;
            this.terrainCoords = new Pair<int>((int)terrainCoords.x, (int)terrainCoords.y);
        }
    }
}

