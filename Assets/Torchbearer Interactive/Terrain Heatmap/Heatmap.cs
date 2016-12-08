using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace TerrainHeatmap
{
    [System.Serializable]
    public class Heatmap
    {
        public int heatmapResolution;
        public bool displayFlippedConstraints;
        public bool autoConstrain;
        public float[,,] alphaMapData;
        public float[,] visualisationValueMap;
        public float lowerValueThreshold;
        public float upperValueThreshold;
        public float coldHotValueOffset;
        public string name;
        public TextureSource texSource;
        public SplatPrototype[] splatPrototypes;
        public InterpolationMode interpolationMode;
        public HeatmapDatum[,] mapData;
        public HeatmapData dataType;
        [SerializeField]
        public List<HeatmapSplatprototype> dataVisualisaitonSplatMaps;
        public bool splatPrototypesUpdated;
        public float baseValue;
        public string filter;

        public Heatmap(string _name)
        {
            heatmapResolution = 64;
            displayFlippedConstraints = false;
            autoConstrain = false;
            alphaMapData = null;
            visualisationValueMap = null;
            lowerValueThreshold = 0;
            upperValueThreshold = 100;
            coldHotValueOffset = 0;
            name = _name;
            texSource = TextureSource.DefaultColors;
            splatPrototypes = null;
            interpolationMode = InterpolationMode.NearestNeighbor;
            mapData = null;
            dataType = HeatmapData.HeightMap;
            dataVisualisaitonSplatMaps = new List<HeatmapSplatprototype>();
            splatPrototypesUpdated = false;
            baseValue = 0.0f;
            filter = "";

        }

        public Heatmap()
        {
        }
    }
}
