using UnityEngine;
using System.Collections.Generic;


namespace TerrainHeatmap
{

    [System.Serializable]
    public class Heatmap
    {
        public int heatmapResolution;
        public bool flipAutoConstrain;
        public bool autoConstrain;
        public float[,,] alphaMapData;
        public float[,] heatmapValues;
        public float lowerValueLimit;
        public float higherValueLimit;
        public string name;
        public TextureSource texSource;
        public SplatPrototype[] splatPrototypes;
        public InterpolationMode interpolationMode;       
        public HeatmapDatum[,] heatmapDataPoints;
        public HeatmapData dataType;
        public List<HeatmapSplatprototype> heatmapSplatMaps;
        public float baseValue;
        public string filter;

        public Heatmap(string _name)
        {
            heatmapResolution = 64;
            flipAutoConstrain = false;
            autoConstrain = false;
            alphaMapData = null;
            heatmapValues = null;
            lowerValueLimit = 0;
            higherValueLimit = 100;
            name = _name;
            texSource = TextureSource.DefaultColors;
            splatPrototypes = null;
            interpolationMode = InterpolationMode.NearestNeighbor;
            heatmapDataPoints = null;
            dataType = HeatmapData.HeightMap;
            heatmapSplatMaps = new List<HeatmapSplatprototype>();
            baseValue = 0.0f;
            filter = "";

        }
    }
}
