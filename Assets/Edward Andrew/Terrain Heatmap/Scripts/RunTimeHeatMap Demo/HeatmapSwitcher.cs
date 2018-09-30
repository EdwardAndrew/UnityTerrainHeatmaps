using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TerrainHeatmap;

public class HeatmapSwitcher : MonoBehaviour {

    HeatmapController heatmap;

    public KeyCode toggleHeatmap = KeyCode.A;
    public KeyCode switchToHeightMap = KeyCode.B;
    public KeyCode switchToCustomMap = KeyCode.C;
    public KeyCode switchToCustomColors = KeyCode.D;

    // Use this for initialization
    void Start () {
        heatmap = GameObject.Find("Terrain Heatmap").GetComponent<HeatmapController>();
	}
	
	// Update is called once per frame
	void Update () {

        if (Input.GetKeyDown(toggleHeatmap)) heatmap.DisplayHeatmap = !heatmap.DisplayHeatmap;

        if (Input.GetKeyDown(switchToHeightMap)) heatmap.SwitchToHeatmap("Height Map");

        if (Input.GetKeyDown(switchToCustomMap)) heatmap.SwitchToHeatmap("Custom Map");

        if (Input.GetKeyDown(switchToCustomColors)) heatmap.SwitchToHeatmap("Custom Colors");
    }
}
