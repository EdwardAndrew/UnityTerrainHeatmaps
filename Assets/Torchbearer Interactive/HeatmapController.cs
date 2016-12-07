﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TBUnityLib.TextureTools;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TerrainHeatmap
{
    [RequireComponent(typeof(Terrain))]
    [RequireComponent(typeof(TerrainCollider))]
    public class HeatmapController : MonoBehaviour
    {


        public Terrain referenceTerrainObject;

        [SerializeField]
        Terrain _terrain;

        [SerializeField]
        TerrainData _terrainData;

        public int AlphaMapResolution
        {
            get
            {
                if (_terrain != null && _terrainData != null) return _terrainData.alphamapResolution;
                else return 0;
            }
        }

        [SerializeField]
        bool _displayHeatmap;

        public bool DisplayHeatmap
        {
            get { return _displayHeatmap; }
            set { _displayHeatmap = value; }
        }

        bool _displayDataPointGizmos = false;
        public bool DisplayDataPointGizmos
        {
            get { return _displayDataPointGizmos; }
            set { _displayDataPointGizmos = value; }
        }

        Color _dataPointGizmosColor = Color.green;
        public Color DataPointGizmoColor
        {
            get { return _dataPointGizmosColor; }
            set { _dataPointGizmosColor = value; }
        }

        [SerializeField]
        bool _hasInitialised = false;
        public bool HasInitialised
        {
            get { return _hasInitialised; }
        }

        int _selectedHeatmapIndex = 0;

        public int SelectedHeatmapIndex
        {
            get { return _selectedHeatmapIndex; }
            set { _selectedHeatmapIndex = value; }
        }

        List<Heatmap> _heatmaps;
        public Heatmap SelectedHeatmap
        {
            get { return _heatmaps[SelectedHeatmapIndex]; }
        }


        public int HeatmapCount
        {
            get
            {

                if (_heatmaps != null) return _heatmaps.Count;
                else return 0;
            }
        }

        public SplatPrototype[] Splatprototypes
        {
            get { return _terrainData.splatPrototypes; }
            set { _terrainData.splatPrototypes = value; } 
        }


        public void Initialise()
        {
            _heatmaps = new List<Heatmap>();
            _heatmaps.Add(new Heatmap("Generated Default Heatmap"));
            _hasInitialised = false;
        }


        // Use this for initialization
        void Start()
        {
            if (HasInitialised == false) Initialise();
        }

        // Update is called once per frame
        void Update()
        {

        }


        public void EditorUpdate()
        {

        }

        public void ToggleDisplayReferenceObject(bool showObject = false)
        {
            GetComponent<Terrain>().enabled = showObject;
            referenceTerrainObject.GetComponent<Terrain>().enabled = !showObject;
        }

        public void RefreshReferenceTerrainObject()
        {
            if (referenceTerrainObject == null) return;

            this.transform.position = referenceTerrainObject.transform.position;
            this.transform.rotation = referenceTerrainObject.transform.rotation;
            this.transform.localScale = referenceTerrainObject.transform.localScale;

            _terrain = GetComponent<Terrain>();

            _terrainData = referenceTerrainObject.terrainData != null ? new TerrainData() : null;

            if (_terrainData != null)
            {
                _terrainData.heightmapResolution = referenceTerrainObject.terrainData.heightmapResolution;
                _terrainData.SetHeights(0, 0, referenceTerrainObject.terrainData.GetHeights(0, 0, referenceTerrainObject.terrainData.heightmapResolution, referenceTerrainObject.terrainData.heightmapResolution));


                _terrainData.size = referenceTerrainObject.terrainData.size;
                _terrain.GetComponent<TerrainCollider>().terrainData = _terrainData;
                _terrain.terrainData = _terrainData;
            }
        }


    }

#if UNITY_EDITOR
    [CustomEditor(typeof(HeatmapController))]
    public class HeatmapControllerCustomInspector : Editor
    {
        HeatmapController _heatmapController;
        bool _isSceneViewUpdateRequired = false;
        int _selectedTextureGrid = 0;
        CustomTextureSettingsWindow _tWindow;

        void OnEnable()
        {
            _heatmapController = target is HeatmapController ? target as HeatmapController : null;

            if (_heatmapController != null)
            {
                if (_heatmapController.HasInitialised == false) _heatmapController.Initialise();
            }
        }


        public override void OnInspectorGUI()
        {
            ReferenceTerrainObjectField();

            DisplayReferenceTerrainObjectToggle();
            DisplayDataPointGizmos();

            SelectHeatmapButtons();

            HorizontalLine();

            HeatmapDataGUI();

            HorizontalLine();

            AddRemoveHeatmapButtons();

            UpdateSceneViewIfNeeded();

        }

        void ReferenceTerrainObjectField()
        {
            EditorGUI.BeginChangeCheck();
            _heatmapController.referenceTerrainObject = (Terrain)EditorGUILayout.ObjectField("Reference Terrain", _heatmapController.referenceTerrainObject, typeof(Terrain), true);
            EditorGUI.EndChangeCheck();
            if (GUI.changed) _heatmapController.RefreshReferenceTerrainObject();
        }

        void DisplayReferenceTerrainObjectToggle()
        {
            EditorGUI.BeginChangeCheck();
            _heatmapController.DisplayHeatmap = GUILayout.Toggle(_heatmapController.DisplayHeatmap, "Display Heatmap");
            EditorGUI.EndChangeCheck();
            if (GUI.changed) _heatmapController.ToggleDisplayReferenceObject(_heatmapController.DisplayHeatmap);
        }

        void SelectHeatmapButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Selected Heatmap");

            if (GUILayout.Button("<", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmapIndex--;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("" + (_heatmapController.SelectedHeatmapIndex + 1), GUILayout.Width(20.0f + 10.0f * (_heatmapController.HeatmapCount / 10)));
            EditorGUI.EndChangeCheck();

            if (GUILayout.Button(">", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmapIndex++;
            }

            GUILayout.Label(" of " + _heatmapController.HeatmapCount, GUILayout.Width(40.0f));

            if (GUI.changed)
            {
                TBUnityLib.Generic.BoundedType<int> boundHeatmapIndex = new TBUnityLib.Generic.BoundedType<int>(_heatmapController.SelectedHeatmapIndex, 0, _heatmapController.HeatmapCount - 1);
                _heatmapController.SelectedHeatmapIndex = boundHeatmapIndex.Value;
            }


            EditorGUILayout.EndHorizontal();
        }

        void HorizontalLine()
        {
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        }

        void HeatmapDataGUI()
        {
            DisplayHeatmapName();
            DisplayHeatmapResolution();
            DisplayHeatmapInterpolationMode();
            DisplayHeatmapDataSource();
            DisplayHeatmapTextures();
        }

        void AddRemoveHeatmapButtons()
        {
            bool isLastHeatmap = _heatmapController.HeatmapCount >= 1 ? false : true;

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Heatmap"))
            {

            }
            GUILayout.Space(30.0f);
            GUI.enabled = isLastHeatmap;
            if (GUILayout.Button("Remove Heatmap"))
            {

            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        void DisplayHeatmapInterpolationMode()
        {
            _heatmapController.SelectedHeatmap.interpolationMode = (InterpolationMode)EditorGUILayout.EnumPopup("Interpolation Mode", _heatmapController.SelectedHeatmap.interpolationMode);
        }

        void DisplayDataPointGizmos()
        {
            EditorGUILayout.BeginHorizontal();
            _heatmapController.DisplayDataPointGizmos = EditorGUILayout.ToggleLeft("Display Heatmap Data Points", _heatmapController.DisplayDataPointGizmos);
            if (_heatmapController.DisplayDataPointGizmos)
            {
                _heatmapController.DataPointGizmoColor = EditorGUILayout.ColorField(_heatmapController.DataPointGizmoColor);
            }
            EditorGUILayout.EndHorizontal();
        }

        void DisplayHeatmapDataSource()
        {
            _heatmapController.SelectedHeatmap.dataType = (HeatmapData)EditorGUILayout.EnumPopup("Data Source", _heatmapController.SelectedHeatmap.dataType);
            if (_heatmapController.SelectedHeatmap.dataType == HeatmapData.Custom)
            {
                _heatmapController.SelectedHeatmap.filter = EditorGUILayout.DelayedTextField("Custom Data Filter", _heatmapController.SelectedHeatmap.filter);
            }
        }

        void DisplayHeatmapTextures()
        {
            _heatmapController.SelectedHeatmap.texSource = (TextureSource)EditorGUILayout.EnumPopup("Texture Source", _heatmapController.SelectedHeatmap.texSource);
            if (_heatmapController.SelectedHeatmap.texSource == TextureSource.Custom)
            {
                int gridSquareSize = 60;

                GUIStyle gridStyling = new GUIStyle();

                int gridRowCount = Mathf.FloorToInt(EditorGUIUtility.currentViewWidth / gridSquareSize);
                gridStyling.fixedHeight = gridSquareSize;
                gridStyling.fixedWidth = gridSquareSize;
                gridStyling.padding = new RectOffset(4, 4, 4, 4);
                gridStyling.onNormal.background = TextureGenerator.GenerateTexture2D(new Color(1.0f, 0.4f, 0.0f), gridSquareSize, gridSquareSize);
                gridStyling.stretchWidth = false;
                gridStyling.stretchHeight = false;
                gridStyling.alignment = TextAnchor.MiddleCenter;
                gridStyling.clipping = TextClipping.Overflow;
                gridStyling.normal.background = TextureGenerator.GenerateTexture2D(new Color(0.17f, 0.17f, 0.17f), gridSquareSize, gridSquareSize);

                GUI.skin.customStyles[0] = gridStyling;


                _selectedTextureGrid = GUILayout.SelectionGrid(_selectedTextureGrid, GetSplatPrototypesAsTexture2DArray(_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps),gridRowCount, GUI.skin.customStyles[0]);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Texture"))
                {
                    if (_tWindow != null) _tWindow.Close();
                    _tWindow = CustomTextureSettingsWindow.Setup(this);
                    _tWindow.ShowAddLayer();
                }
                if (GUILayout.Button("Edit Texture"))
                {
                    if (_tWindow != null) _tWindow.Close();
                    _tWindow = CustomTextureSettingsWindow.Setup(this);
                    _tWindow.ShowEditLayer(_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps[_selectedTextureGrid]);
                }
                if (GUILayout.Button("Remove Texture"))
                {
                    if(_heatmapController.SelectedHeatmap.texSource == TextureSource.Custom)
                    {
                        if(_selectedTextureGrid >= 0 && _selectedTextureGrid < _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Count)
                        {
                            _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.RemoveAt(_selectedTextureGrid);
                        }
                    }
                }
                EditorGUILayout.EndHorizontal(); 
            }
        }

        void DisplayHeatmapName()
        {
            _heatmapController.SelectedHeatmap.name = EditorGUILayout.DelayedTextField("Name", _heatmapController.SelectedHeatmap.name);
        }

        void DisplayHeatmapResolution()
        {
            bool resolutionUpdated = false;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Resolution");
            if (GUILayout.Button("<", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmap.heatmapResolution /= 2;
                if (_heatmapController.SelectedHeatmap.heatmapResolution < 1) _heatmapController.SelectedHeatmap.heatmapResolution = 1;
                resolutionUpdated = true;
            }

            EditorGUI.BeginChangeCheck();
            _heatmapController.SelectedHeatmap.heatmapResolution = EditorGUILayout.DelayedIntField(_heatmapController.SelectedHeatmap.heatmapResolution, GUILayout.Width(50.0f));
            EditorGUI.EndChangeCheck();
            if (GUI.changed)
            {
                resolutionUpdated = true;
            }

            if (GUILayout.Button(">", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmap.heatmapResolution *= 2;
                resolutionUpdated = true;
            }

            if (resolutionUpdated)
            {
                for (int i = 1; i <= _heatmapController.AlphaMapResolution; i *= 2)
                {
                    if (_heatmapController.SelectedHeatmap.heatmapResolution == i) break;
                    if (_heatmapController.SelectedHeatmap.heatmapResolution < i) _heatmapController.SelectedHeatmap.heatmapResolution = i / 2;
                }

                TBUnityLib.Generic.BoundedType<int> boundHeatmapResolution = new TBUnityLib.Generic.BoundedType<int>(_heatmapController.SelectedHeatmap.heatmapResolution, 1, _heatmapController.AlphaMapResolution);
                _heatmapController.SelectedHeatmap.heatmapResolution = boundHeatmapResolution.Value;

            }



            EditorGUILayout.EndHorizontal();
        }

        void UpdateSceneViewIfNeeded()
        {
            if (_isSceneViewUpdateRequired) SceneView.RepaintAll();
        }


        Texture2D[] GetSplatPrototypesAsTexture2DArray(List<HeatmapSplatprototype> splatPrototypes)
        {
            if (splatPrototypes == null) return null;

            List<Texture2D> texture2DArray = new List<Texture2D>();

            foreach(HeatmapSplatprototype splatPrototype in splatPrototypes)
            {
                if (splatPrototype is TextureSplatMap)
                {
                    texture2DArray.Add(((TextureSplatMap)splatPrototype).texture);
                }
                else
                {
                    texture2DArray.Add(TextureGenerator.GenerateTexture2D(((ColorSplatMap)splatPrototype).color, 40, 40));
                }
            }

            return texture2DArray.ToArray();
        }

        public void UpdateSplatMapProperties(float _metallic, float _smoothness, Vector2 _tileSize, Vector2 _tileOffset, Texture2D _textureMap, Texture2D _normalMap)
        {
            SplatPrototype[] splatMaps = _heatmapController.Splatprototypes;

            if (_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Count != splatMaps.Length)
            {
                return;
            }
            if (!_heatmapController.DisplayHeatmap)
            {
                return;
            }

            if (_textureMap == null) return;

            splatMaps[_selectedTextureGrid].metallic = _metallic;
            splatMaps[_selectedTextureGrid].smoothness = _smoothness;
            splatMaps[_selectedTextureGrid].tileOffset = _tileOffset;
            splatMaps[_selectedTextureGrid].tileSize = _tileSize;
            splatMaps[_selectedTextureGrid].normalMap = _normalMap;
            splatMaps[_selectedTextureGrid].texture = _textureMap;

            _heatmapController.Splatprototypes = splatMaps;
        }

        public void AddHeatmapSplatPrototype(HeatmapSplatprototype newHeatmapSplatprototype)
        {
            _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Add(newHeatmapSplatprototype);
        }

        public HeatmapSplatprototype GenerateHeatmapSplatprototype(Texture2D _albedo, Texture2D _normal, float _metallic, float _smoothness, Vector2 _tileOffset, Vector2 _tileSizing)
        {
            var returnSplatPrototype = new TextureSplatMap();

            returnSplatPrototype.texture = _albedo;
            returnSplatPrototype.normalMap = _normal;
            returnSplatPrototype.metallic = _metallic;
            returnSplatPrototype.smoothness = _smoothness;
            returnSplatPrototype.tileOffset = _tileOffset;
            returnSplatPrototype.tileSizing = _tileSizing;

            return returnSplatPrototype;
        }

        public HeatmapSplatprototype GenerateHeatmapSplatprototype(Color _color, Texture2D _normal, float _metallic, float _smoothness, Vector2 _tileOffset, Vector2 _tileSizing)
        {
            var returnSplatPrototype = new ColorSplatMap();

            returnSplatPrototype.color = _color;
            returnSplatPrototype.normalMap = _normal;
            returnSplatPrototype.metallic = _metallic;
            returnSplatPrototype.smoothness = _smoothness;
            returnSplatPrototype.tileOffset = _tileOffset;
            returnSplatPrototype.tileSizing = _tileSizing;

            return returnSplatPrototype;
        }
    }
#endif
}