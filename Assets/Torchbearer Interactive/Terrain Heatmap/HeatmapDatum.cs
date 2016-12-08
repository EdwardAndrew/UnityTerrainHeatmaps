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
using TBUnityLib.Generic;

namespace TerrainHeatmap
{
    // Node for storing heatMapData.
    public class HeatmapDatum : ScriptableObject
    {

        public Vector3 nodePosition;
        public float value;
        public Pair<int> terrainCoords;
        public string filter;

        public void Init(Vector3 nodePos, float val, Pair<int> terrainCoords, string filter)
        {
            this.value = val;
            this.nodePosition = nodePos;
            this.terrainCoords = terrainCoords;
            this.filter = filter;
        }
        public void Init(Vector3 nodePos, float val, Pair<int> terrainCoords)
        {
            this.value = val;
            this.nodePosition = nodePos;
            this.terrainCoords = terrainCoords;
            this.filter = "";
        }

        public void Init(Vector3 nodePos, float val, Vector2 terrainCoords)
        {
            this.value = val;
            this.nodePosition = nodePos;
            this.terrainCoords = new Pair<int>((int)terrainCoords.x, (int)terrainCoords.y);
        }
    }
}

