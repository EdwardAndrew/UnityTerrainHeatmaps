using UnityEngine;

namespace TerrainHeatmap
{
    [System.Serializable]
    public abstract class HeatmapSplatprototype : ScriptableObject
    {
        public float metallic;
        public float smoothness;
        public Vector2 tileOffset;
        public Vector2 tileSizing;
        public Texture2D normalMap;
        public string SplatMapType;
    }
}