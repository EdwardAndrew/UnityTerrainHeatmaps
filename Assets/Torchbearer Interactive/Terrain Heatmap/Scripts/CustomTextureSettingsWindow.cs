//************************************************************************
// Copyright (C) Torchbearer Interactive, Ltd. - All Rights Reserved
// 
// Unauthorized copying of this file, via any medium is strictly prohibited
// Proprietary and confidential
// 
// Written by: Edward S Andrew - ed@tbinteractive.co.uk 2017
//************************************************************************
#if UNITY_EDITOR
using System.Collections;
using UnityEngine;
using UnityEditor;
using TBUnityLib.TextureTools;

namespace TerrainHeatmap
{
    public class CustomTextureSettingsWindow : EditorWindow
    {

        bool _textureToggle = false;

        // The diffuse texture map.
        Texture2D _albedoTexture;

        // The normal texture.
        Texture2D _normalTexture;

        // If using custom Colors, the Color of the splatMap
        Color _color;

        // The smoothness of the texture.
        float _smoothness;

        // How metallic the object is.
        float _metallic;

        // The tile size of the applied texture.
        Vector2 _tileSize;

        // the tile offset of the applied texture.
        Vector2 _tileOffset;

        // A reference to the CustomInspectorof the GameObject.
        HeatmapControllerCustomInspector _customInspector;

        // Controls if the window is in edit Texture mode or not.
        bool _editTexture = false;

        HeatmapSplatprototype splatMap;


        // Displays the window as an Add texture window, so clicking apply will add a new texture layer to the DataTexture.
        public void ShowAddLayer()
        {
            // Set the title to add texture.
            this.titleContent = new GUIContent("Add Texture");

            // We don't want the used to be able to make the window too small.
            this.minSize = new Vector2(200.0f, 300.0f);

            // Display the window as a utility window.
            this.ShowUtility();

            // Set all the values to good default values.
            _albedoTexture = null;
            _color = Color.black;
            _normalTexture = null;
            _smoothness = 0;
            _metallic = 0;
            _tileSize = new Vector2(15.0f, 15.0f);
            _tileOffset = new Vector2(0.0f, 0.0f);

            // We launched the window in "Add Texture" mode, so we must set Edit texture to null.
            _editTexture = false;
        }

        // Displays the window as the "Edit texture" window, so clicking apply will edit the currently selected texture.
        public void ShowEditLayer(HeatmapSplatprototype layer)
        {
            splatMap = layer;
            if (splatMap is TextureSplatMap)
            {
                _textureToggle = false;
            }
            else
            {
                _textureToggle = true;
            }
            // Set the window title to "Edit Texture".
            this.titleContent = new GUIContent("Edit Texture");

            // We don't want the used to be able to make the window too small.
            this.minSize = new Vector2(200.0f, 300.0f);

            // Display the window as a utility window.
            this.ShowUtility();

            // Retrieve the correct parameters and display them in the window.
            if (layer is ColorSplatMap)
            {
                _albedoTexture = TextureGenerator.GenerateTexture2D(((ColorSplatMap)splatMap).color, 40, 40);
            }
            else
            {
                _albedoTexture = ((TextureSplatMap)splatMap).texture;
            }
            if (layer is TextureSplatMap)
            {
                try
                {
                    if (((TextureSplatMap)splatMap).texture != null)
                    {
                        _color = ((TextureSplatMap)splatMap).texture.GetPixel(0, 0);
                    }
                }
                catch (UnityException)
                {
                    _color = Color.black;
                }
            }
            else
            {
                _color = ((ColorSplatMap)splatMap).color;
            }

            _normalTexture = splatMap.normalMap;
            _smoothness = splatMap.smoothness;
            _metallic = splatMap.metallic;
            _tileOffset = splatMap.tileOffset;
            _tileSize = splatMap.tileSizing;


            // We launched in "Edit Texture" mode, so set editTexture to true.
            _editTexture = true;
        }


        // Setup the TerrainDataTextureSettingsWindow so we can use it safely.
        public static CustomTextureSettingsWindow Setup(HeatmapControllerCustomInspector inspector)
        {
            var window = (CustomTextureSettingsWindow)ScriptableObject.CreateInstance(typeof(CustomTextureSettingsWindow));
            window._customInspector = inspector;

            return window;
        }



        void OnGUI()
        {

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            _textureToggle = GUILayout.Toggle(_textureToggle, "Use Texture");
            _textureToggle = GUILayout.Toggle(!_textureToggle, "Use Color");
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("");
            _color = EditorGUILayout.ColorField(_color);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            // Apply Albedo label above the other labels.
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Albedo (RGB)", GUILayout.Width(90.0f));
            _albedoTexture = (Texture2D)EditorGUILayout.ObjectField("", _albedoTexture, typeof(Texture2D), false, GUILayout.Width(90.0f));
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Normal", GUILayout.Width(90.0f));
            _normalTexture = (Texture2D)EditorGUILayout.ObjectField("", _normalTexture, typeof(Texture2D), false, GUILayout.Width(90.0f));
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(15.0f);

            // Create and position the Texture selector boxes horizontally next to each other.
            GUILayout.BeginHorizontal();
            GUILayout.Space(10.0f);
            GUILayout.EndHorizontal();

            // Leave a 10.0f pixel space for the next components.
            GUILayout.Space(10.0f);

            // Add the metallic slider.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Metallic", GUILayout.Width(73.0f));
            EditorGUI.BeginChangeCheck();
            _metallic = EditorGUILayout.Slider(_metallic, 0.0f, 1.0f);
            EditorGUI.EndChangeCheck();
            GUILayout.EndHorizontal();

            // Leave a 10.0f pixel space for the next components.
            GUILayout.Space(10.0f);

            // Add the smoothness slider.
            GUILayout.BeginHorizontal();
            GUILayout.Label("Smoothness", GUILayout.Width(73.0f));
            _smoothness = EditorGUILayout.Slider(_smoothness, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            // Leave a 10.0f pixel space for the next components.
            GUILayout.Space(10.0f);

            // Create the 2x2 grid for adding tileSize and tileOffset.
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Space(20.0f);
            GUILayout.Label("x", GUILayout.Width(10.0f));
            GUILayout.Label("y", GUILayout.Width(10.0f));
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Size");
            _tileSize.x = EditorGUILayout.FloatField(_tileSize.x);
            _tileSize.y = EditorGUILayout.FloatField(_tileSize.y);
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            GUILayout.Label("Offset");
            _tileOffset.x = EditorGUILayout.FloatField(_tileOffset.x);
            _tileOffset.y = EditorGUILayout.FloatField(_tileOffset.y);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            if (_editTexture)
            {

                if (_textureToggle)
                {
                    _customInspector.UpdateSplatMapProperties(_metallic, _smoothness, _tileSize, _tileOffset, TextureGenerator.GenerateTexture2D(_color, 1, 1), _normalTexture);
                }
                else
                {
                    _customInspector.UpdateSplatMapProperties(_metallic, _smoothness, _tileSize, _tileOffset, _albedoTexture, _normalTexture);
                }
            }

            // Create and position the Apply button.
            if (GUI.Button(new Rect(this.position.width - 105.0f, this.position.height - 25.0f, 100.0f, 20.0f), "Apply"))
            {
                // Close the window.
                Apply();
                this.Close();
            }

        }

        void Apply()
        {
            //  If we're not in edit texture mode, then clicking apply will add a new texture.
            if (!_editTexture)
            {
                if (!_textureToggle && _albedoTexture != null)
                {
                    _customInspector.AddHeatmapSplatPrototype(_customInspector.GenerateHeatmapSplatprototype(_albedoTexture, _normalTexture, _metallic, _smoothness, _tileOffset, _tileSize));
                }
                else _customInspector.AddHeatmapSplatPrototype(_customInspector.GenerateHeatmapSplatprototype(_color, _normalTexture, _metallic, _smoothness, _tileOffset, _tileSize));


            }

            // If we're in edit mode, clicking apply will update the selected texture.
            else
            {

                if (!_textureToggle && _albedoTexture != null)
                {
                    _customInspector.UpdateSelectedSplatPrototype(_customInspector.GenerateHeatmapSplatprototype(_albedoTexture, _normalTexture, _metallic, _smoothness, _tileOffset, _tileSize));
                }
                else _customInspector.UpdateSelectedSplatPrototype(_customInspector.GenerateHeatmapSplatprototype(_color, _normalTexture, _metallic, _smoothness, _tileOffset, _tileSize));

            }
        }
    }
}
#endif