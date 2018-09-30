using UnityEngine;
using EAUnityLib.Generic;

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

