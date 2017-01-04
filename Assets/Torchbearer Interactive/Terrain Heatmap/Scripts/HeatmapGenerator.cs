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
using System.Collections.Generic;
using System.Threading;
using TBUnityLib.TextureTools;
using TBUnityLib.TerrainTools;
using TBUnityLib.Generic;

namespace TerrainHeatmap
{
    /// <summary>
    /// Generates Heatmaps.
    /// </summary>
    public class HeatmapGenerator
    {

        Thread _generateHeatMapThread;
        public bool isGenerateHeatMapThreadFinished = true;

        int _selectedTextureIndex;
        List<Heatmap> _heatmaps;
        int _alphaMapResolution;
        Vector3 _terrainObjectSize;
        public int terrainX = -9999;
        public int terrainY = -9999;
        int _width;
        int _height;
        float[,] _heightMap;
        Vector3 _heightMapScale;
        public float[,,] processedAlphaMap;
        float[,,] _originalAlphaMap;
        HeatmapNode[] _customData;
        Vector3 _positionOffset;

        /// <summary>
        /// Aborts the thread.
        /// </summary>
        public void Abort()
        {
            if (_generateHeatMapThread == null) return;
            _generateHeatMapThread.Abort();
        }

        /// <summary>
        /// Method executed by the _genereateHeatmapThread.
        /// </summary>
        public void GenerateHeatMapChunkThreaded()
        {
            GenerateHeatMap(_selectedTextureIndex, ref _heatmaps, _alphaMapResolution, _terrainObjectSize, terrainX, terrainY, _width, _height, _heightMap, _heightMapScale, false, _customData, _positionOffset);
            processedAlphaMap = Chunks.GetAlphaMapChunk(_originalAlphaMap, terrainY, terrainX, _width, _height);
            isGenerateHeatMapThreadFinished = true;
        }

        /// <summary>
        /// Begins the thread processing the current Heatmap job.
        /// </summary>
        /// <param name="selectedTextureIndex"></param>
        /// <param name="heatmaps"></param>
        /// <param name="heightMap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="terrainObjectSize"></param>
        /// <param name="heightMapScale"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="customData"></param>
        /// <param name="positionOffset"></param>
        public void ProcessHeatMapThreaded(int selectedTextureIndex, ref List<Heatmap> heatmaps, float[,] heightMap, int alphaMapResolution, Vector3 terrainObjectSize, Vector3 heightMapScale, int x, int y, int width, int height, HeatmapNode[] customData, Vector3 positionOffset)
        {
            isGenerateHeatMapThreadFinished = false;

            _selectedTextureIndex = selectedTextureIndex;
            _heatmaps = heatmaps;
            _heightMap = heightMap;
            _alphaMapResolution = alphaMapResolution;
            terrainX = x;
            terrainY = y;
            _width = width;
            _height = height;
            _heightMapScale = heightMapScale;
            _terrainObjectSize = terrainObjectSize;
            _originalAlphaMap = heatmaps[selectedTextureIndex].alphaMapData;
            _customData = customData;
            _positionOffset = positionOffset;

            _generateHeatMapThread = new Thread(GenerateHeatMapChunkThreaded);
            _generateHeatMapThread.Priority = System.Threading.ThreadPriority.Highest;
            _generateHeatMapThread.Start();
        }


        /// <summary>
        /// Overload to generate a heatmap with fewer parameters.
        /// </summary>
        /// <param name="selectedTextureIndex"></param>
        /// <param name="heatmaps"></param>
        /// <param name="terrain"></param>
        /// <param name="customData"></param>
        /// <param name="positionOffset"></param>
        public void GenerateHeatMap(int selectedTextureIndex, ref List<Heatmap> heatmaps, Terrain terrain, HeatmapNode[] customData, Vector3 positionOffset)
        {
            if (terrain == null)
            {
                return;
            }
            if (terrain.terrainData == null) return;
            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            GenerateHeatMap(selectedTextureIndex, ref heatmaps, terrain.terrainData.alphamapResolution, terrain.terrainData.size, 0, 0, terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution, heights, terrain.terrainData.heightmapScale, true, customData, positionOffset);
        }

        /// <summary>
        /// Generates a Heatmap.
        /// </summary>
        /// <param name="selectedTextureIndex"></param>
        /// <param name="heatmaps"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="terrainObjectSize"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="heightMap"></param>
        /// <param name="heightMapScale"></param>
        /// <param name="splatMapUpdateOverride"></param>
        /// <param name="customData"></param>
        /// <param name="positionOffset"></param>
        public void GenerateHeatMap(int selectedTextureIndex, ref List<Heatmap> heatmaps, int alphaMapResolution, Vector3 terrainObjectSize, int x, int y, int width, int height, float[,] heightMap, Vector3 heightMapScale, bool splatMapUpdateOverride, HeatmapNode[] customData, Vector3 positionOffset)
        {

            Heatmap selectedHeatmap;

            //Attempt to store the selected Visualisation Texture into a local variable.
            if (heatmaps == null)
            {
                return;
            }
            else
            {
                selectedHeatmap = heatmaps[selectedTextureIndex];
            }

            float originalUpperValueThreshold = selectedHeatmap.upperValueThreshold;
            float originalLowerValueThreshold = selectedHeatmap.lowerValueThreshold;

            if ((selectedHeatmap.texSource == TextureSource.Custom) && (selectedHeatmap.dataVisualisaitonSplatMaps.Count <= 0))
            {
                return;
            }

            if (selectedHeatmap.displayFlippedConstraints) selectedHeatmap = this.FlipHeatmapConstraints(selectedTextureIndex, ref heatmaps);

            // Generate splatPrototypes for heatMap.
            if (!splatMapUpdateOverride)
            {
                if (selectedHeatmap.splatPrototypes == null || selectedHeatmap.splatPrototypesUpdated)
                {
                    if (!isGenerateHeatMapThreadFinished) return;
                    selectedHeatmap.splatPrototypes = GenerateSplatPrototypes(selectedHeatmap);
                }
            }
            else
            {
                selectedHeatmap.splatPrototypes = GenerateSplatPrototypes(selectedHeatmap);
                selectedHeatmap.alphaMapData = new float[alphaMapResolution, alphaMapResolution, selectedHeatmap.splatPrototypes.Length];
            }

            if (!selectedHeatmap.displayFlippedConstraints) selectedHeatmap.coldHotValueOffset = CalculateLowerUpperThresholdValueOffset(selectedHeatmap.lowerValueThreshold);

            // Create array that will be the heatmap splatPrototypes information.
            if (selectedHeatmap.alphaMapData == null)
            {
                selectedHeatmap.alphaMapData = new float[alphaMapResolution, alphaMapResolution, selectedHeatmap.splatPrototypes.Length];
            }

            // Create Value Map.
            selectedHeatmap.visualisationValueMap = new float[alphaMapResolution, alphaMapResolution];

            // Load Genereated heatMap data into Array.
            AssignMapData(selectedHeatmap, alphaMapResolution, terrainObjectSize, heightMap, heightMapScale, customData, positionOffset);

            if (selectedHeatmap.autoConstrain) AutoConstrainValues(ref selectedHeatmap, selectedHeatmap.displayFlippedConstraints);
            if (selectedHeatmap.displayFlippedConstraints) selectedHeatmap.coldHotValueOffset = CalculateLowerUpperThresholdValueOffset(selectedHeatmap.lowerValueThreshold);


            // Interpolate the data on the Heat value Map.
            selectedHeatmap.visualisationValueMap = InterpolateColorTextureValues(ref selectedHeatmap, alphaMapResolution);

            // Assignt the correct HeatMap Color value to each pixel on the Terrain.
            AssignVisualisationTextureColorValue(ref selectedHeatmap, x, y, width, height);

            selectedHeatmap.lowerValueThreshold = originalLowerValueThreshold;
            selectedHeatmap.upperValueThreshold = originalUpperValueThreshold;

        }

        /// <summary>
        /// Finds the highest and lowest values and uses them as the constraints.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="flipConstraints"></param>
        void AutoConstrainValues(ref Heatmap heatmap, bool flipConstraints)
        {
            float upperValue = heatmap.mapData[0, 0].value;
            float lowerValue = heatmap.mapData[0, 0].value;

            foreach (HeatmapDatum datum in heatmap.mapData)
            {
                if (datum.value > upperValue) upperValue = datum.value;
                if (datum.value < lowerValue) lowerValue = datum.value;
            }

            if (!flipConstraints)
            {

                heatmap.lowerValueThreshold = lowerValue + 1;
                heatmap.upperValueThreshold = upperValue - 1;
            }
            else
            {
                heatmap.upperValueThreshold = lowerValue + 1;
                heatmap.lowerValueThreshold = upperValue - 1;
            }

        }

        // Generate Splat Prototypes for use as SplatMap Textures.
        SplatPrototype[] GenerateSplatPrototypesDefault()
        {
            // Create list to store splatPrototypes.
            List<SplatPrototype> heatMapColours = new List<SplatPrototype>();


            // Load the red colour into SplatProtoype.
            var red = new SplatPrototype();
            red.texture = TextureGenerator.GenerateTexture2D(Color.red);
            red.tileSize = new Vector2(10.0f, 10.0f);
            red.tileOffset = Vector2.zero;

            // Load the yellow colour into SplatProtoype.
            var yellow = new SplatPrototype();
            yellow.texture = TextureGenerator.GenerateTexture2D(Color.yellow);
            yellow.tileSize = new Vector2(10.0f, 10.0f);
            yellow.tileOffset = Vector2.zero;

            // Load the green colour into SplatProtoype.
            var green = new SplatPrototype();
            green.texture = TextureGenerator.GenerateTexture2D(Color.green);
            green.tileSize = new Vector2(10.0f, 10.0f);
            green.tileOffset = Vector2.zero;

            // Load the cyan colour into SplatProtoype.
            var cyan = new SplatPrototype();
            cyan.texture = TextureGenerator.GenerateTexture2D(Color.cyan);
            cyan.tileSize = new Vector2(10.0f, 10.0f);
            cyan.tileOffset = Vector2.zero;

            // Load the blue colour into SplatProtoype.
            var blue = new SplatPrototype();
            blue.texture = TextureGenerator.GenerateTexture2D(Color.blue);
            blue.tileSize = new Vector2(10.0f, 10.0f);
            blue.tileOffset = Vector2.zero;

            heatMapColours.Add(blue);
            heatMapColours.Add(cyan);
            heatMapColours.Add(green);
            heatMapColours.Add(yellow);
            heatMapColours.Add(red);


            // Return the Array of the list.
            return heatMapColours.ToArray();

        }

        /// <summary>
        /// Generate the custom splatPrototypes for a Heatmap.
        /// </summary>
        /// <param name="datatexture"></param>
        /// <returns></returns>
        SplatPrototype[] GenerateSplatPrototypesCustom(Heatmap datatexture)
        {
            List<SplatPrototype> splats = new List<SplatPrototype>();

            foreach (HeatmapSplatprototype layer in datatexture.dataVisualisaitonSplatMaps)
            {
                SplatPrototype a = new SplatPrototype();

                a.normalMap = layer.normalMap;
                a.metallic = layer.metallic;
                a.smoothness = layer.smoothness;
                a.tileOffset = layer.tileOffset;
                a.tileSize = layer.tileSizing;

                if (layer is ColorSplatMap)
                {
                    a.texture = TextureGenerator.GenerateTexture2D(((ColorSplatMap)layer).color, 1, 1);
                }
                else
                {
                    a.texture = ((TextureSplatMap)layer).texture;
                }

                splats.Add(a);
            }


            return splats.ToArray();
        }

        /// <summary>
        /// Genereates the correct type of Splatprototypes for the heatmap
        /// </summary>
        /// <param name="heatmap"></param>
        /// <returns></returns>
        SplatPrototype[] GenerateSplatPrototypes(Heatmap heatmap)
        {
            switch (heatmap.texSource)
            {
                case (TextureSource.Custom):
                    return GenerateSplatPrototypesCustom(heatmap);

                default:
                    return GenerateSplatPrototypesDefault();

            }

        }

        /// <summary>
        /// Flip the heatmap constraints.
        /// </summary>
        /// <param name="heatmapIndex"></param>
        /// <param name="heatmaps"></param>
        /// <returns></returns>
        Heatmap FlipHeatmapConstraints(int heatmapIndex, ref List<Heatmap> heatmaps)
        {
            var heatmap = heatmaps[heatmapIndex];

            float temp = heatmap.upperValueThreshold;

            heatmap.upperValueThreshold = heatmap.lowerValueThreshold;
            heatmap.lowerValueThreshold = temp;

            return heatmap;
        }

        /// <summary>
        /// Chooses the correct method to assign the values to the Heatmap's data points.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="terrainObjectSize"></param>
        /// <param name="heightMap"></param>
        /// <param name="heightMapScale"></param>
        /// <param name="customDataList"></param>
        /// <param name="positionOffset"></param>
        void AssignMapData(Heatmap heatmap, int alphaMapResolution, Vector3 terrainObjectSize, float[,] heightMap, Vector3 heightMapScale, HeatmapNode[] customDataList, Vector3 positionOffset)
        {

            switch (heatmap.dataType)
            {
                case (HeatmapData.Custom):

                    CustomDataToHeatmapData(heatmap, customDataList, false, terrainObjectSize, alphaMapResolution, positionOffset);
                    break;

                default:
                    TerrainHeightToHeatmapData(heatmap, terrainObjectSize, alphaMapResolution, heightMap, heightMapScale);
                    break;
            }

        }

        /// <summary>
        /// Generate heatmap data from the Terrain's height map.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="terrainObjectSize"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="heightMap"></param>
        /// <param name="heightMapScale"></param>
        void TerrainHeightToHeatmapData(Heatmap heatmap, Vector3 terrainObjectSize, int alphaMapResolution, float[,] heightMap, Vector3 heightMapScale)
        {
            HeatmapDatum[,] heatmapValues = new HeatmapDatum[heatmap.heatmapResolution + 1, heatmap.heatmapResolution + 1];

            for (int x = 0; x <= heatmap.heatmapResolution; x++)
            {
                for (int y = 0; y <= heatmap.heatmapResolution; y++)
                {
                    float resScaleFactorX = terrainObjectSize.x / heatmap.heatmapResolution;
                    float resScaleFactorZ = terrainObjectSize.z / heatmap.heatmapResolution;

                    Vector3 nodeLocation = new Vector3((int)(y * resScaleFactorZ / (terrainObjectSize.z / 512)), 0.0f, (int)(x * resScaleFactorX / (terrainObjectSize.x / 512)));

                    float nodeValue = heightMap[(int)nodeLocation.z, (int)nodeLocation.x] * heightMapScale.y;


                    heatmapValues[x, y] = new HeatmapDatum();
                    heatmapValues[x, y].Init(nodeLocation, nodeValue, Coordinates.WorldToTerrainCoords(nodeLocation, terrainObjectSize, alphaMapResolution));
                }
            }

            heatmap.mapData = heatmapValues;
        }


        /// <summary>
        /// Generate Heatmap data from custom data provided.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="customData"></param>
        /// <param name="addValuesOfIntersectingPoints"></param>
        /// <param name="terrainSize"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="positionOffset"></param>
        void CustomDataToHeatmapData(Heatmap heatmap, HeatmapNode[] customData, bool addValuesOfIntersectingPoints, Vector3 terrainSize, int alphaMapResolution, Vector3 positionOffset)
        {
            HeatmapDatum[,] heatmapDataArray = new HeatmapDatum[heatmap.heatmapResolution + 1, heatmap.heatmapResolution + 1];

            // Assign the resulting value of the alphaMapResolution divided by the dataTextureResolution to a variable, as this is used a lot.
            int alphaMapDividedByDataTextureResolution = (alphaMapResolution / heatmap.heatmapResolution);


            // Populate the newly created returnVal array with default data.
            for (int x = 0; x <= heatmap.heatmapResolution; x++)
            {
                for (int y = 0; y <= heatmap.heatmapResolution; y++)
                {
                    heatmapDataArray[x, y] = new HeatmapDatum();
                    heatmapDataArray[x, y].value = heatmap.baseValue;
                    heatmapDataArray[x, y].nodePosition = new Vector3((int)(x * alphaMapDividedByDataTextureResolution), 0.0f, (int)(y * alphaMapDividedByDataTextureResolution));
                    heatmapDataArray[x, y].terrainCoords = new Pair<int>((int)(x * alphaMapDividedByDataTextureResolution), (int)(y * alphaMapDividedByDataTextureResolution));
                }
            }

            float incrementInterval = (alphaMapResolution / heatmap.heatmapResolution);

            //Assign the values of all the points we know to the correct position in the array.
            foreach (HeatmapNode customDatum in customData)
            {
                int arrayPositionX, arrayPositionY;
                int brushSize = customDatum.brushSize;

                float brushHardness = customDatum.brushHardness;

                float brushOpacity = customDatum.brushOpacity;

                // Ensure that the data is valid.
                brushOpacity = Mathf.Clamp(brushOpacity, 0.0f, 100.0f) / 100;
                brushHardness = Mathf.Clamp(brushHardness, 0.0f, 100.0f) / 100;

                // Find the correct position in the array based on the object's transform.
                Vector3 closetMapPointPosition = GetClosestDataPointPosition((customDatum.position - positionOffset), terrainSize, heatmap.heatmapResolution, alphaMapResolution, incrementInterval);
                Vector2 textureCoordianates = Coordinates.WorldToTerrainCoords(closetMapPointPosition, terrainSize, alphaMapResolution);

                // Using the dataTexture Resolution and the Terrain coordiantes, calculate the correct position in the array.
                arrayPositionX = (int)textureCoordianates.x / alphaMapDividedByDataTextureResolution;
                arrayPositionY = (int)textureCoordianates.y / alphaMapDividedByDataTextureResolution;


                // Calculate the correct value when taking the opacity of the brush into account.
                float value = customDatum.value * brushOpacity;

                if (!customDatum.overwriteOtherNodeValues)
                {
                    if (arrayPositionX >= 0 && arrayPositionY >= 0 && arrayPositionX < heatmap.heatmapResolution && arrayPositionY < heatmap.heatmapResolution)
                    {
                        value = customDatum.value * brushOpacity + heatmapDataArray[arrayPositionX, arrayPositionY].value;
                    }
                }



                // Using the terrain Coordinates, create the Datum that we're going to store in the Array.
                HeatmapDatum textureDatum = new HeatmapDatum();
                textureDatum.Init(closetMapPointPosition, value, new Pair<int>((int)textureCoordianates.x, (int)textureCoordianates.y));

                // Assign the VisualisationTextureDatum to the correct Index in the Array.
                if (arrayPositionX < heatmap.heatmapResolution && arrayPositionY < heatmap.heatmapResolution && arrayPositionX > 0 && arrayPositionY > 0) heatmapDataArray[arrayPositionX, arrayPositionY] = textureDatum;




                // If the brushSize is less than zero then we won't attempt to update the values of the surrounding nodes.
                if (brushSize > 0)
                {
                    // Calculate how many steps outwards we need to take (and thus how many other nodes we need to update).
                    int diameter = (int)(brushSize / alphaMapDividedByDataTextureResolution);

                    // We must have at least 2 steps outwards to be able to define a brush.
                    if (diameter > 0)
                    {
                        int radius = diameter / 2;
                        int radiusSquared = radius * radius;

                        for (int x = -radius; x < +radius; x++)
                        {
                            for (int y = -radius; y < +radius; y++)
                            {
                                int arryPosX = arrayPositionX + x;
                                int arryPosY = arrayPositionY + y;

                                if (arryPosX == arrayPositionX && arryPosY == arrayPositionY) continue;

                                if (arryPosX >= 0 && arryPosY >= 0 && arryPosX <= heatmap.heatmapResolution && arryPosY <= heatmap.heatmapResolution)
                                {
                                    int dx = arryPosX - arrayPositionX;
                                    int dy = arryPosY - arrayPositionY;

                                    float distanceSquared = (dx * dx) + (dy * dy);

                                    if (distanceSquared <= radiusSquared)
                                    {
                                        float brushHardnessModifier = radiusSquared - (distanceSquared * (1.0f - brushHardness));
                                        brushHardnessModifier /= radiusSquared;

                                        if (!customDatum.overwriteOtherNodeValues)
                                        {
                                            heatmapDataArray[arryPosX, arryPosY].value += (brushHardnessModifier * customDatum.value) * brushOpacity;
                                        }
                                        else
                                        {
                                            heatmapDataArray[arryPosX, arryPosY].value = (brushHardnessModifier * customDatum.value) * brushOpacity;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

            }

            heatmap.mapData = heatmapDataArray;
        }


        /// <summary>
        /// Assigns the colour value based on the heatMap data to a given terrain Pixel.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        void AssignHeatmapColorValue(ref Heatmap heatmap, int x, int y)
        {

            // Calculate the value as a % of HottestValueThreshold.
            float value = heatmap.visualisationValueMap[x, y];

            value += CalculateLowerUpperThresholdValueOffset(heatmap.lowerValueThreshold);

            // Offset the value to ensure it's positive.
            value += heatmap.upperValueThreshold / heatmap.splatPrototypes.Length;

            // Calculate the value as a % of the range of values it could be.
            value = (value / (heatmap.upperValueThreshold - heatmap.lowerValueThreshold));

            // Normalise the value so if the value is at HottestValueThreshold, that threshold is equal to the last index in heatMapSplatPrototypes.
            value = (value * heatmap.splatPrototypes.Length - 1);

            if (float.IsNaN(value)) value = 0.0f;

            // If the value is less than zero, then set the bottom layer to an opacity of 100% and all the other layers to 0% opacity.
            if (value < 0)
            {
                // Set the bottom layer to be 100% opaque.
                heatmap.alphaMapData[x, y, 0] = 1.0f;

                // Loop through all other layers setting their values to 0% opacity.
                for (int layerAboveZero = 1; layerAboveZero < heatmap.splatPrototypes.Length; layerAboveZero++)
                {
                    heatmap.alphaMapData[x, y, layerAboveZero] = 0.0f;
                }

                return;
            }
            // If the value is above the index number of the highest layer.
            else if (value >= heatmap.splatPrototypes.Length - 1)
            {
                // Set the value fot the hottest layer to be 100% opaque.
                heatmap.alphaMapData[x, y, heatmap.splatPrototypes.Length - 1] = 1.0f;

                // Loop through all the other layers setting their values to 0% opacity.
                for (int layersBelowTop = (heatmap.splatPrototypes.Length - 2); layersBelowTop >= 0; layersBelowTop--)
                {
                    heatmap.alphaMapData[x, y, layersBelowTop] = 0.0f;
                }
                return;
            }

            // If the value is within the range of the heatMap.
            else
            {
                // Loop through all the layers to find the layers where the blending should occur.
                for (int layernum = heatmap.splatPrototypes.Length - 1; layernum > 0; layernum--)
                {
                    // If this condition is true, we need to blend these layers.
                    if (value < layernum && value >= layernum - 1)
                    {
                        heatmap.alphaMapData[x, y, layernum] = value - (layernum - 1);
                        heatmap.alphaMapData[x, y, layernum - 1] = 1.0f - (value - (layernum - 1));

                        // Loop through all layers above this layer and set to 0% opacity.
                        for (int num = layernum + 1; num < heatmap.splatPrototypes.Length; num++)
                        {
                            heatmap.alphaMapData[x, y, num] = 0.0f;
                        }

                        // Loop through all layers below this layer and set to 0% opacity.
                        for (int num = layernum - 2; num >= 0; num--)
                        {
                            heatmap.alphaMapData[x, y, num] = 0.0f;
                        }
                        // Return as correct value has been set for all layers.
                        return;
                    }

                }

                Debug.LogError("The color value of " + value + " could not bet set at location [" + x + "," + y + "]");
                return;
            }

        }

        /// <summary>
        /// Assigns the colour value based on the heatMap data to a range of pixels.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="startX"></param>
        /// <param name="startY"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        void AssignVisualisationTextureColorValue(ref Heatmap heatmap, int startX, int startY, int width, int height)
        {

            // Assign the correct pixel value to each requested pixel.
            for (int x = startX; x < (startX + width); x++)
            {
                for (int y = startY; y < (startY + height); y++)
                {
                    AssignHeatmapColorValue(ref heatmap, x, y);
                }
            }

        }

        /// <summary>
        /// Interpolate the zero points on the HeatValueMap.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <returns></returns>
        float[,] InterpolateColorTextureValues(ref Heatmap heatmap, int alphaMapResolution)
        {
            // If the dataTexture Resoloution and the alphaMapResolution match then there is no need to interpolate the data.
            if (heatmap.heatmapResolution == alphaMapResolution)
            {
                return InterpolationMethodNone(ref heatmap, alphaMapResolution);
            }

            switch (heatmap.interpolationMode)
            {
                case InterpolationMode.NearestNeighbor:
                    return InterpolationMethodNearestNeighbor(ref heatmap, alphaMapResolution);

                case InterpolationMode.Bilinear:
                    return InterpolationMethodBilinear(ref heatmap, alphaMapResolution);

                default:
                    return InterpolationMethodNearestNeighbor(ref heatmap, alphaMapResolution);

            }

        }

        /// <summary>
        /// Chooses the correct interpolation method.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        float[,] InterpolateColorTextureValues(ref Heatmap heatmap, int alphaMapResolution, int x, int y, int width, int height)
        {

            // If the dataTexture Resoloution and the alphaMapResolution match then there is no need to interpolate the data.
            if (heatmap.heatmapResolution == alphaMapResolution)
            {
                return InterpolationMethodNone(ref heatmap, alphaMapResolution);
            }

            switch (heatmap.interpolationMode)
            {
                case InterpolationMode.NearestNeighbor:
                    return InterpolationMethodNearestNeighbor(ref heatmap, alphaMapResolution, x, y, width, height);

                case InterpolationMode.Bilinear:
                    return InterpolationMethodBilinear(ref heatmap, alphaMapResolution, x, y, width, height);

                default:
                    return InterpolationMethodNearestNeighbor(ref heatmap, alphaMapResolution, x, y, width, height);

            }

        }

        /// <summary>
        /// Calculates the offest of the cold value thereshold from 0 if it's negative.
        /// </summary>
        /// <param name="lowerThreshold"></param>
        /// <returns></returns>
        float CalculateLowerUpperThresholdValueOffset(float lowerThreshold)
        {
            if (lowerThreshold < 0) return Mathf.Abs(lowerThreshold);

            return -lowerThreshold;
        }

        /// <summary>
        /// Perform Nearest-Neighbour interpolation with fewer parameters.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodNearestNeighbor(ref Heatmap heatmap, int alphaMapResolution)
        {
            return InterpolationMethodNearestNeighbor(ref heatmap, alphaMapResolution, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        /// <summary>
        /// Nearest neighbour interpolation.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodNearestNeighbor(ref Heatmap heatmap, int alphaMapResolution, int x, int y, int width, int height)
        {
            float[,] heatmapValues = new float[width, height];
            float incrementInterval = (alphaMapResolution / heatmap.heatmapResolution);


            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    heatmapValues[i, j] = this.GetClosestDataPoint(ref heatmap, i, j, alphaMapResolution, incrementInterval).value;
                }
            }


            return heatmapValues;
        }

        /// <summary>
        /// No Interpolation, just extract the mapData we have (When  DataPoint resolution matches the alphamapResolution this is the default).
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodNone(ref Heatmap heatmap, int alphaMapResolution)
        {
            return InterpolationMethodNone(ref heatmap, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        /// <summary>
        /// No Interpolation, just extract the mapData we have (When  DataPoint resolution matches the alphamapResolution this is the default).
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodNone(ref Heatmap heatmap, int x, int y, int width, int height)
        {
            float[,] heatmapValues = new float[width, height];

            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    heatmapValues[i, j] = heatmap.mapData[i, j].value;
                }
            }

            return heatmapValues;
        }


        /// <summary>
        /// Perform Bi-Linear interpolation with fewer parameters.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodBilinear(ref Heatmap heatmap, int alphaMapResolution)
        {
            return InterpolationMethodBilinear(ref heatmap, alphaMapResolution, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        /// <summary>
        /// // Perform Bi-Linear interpolation.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <returns></returns>
        public float[,] InterpolationMethodBilinear(ref Heatmap heatmap, int alphaMapResolution, int x, int y, int width, int height)
        {
            float[,] heatmapValues = new float[width, height];
            float incrementInterval = (alphaMapResolution / (heatmap.heatmapResolution));

            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    HeatmapDatum[,] qPoints = GetQPoints(ref heatmap, i, j, alphaMapResolution, incrementInterval);

                    float xT, yT, rOne, rTwo;

                    xT = (((float)i - qPoints[0, 0].terrainCoords.First) / (qPoints[1, 0].terrainCoords.First - qPoints[0, 0].terrainCoords.First));
                    yT = (((float)j - qPoints[0, 0].terrainCoords.Second) / (qPoints[0, 1].terrainCoords.Second - qPoints[0, 0].terrainCoords.Second));

                    rOne = Mathf.Lerp(qPoints[0, 0].value, qPoints[1, 0].value, xT);
                    rTwo = Mathf.Lerp(qPoints[0, 1].value, qPoints[1, 1].value, xT);

                    heatmapValues[i, j] = Mathf.Lerp(rOne, rTwo, yT);

                }
            }

            return heatmapValues;
        }


        /// <summary>
        /// Retrieves QPoints for interpolation.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="incrementInterval"></param>
        /// <returns></returns>
        HeatmapDatum[,] GetQPoints(ref Heatmap heatmap, int x, int y, int alphaMapResolution, float incrementInterval)
        {
            HeatmapDatum[,] qPoints = new HeatmapDatum[2, 2];

            int xGridSquare = Mathf.FloorToInt(((float)x / incrementInterval));
            int yGridSquare = Mathf.FloorToInt(((float)y / incrementInterval));


            qPoints[0, 0] = heatmap.mapData[xGridSquare, yGridSquare];
            qPoints[0, 1] = heatmap.mapData[xGridSquare, yGridSquare + 1];
            qPoints[1, 0] = heatmap.mapData[xGridSquare + 1, yGridSquare];
            qPoints[1, 1] = heatmap.mapData[xGridSquare + 1, yGridSquare + 1];

            return qPoints;

        }

        /// <summary>
        /// Returns the closest data point to texture coordinate.
        /// </summary>
        /// <param name="heatmap"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="incrementInterval"></param>
        /// <returns></returns>
        HeatmapDatum GetClosestDataPoint(ref Heatmap heatmap, int x, int y, int alphaMapResolution, float incrementInterval)
        {
            HeatmapDatum closetDataPoint;

            int xGridSquare = Mathf.RoundToInt(x / incrementInterval);
            int yGridSquare = Mathf.RoundToInt(y / incrementInterval);

            closetDataPoint = heatmap.mapData[xGridSquare, yGridSquare];

            return closetDataPoint;
        }

        /// <summary>
        /// Get the postion of the closest data point to a given world position.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="terrainSize"></param>
        /// <param name="dataTextureResolution"></param>
        /// <param name="alphaMapResolution"></param>
        /// <param name="incrementInterval"></param>
        /// <returns></returns>
        public Vector3 GetClosestDataPointPosition(Vector3 worldPosition, Vector3 terrainSize, int dataTextureResolution, int alphaMapResolution, float incrementInterval)
        {
            Vector3 closestDataPoint;

            Vector2 terrainCoords = Coordinates.WorldToTerrainCoords(worldPosition, terrainSize, alphaMapResolution);

            int xGridSquare = Mathf.RoundToInt(terrainCoords.x / incrementInterval);
            int yGridSquare = Mathf.RoundToInt(terrainCoords.y / incrementInterval);

            closestDataPoint = new Vector3(yGridSquare * incrementInterval, 0.0f, xGridSquare * incrementInterval);

            return closestDataPoint;
        }

    }
}