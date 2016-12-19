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
using System.Collections.Generic;
using System.Threading;
using TBUnityLib.TextureTools;
using TBUnityLib.TerrainTools;
using TBUnityLib.Generic;

namespace TerrainHeatmap
{
    public class HeatmapGenerator
    {

        Thread _generateHeatMapThread;
        public bool isGenerateHeatMapThreadFinished = true;

        int _selectedTextureIndex;
        List<Heatmap> _visualisationTextures;
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
        HeatmapNode[] _customDataList;
        Vector3 _positionOffset;

        public void Abort()
        {
            if (_generateHeatMapThread == null) return;
            _generateHeatMapThread.Abort();
        }

        public void Pause()
        {
            if (_generateHeatMapThread == null) return;
            Thread.Sleep(Timeout.Infinite);
        }

        public void Resume()
        {
            if (_generateHeatMapThread == null) return;
            _generateHeatMapThread.Interrupt();
        }


        public void GenerateHeatMapChunkThreaded()
        {
            GenerateHeatMap(_selectedTextureIndex, ref _visualisationTextures, _alphaMapResolution, _terrainObjectSize, terrainX, terrainY, _width, _height, _heightMap, _heightMapScale, false, _customDataList, _positionOffset);
            processedAlphaMap = Chunks.GetAlphaMapChunk(_originalAlphaMap, terrainY, terrainX, _width, _height);
            isGenerateHeatMapThreadFinished = true;
        }

        public void ProcessHeatMapThreaded(int selectedTextureIndex, ref List<Heatmap> visualisationTextures, float[,] heightMap, int alphaMapResolution, Vector3 terrainObjectSize, Vector3 heightMapScale, int x, int y, int width, int height, HeatmapNode[] customDataList, Vector3 positionOffset)
        {
            isGenerateHeatMapThreadFinished = false;

            _selectedTextureIndex = selectedTextureIndex;
            _visualisationTextures = visualisationTextures;
            _heightMap = heightMap;
            _alphaMapResolution = alphaMapResolution;
            terrainX = x;
            terrainY = y;
            _width = width;
            _height = height;
            _heightMapScale = heightMapScale;
            _terrainObjectSize = terrainObjectSize;
            _originalAlphaMap = visualisationTextures[selectedTextureIndex].alphaMapData;
            _customDataList = customDataList;
            _positionOffset = positionOffset;

            _generateHeatMapThread = new Thread(GenerateHeatMapChunkThreaded);
            _generateHeatMapThread.Priority = System.Threading.ThreadPriority.Highest;
            _generateHeatMapThread.Start();
        }


        public void GenerateHeatMap(int selectedTextureIndex, ref List<Heatmap> visualisationTextures, Terrain terrain, HeatmapNode[] customDataList, Vector3 positionOffset)
        {
            if (terrain == null)
            {
                return;
            }
            float[,] heights = terrain.terrainData.GetHeights(0, 0, terrain.terrainData.heightmapResolution, terrain.terrainData.heightmapResolution);
            GenerateHeatMap(selectedTextureIndex, ref visualisationTextures, terrain.terrainData.alphamapResolution, terrain.terrainData.size, 0, 0, terrain.terrainData.alphamapResolution, terrain.terrainData.alphamapResolution, heights, terrain.terrainData.heightmapScale, true, customDataList, positionOffset);
        }

        public void GenerateHeatMap(int selectedTextureIndex, ref List<Heatmap> heatmaps, int alphaMapResolution, Vector3 terrainObjectSize, int x, int y, int width, int height, float[,] heightMap, Vector3 heightMapScale, bool splatMapUpdateOverride, HeatmapNode[] customDataList, Vector3 positionOffset)
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

            if (selectedHeatmap.displayFlippedConstraints) selectedHeatmap = this.FlipDataTextureConstraints(selectedTextureIndex, ref heatmaps);

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
            AssignMapData(selectedHeatmap, alphaMapResolution, terrainObjectSize, heightMap, heightMapScale, customDataList, positionOffset);

            if (selectedHeatmap.autoConstrain) AutoConstrainValues(ref selectedHeatmap, selectedHeatmap.displayFlippedConstraints);
            if (selectedHeatmap.displayFlippedConstraints) selectedHeatmap.coldHotValueOffset = CalculateLowerUpperThresholdValueOffset(selectedHeatmap.lowerValueThreshold);


            // Interpolate the data on the Heat value Map.
            selectedHeatmap.visualisationValueMap = InterpolateColorTextureValues(ref selectedHeatmap, alphaMapResolution);

            // Assignt the correct HeatMap Color value to each pixel on the Terrain.
            AssignVisualisationTextureColorValue(ref selectedHeatmap, x, y, width, height);

            selectedHeatmap.lowerValueThreshold = originalLowerValueThreshold;
            selectedHeatmap.upperValueThreshold = originalUpperValueThreshold;

        }

        void AutoConstrainValues(ref Heatmap dataTexture, bool flipConstraints)
        {
            float upperValue = dataTexture.mapData[0, 0].value;
            float lowerValue = dataTexture.mapData[0, 0].value;

            foreach (HeatmapDatum datum in dataTexture.mapData)
            {
                if (datum.value > upperValue) upperValue = datum.value;
                if (datum.value < lowerValue) lowerValue = datum.value;
            }

            if (!flipConstraints)
            {

                dataTexture.lowerValueThreshold = lowerValue + 1;
                dataTexture.upperValueThreshold = upperValue - 1;
            }
            else
            {
                dataTexture.upperValueThreshold = lowerValue + 1;
                dataTexture.lowerValueThreshold = upperValue - 1;
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

        SplatPrototype[] GenerateSplatPrototypes(Heatmap datatexture)
        {
            switch (datatexture.texSource)
            {
                case (TextureSource.DefaultColors):
                    return GenerateSplatPrototypesDefault();

                case (TextureSource.Custom):
                    return GenerateSplatPrototypesCustom(datatexture);

                default:
                    return GenerateSplatPrototypesDefault();

            }

        }

        Heatmap FlipDataTextureConstraints(int dataTextureIndex, ref List<Heatmap> visualisationTextures)
        {
            var dataTex = visualisationTextures[dataTextureIndex];

            float temp = dataTex.upperValueThreshold;

            dataTex.upperValueThreshold = dataTex.lowerValueThreshold;
            dataTex.lowerValueThreshold = temp;

            return dataTex;
        }

        void AssignMapData(Heatmap dataTexture, int alphaMapResolution, Vector3 terrainObjectSize, float[,] heightMap, Vector3 heightMapScale, HeatmapNode[] customDataList, Vector3 positionOffset)
        {

            switch (dataTexture.dataType)
            {

                case (HeatmapData.HeightMap):
                    TerrainHeightToVisualisationTextureData(dataTexture, terrainObjectSize, alphaMapResolution, heightMap, heightMapScale);
                    break;

                //case (VisualisationData.Steepness):
                //    return TerrainSteepnessToVisualisationTextureData(dataTexture,terrainObjectSize,alphaMapResolution);

                case (HeatmapData.Custom):

                    CustomDataToVisualisationTextureData(dataTexture, customDataList, false, terrainObjectSize, alphaMapResolution, positionOffset);
                    break;

                default:
                    TerrainHeightToVisualisationTextureData(dataTexture, terrainObjectSize, alphaMapResolution, heightMap, heightMapScale);
                    break;
            }

        }

        // Generate heatmap data from the Terrain's height map.
        void TerrainHeightToVisualisationTextureData(Heatmap dataTexture, Vector3 terrainObjectSize, int alphaMapResolution, float[,] heightMap, Vector3 heightMapScale)
        {
            HeatmapDatum[,] returnVal = new HeatmapDatum[dataTexture.heatmapResolution + 1, dataTexture.heatmapResolution + 1];

            for (int x = 0; x <= dataTexture.heatmapResolution; x++)
            {
                for (int y = 0; y <= dataTexture.heatmapResolution; y++)
                {
                    float resScaleFactorX = terrainObjectSize.x / dataTexture.heatmapResolution;
                    float resScaleFactorZ = terrainObjectSize.z / dataTexture.heatmapResolution;

                    Vector3 nodeLocation = new Vector3((int)(y * resScaleFactorZ / (terrainObjectSize.z / 512)), 0.0f, (int)(x * resScaleFactorX / (terrainObjectSize.x / 512)));

                    float nodeValue = heightMap[(int)nodeLocation.z, (int)nodeLocation.x] * heightMapScale.y;


                    returnVal[x, y] = new HeatmapDatum();
                    returnVal[x, y].Init(nodeLocation, nodeValue, Coordinates.WorldToTerrainCoords(nodeLocation, terrainObjectSize, alphaMapResolution));
                }
            }

            dataTexture.mapData = returnVal;
        }

        //// Generate heatmap data from the Terrain's steepness.
        //VisualisationTextureDatum[,] TerrainSteepnessToVisualisationTextureData(VisualisationTexture dataTexture,  Vector3 terrainObjectSize, int alphaMapResolution)
        //{
        //    VisualisationTextureDatum[,] returnVal = new VisualisationTextureDatum[dataTexture.DataTextureResolution + 1, dataTexture.DataTextureResolution + 1];

        //    for (int x = 0; x <= dataTexture.DataTextureResolution; x++)
        //    {
        //        for (int y = 0; y <= dataTexture.DataTextureResolution; y++)
        //        {
        //            float resScaleFactorX = terrainObjectSize.x / dataTexture.DataTextureResolution;
        //            float resScaleFactorZ = terrainObjectSize.z / dataTexture.DataTextureResolution;
        //            Vector3 nodeLocation = new Vector3((int)(y * resScaleFactorZ / (terrainObjectSize.z / 512)), 0.0f, (int)(x * resScaleFactorX / (terrainObjectSize.x / 512)));
        //            float nodeValue = terrainObject.terrainData.GetSteepness(nodeLocation.x / terrainObjectSize.x, nodeLocation.z / terrainObjectSize.z);
        //            returnVal[x, y] = new VisualisationTextureDatum(nodeLocation, nodeValue, TBUtilities.TB_Terrain.WorldToTerrainCoords(nodeLocation, terrainObjectSize, alphaMapResolution));
        //        }
        //    }
        //    return returnVal;
        //}


        void CustomDataToVisualisationTextureData(Heatmap dataTexture, HeatmapNode[] customData, bool addCloseValues, Vector3 terrainSize, int alphaMapResolution, Vector3 positionOffset)
        {
            HeatmapDatum[,] returnVal = new HeatmapDatum[dataTexture.heatmapResolution + 1, dataTexture.heatmapResolution + 1];

            // Assign the resulting value of the alphaMapResolution divided by the dataTextureResolution to a variable, as this is used a lot.
            int alphaMapDividedByDataTextureResolution = (alphaMapResolution / dataTexture.heatmapResolution);


            // Populate the newly created returnVal array with default data.
            for (int x = 0; x <= dataTexture.heatmapResolution; x++)
            {
                for (int y = 0; y <= dataTexture.heatmapResolution; y++)
                {
                    returnVal[x, y] = new HeatmapDatum();
                    returnVal[x, y].value = dataTexture.baseValue;
                    returnVal[x, y].nodePosition = new Vector3((int)(x * alphaMapDividedByDataTextureResolution), 0.0f, (int)(y * alphaMapDividedByDataTextureResolution));
                    returnVal[x, y].terrainCoords = new Pair<int>((int)(x * alphaMapDividedByDataTextureResolution), (int)(y * alphaMapDividedByDataTextureResolution));
                }
            }

            float incrementInterval = (alphaMapResolution / dataTexture.heatmapResolution);

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
                Vector3 closetMapPointPosition = GetClosestDataPointPosition((customDatum.position - positionOffset), terrainSize, dataTexture.heatmapResolution, alphaMapResolution, incrementInterval);
                Vector2 textureCoordianates = Coordinates.WorldToTerrainCoords(closetMapPointPosition, terrainSize, alphaMapResolution);

                // Using the dataTexture Resolution and the Terrain coordiantes, calculate the correct position in the array.
                arrayPositionX = (int)textureCoordianates.x / alphaMapDividedByDataTextureResolution;
                arrayPositionY = (int)textureCoordianates.y / alphaMapDividedByDataTextureResolution;


                // Calculate the correct value when taking the opacity of the brush into account.
                float value = customDatum.value * brushOpacity;

                if (!customDatum.overwriteOtherNodeValues)
                {
                    if (arrayPositionX >= 0 && arrayPositionY >= 0 && arrayPositionX < dataTexture.heatmapResolution && arrayPositionY < dataTexture.heatmapResolution)
                    {
                        value = customDatum.value * brushOpacity + returnVal[arrayPositionX, arrayPositionY].value;

                        if (returnVal[arrayPositionX, arrayPositionY].value != dataTexture.baseValue)
                        {
                            float difference = returnVal[arrayPositionX, arrayPositionY].value - value;
                            value += difference / 2;
                        }
                    }
                }



                // Using the terrain Coordinates, create the Datum that we're going to store in the Array.
                HeatmapDatum textureDatum = new HeatmapDatum();
                textureDatum.Init(closetMapPointPosition, value, new Pair<int>((int)textureCoordianates.x, (int)textureCoordianates.y));

                // Assign the VisualisationTextureDatum to the correct Index in the Array.
                if (arrayPositionX < dataTexture.heatmapResolution && arrayPositionY < dataTexture.heatmapResolution && arrayPositionX > 0 && arrayPositionY > 0) returnVal[arrayPositionX, arrayPositionY] = textureDatum;




                // If the brushSize is less than zero then we won't attempt to update the values of the surrounding nodes.
                if (brushSize > 0)
                {
                    // Calculate how many steps outwards we need to take (and thus how many other nodes we need to update).
                    int diameter = (int)(brushSize / alphaMapDividedByDataTextureResolution);

                    // We must have at least 2 steps outwards to be able to define a brush.
                    if (diameter > 0)
                    {
                        if (customDatum.circularBrush)
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

                                    if (arryPosX >= 0 && arryPosY >= 0 && arryPosX < dataTexture.heatmapResolution && arryPosY < dataTexture.heatmapResolution)
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
                                                returnVal[arryPosX, arryPosY].value += (brushHardnessModifier * customDatum.value) * brushOpacity;
                                            }
                                            else
                                            {
                                                returnVal[arryPosX, arryPosY].value = (brushHardnessModifier * customDatum.value) * brushOpacity;
                                            }
                                        }
                                    }
                                }
                            }

                        }
                        else
                        {
                            for (int i = -diameter / 2; i < diameter / 2; i++)
                            {
                                for (int j = -diameter / 2; j < diameter / 2; j++)
                                {
                                    // Calculate the position of the array of this node.
                                    int arryPosX = arrayPositionX + j;
                                    int arryPosY = arrayPositionY + i;

                                    // If the current arrayPosition is out of range of the array, then check the next node.
                                    if (arryPosX < 0 || arryPosX > dataTexture.heatmapResolution) continue;
                                    if (arryPosY < 0 || arrayPositionY > dataTexture.heatmapResolution) continue;

                                    // If the node is the centre node (we've already update this node) then continue.
                                    if (arrayPositionX == arryPosX && arrayPositionY == arryPosY) continue;


                                    // Take the highest absolute value from i and j to determine how far we've stepped out from the centre of the brush.
                                    int currentStepOutwards;

                                    if (Mathf.Abs(i) > Mathf.Abs(j)) currentStepOutwards = Mathf.Abs(i);
                                    else currentStepOutwards = Mathf.Abs(j);

                                    // Calculate the brush hardness modifier, based on which step we're on and what the value of the brushHardness is.
                                    float brushHardnessModifier = (diameter / 2) - (currentStepOutwards * (1.0f - brushHardness));
                                    brushHardnessModifier /= diameter / 2;

                                    // Calculate and assign the value of this node, taking into account the brush hardness modifier, the opacity and the customDatum value.
                                    if (customDatum.overwriteOtherNodeValues)
                                    {
                                        // Assign the value calculated of this node to the correct entry in the array.
                                        returnVal[arryPosX, arryPosY].value = (brushHardnessModifier * customDatum.value) * brushOpacity;
                                    }
                                    else
                                    {
                                        // Add the value calculated for this node to the current value (if a current value exists).
                                        returnVal[arryPosX, arryPosY].value += (brushHardnessModifier * customDatum.value) * brushOpacity;
                                    }

                                }
                            }
                        }
                    }
                }

            }

            dataTexture.mapData = returnVal;
        }


        // Assigns the colour value based on the heatMap data to a given terrain Pixel.
        void AssignVisualisationTextureColorValue(ref Heatmap dataTexture, int x, int y)
        {

            // Calculate the value as a % of HottestValueThreshold.
            float value = dataTexture.visualisationValueMap[x, y];

            value += CalculateLowerUpperThresholdValueOffset(dataTexture.lowerValueThreshold);

            // Offset the value to ensure it's positive.
            value += dataTexture.upperValueThreshold / dataTexture.splatPrototypes.Length;

            // Calculate the value as a % of the range of values it could be.
            value = (value / (dataTexture.upperValueThreshold - dataTexture.lowerValueThreshold));

            // Normalise the value so if the value is at HottestValueThreshold, that threshold is equal to the last index in heatMapSplatPrototypes.
            value = (value * dataTexture.splatPrototypes.Length - 1);

            if (float.IsNaN(value)) value = 0.0f;

            // If the value is less than zero, then set the bottom layer to an opacity of 100% and all the other layers to 0% opacity.
            if (value < 0)
            {
                // Set the bottom layer to be 100% opaque.
                dataTexture.alphaMapData[x, y, 0] = 1.0f;

                // Loop through all other layers setting their values to 0% opacity.
                for (int layerAboveZero = 1; layerAboveZero < dataTexture.splatPrototypes.Length; layerAboveZero++)
                {
                    dataTexture.alphaMapData[x, y, layerAboveZero] = 0.0f;
                }

                return;
            }
            // If the value is above the index number of the highest layer.
            else if (value >= dataTexture.splatPrototypes.Length - 1)
            {
                // Set the value fot the hottest layer to be 100% opaque.
                dataTexture.alphaMapData[x, y, dataTexture.splatPrototypes.Length - 1] = 1.0f;

                // Loop through all the other layers setting their values to 0% opacity.
                for (int layersBelowTop = (dataTexture.splatPrototypes.Length - 2); layersBelowTop >= 0; layersBelowTop--)
                {
                    dataTexture.alphaMapData[x, y, layersBelowTop] = 0.0f;
                }
                return;
            }

            // If the value is within the range of the heatMap.
            else
            {
                // Loop through all the layers to find the layers where the blending should occur.
                for (int layernum = dataTexture.splatPrototypes.Length - 1; layernum > 0; layernum--)
                {
                    // If this condition is true, we need to blend these layers.
                    if (value < layernum && value >= layernum - 1)
                    {
                        dataTexture.alphaMapData[x, y, layernum] = value - (layernum - 1);
                        dataTexture.alphaMapData[x, y, layernum - 1] = 1.0f - (value - (layernum - 1));

                        // Loop through all layers above this layer and set to 0% opacity.
                        for (int num = layernum + 1; num < dataTexture.splatPrototypes.Length; num++)
                        {
                            dataTexture.alphaMapData[x, y, num] = 0.0f;
                        }

                        // Loop through all layers below this layer and set to 0% opacity.
                        for (int num = layernum - 2; num >= 0; num--)
                        {
                            dataTexture.alphaMapData[x, y, num] = 0.0f;
                        }
                        // Return as correct value has been set for all layers.
                        return;
                    }

                }

                Debug.LogError("The color value of " + value + " could not bet set at location [" + x + "," + y + "]");
                return;
            }

        }

        // Assigns the colour value based on the heatMap data to a range of pixels.
        void AssignVisualisationTextureColorValue(ref Heatmap dataTexture, int startX, int startY, int width, int height)
        {

            // Assign the correct pixel value to each requested pixel.
            for (int x = startX; x < (startX + width); x++)
            {
                for (int y = startY; y < (startY + height); y++)
                {
                    AssignVisualisationTextureColorValue(ref dataTexture, x, y);
                }
            }

        }

        // Interpolate the zero points on the HeatValueMap.
        float[,] InterpolateColorTextureValues(ref Heatmap visualisationTexture, int alphaMapResolution)
        {
            // If the dataTexture Resoloution and the alphaMapResolution match then there is no need to interpolate the data.
            if (visualisationTexture.heatmapResolution == alphaMapResolution)
            {
                return InterpolationMethodNone(ref visualisationTexture, alphaMapResolution);
            }

            switch (visualisationTexture.interpolationMode)
            {
                case InterpolationMode.NearestNeighbor:
                    return InterpolationMethodNearestNeighbor(ref visualisationTexture, alphaMapResolution);

                case InterpolationMode.Bilinear:
                    return InterpolationMethodBilinear(ref visualisationTexture, alphaMapResolution);

                default:
                    return InterpolationMethodNearestNeighbor(ref visualisationTexture, alphaMapResolution);

            }

        }

        float[,] InterpolateColorTextureValues(ref Heatmap visualisationTexture, int alphaMapResolution, int x, int y, int width, int height)
        {

            // If the dataTexture Resoloution and the alphaMapResolution match then there is no need to interpolate the data.
            if (visualisationTexture.heatmapResolution == alphaMapResolution)
            {
                return InterpolationMethodNone(ref visualisationTexture, alphaMapResolution);
            }

            switch (visualisationTexture.interpolationMode)
            {
                case InterpolationMode.NearestNeighbor:
                    return InterpolationMethodNearestNeighbor(ref visualisationTexture, alphaMapResolution, x, y, width, height);

                case InterpolationMode.Bilinear:
                    return InterpolationMethodBilinear(ref visualisationTexture, alphaMapResolution, x, y, width, height);

                default:
                    return InterpolationMethodNearestNeighbor(ref visualisationTexture, alphaMapResolution, x, y, width, height);

            }

        }

        // calculates the offest of the cold value thereshold from 0 if it's negative.
        float CalculateLowerUpperThresholdValueOffset(float lowerThreshold)
        {
            if (lowerThreshold < 0) return Mathf.Abs(lowerThreshold);

            return -lowerThreshold;
        }

        // Perform Nearest-Neighbour interpolation.
        public float[,] InterpolationMethodNearestNeighbor(ref Heatmap visualisationTexture, int alphaMapResolution)
        {
            return InterpolationMethodNearestNeighbor(ref visualisationTexture, alphaMapResolution, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        public float[,] InterpolationMethodNearestNeighbor(ref Heatmap heatmap, int alphaMapResolution, int x, int y, int width, int height)
        {
            float[,] returnValue = new float[width, height];
            float incrementInterval = (alphaMapResolution / heatmap.heatmapResolution);


            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    returnValue[i, j] = this.GetClosestDataPoint(ref heatmap, i, j, alphaMapResolution, incrementInterval).value;
                }
            }


            return returnValue;
        }

        // No Interpolation, just extract the mapData we have (When  DataPoint resolution matches the alphamapResolution this is the default).
        public float[,] InterpolationMethodNone(ref Heatmap visualisationTexture, int alphaMapResolution)
        {
            return InterpolationMethodNone(ref visualisationTexture, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        public float[,] InterpolationMethodNone(ref Heatmap visualisationTexture, int x, int y, int width, int height)
        {
            float[,] returnValue = new float[width, height];

            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    returnValue[i, j] = visualisationTexture.mapData[i, j].value;
                }
            }

            return returnValue;
        }


        // Perform Bi-Linear interpolation.
        public float[,] InterpolationMethodBilinear(ref Heatmap visualisationTexture, int alphaMapResolution)
        {
            return InterpolationMethodBilinear(ref visualisationTexture, alphaMapResolution, 0, 0, alphaMapResolution, alphaMapResolution);
        }

        public float[,] InterpolationMethodBilinear(ref Heatmap visualisationTexture, int alphaMapResolution, int x, int y, int width, int height)
        {
            float[,] returnValue = new float[width, height];
            float incrementInterval = (alphaMapResolution / (visualisationTexture.heatmapResolution));

            for (int i = x; i < x + width; i++)
            {
                for (int j = y; j < y + height; j++)
                {
                    HeatmapDatum[,] qPoints = GetQPoints(ref visualisationTexture, i, j, alphaMapResolution, incrementInterval);

                    float xT, yT, rOne, rTwo;

                    xT = (((float)i - qPoints[0, 0].terrainCoords.First) / (qPoints[1, 0].terrainCoords.First - qPoints[0, 0].terrainCoords.First));
                    yT = (((float)j - qPoints[0, 0].terrainCoords.Second) / (qPoints[0, 1].terrainCoords.Second - qPoints[0, 0].terrainCoords.Second));

                    rOne = Mathf.Lerp(qPoints[0, 0].value, qPoints[1, 0].value, xT);
                    rTwo = Mathf.Lerp(qPoints[0, 1].value, qPoints[1, 1].value, xT);

                    returnValue[i, j] = Mathf.Lerp(rOne, rTwo, yT);

                }
            }

            return returnValue;
        }


        // Retrieves QPoints for interpolation.
        HeatmapDatum[,] GetQPoints(ref Heatmap visualisationTexture, int x, int y, int alphaMapResolution, float incrementInterval)
        {
            HeatmapDatum[,] qPoints = new HeatmapDatum[2, 2];

            int xGridSquare = Mathf.FloorToInt(((float)x / incrementInterval));
            int yGridSquare = Mathf.FloorToInt(((float)y / incrementInterval));


            qPoints[0, 0] = visualisationTexture.mapData[xGridSquare, yGridSquare];
            qPoints[0, 1] = visualisationTexture.mapData[xGridSquare, yGridSquare + 1];
            qPoints[1, 0] = visualisationTexture.mapData[xGridSquare + 1, yGridSquare];
            qPoints[1, 1] = visualisationTexture.mapData[xGridSquare + 1, yGridSquare + 1];

            return qPoints;

        }

        // Returns the closest data point to texture coordinate.
        HeatmapDatum GetClosestDataPoint(ref Heatmap dataTexture, int x, int y, int alphaMapResolution, float incrementInterval)
        {
            HeatmapDatum closetDataPoint;

            int xGridSquare = Mathf.RoundToInt(x / incrementInterval);
            int yGridSquare = Mathf.RoundToInt(y / incrementInterval);

            closetDataPoint = dataTexture.mapData[xGridSquare, yGridSquare];

            return closetDataPoint;
        }

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