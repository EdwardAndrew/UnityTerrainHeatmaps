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
    public class HeatmapNode : MonoBehaviour
    {

        [Tooltip("The value of this node.")]
        public float value = 0.0f;

        [Tooltip("The brush size of this node.")]
        public int brushSize = 10;

        [Tooltip("The brush hardness of this node, valid values range from 0-100.")]
        public float brushHardness = 0.0f;

        [Tooltip("The brush opacity of this node, valid values range from 0-100.")]
        public float brushOpacity = 100.0f;

        [Tooltip("If this is checked, then the values of this node will overwrite all other values.")]
        public bool overwriteOtherNodeValues = false;

        [Tooltip("If this is checked, the brush will be circular, otherwise the brush will be square.")]
        public bool circularBrush = true;

        [Tooltip("Make this Data node specific to a filter.")]
        public string filter = "";

        [HideInInspector]
        public Vector3 position;

        // To be executed only by the main thread, this method assigns the value of the transform.position to the position Vector3 variable.
        public void UpdatePosition()
        {
            position = this.transform.position;
        }
    }
}
