using UnityEngine;
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
        /// <summary>
        /// The reference Terrain object.
        /// </summary>
        public Terrain referenceTerrainObject;

        /// <summary>
        /// The HeatmapController's Terrain object.
        /// </summary>
        [SerializeField]      
        Terrain _terrain;

        /// <summary>
        /// The HeatmapController's TerrainData.
        /// </summary>
        [SerializeField]
        TerrainData _terrainData;

        /// <summary>
        /// True if the HeatmapController has been initialised.
        /// </summary>
        [SerializeField]
        bool _initialised = false;

        /// <summary>
        /// The HeatmapController's HeatmapGenerator object.
        /// </summary>
        HeatmapGenerator Generator;

        /// <summary>
        /// True if RealTimeEditor updates have been initialised.
        /// </summary>
        bool _editorUpdateInitialised = false;

        /// <summary>
        /// The HeatmapController's RealTimeUpdate Coroutine.
        /// </summary>
        IEnumerator _realtimeHeatmapUpdate;

        /// <summary>
        /// The time of the HeatmapController's last RealTimeUpdate.
        /// </summary>
        float _lastRealTimeUpdate = 0.0f;

        /// <summary>
        /// How long the HeatmapController should wait while processing a thread before it times out.
        /// </summary>
        float _realTimeUpdateThreadTimeout = 3.0f;

        /// <summary>
        /// Gets or sets a value indicating whether [editor update initialised].
        /// </summary>
        /// <value>
        /// <c>true</c> if [editor update initialised]; otherwise, <c>false</c>.
        /// </value>
        public bool EditorUpdateInitialised
        {
            get { return _editorUpdateInitialised; }
            set { _editorUpdateInitialised = value; }
        }

        /// <summary>
        /// Returns true if Real Time Updates are Enabled.
        /// </summary>
        [SerializeField]
        bool _realTimeUpdateEnabled = false;
        /// <summary>
        /// Gets or sets a value indicating whether [real time update enabled].
        /// </summary>
        /// <value>
        /// <c>true</c> if [real time update enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool RealTimeUpdateEnabled
        {
            get { return _realTimeUpdateEnabled; }
            set { _realTimeUpdateEnabled = value; } 
        }
        /// <summary>
        /// Returns true if Real Time Updates are enabled in editor mode.
        /// </summary>
        [SerializeField]
        bool _realTimeEditorUpdateEnabled = false;

        /// <summary>
        /// Gets or sets a value indicating whether [real time editor update enabled].
        /// </summary>
        /// <value>
        /// <c>true</c> if [real time editor update enabled]; otherwise, <c>false</c>.
        /// </value>
        public bool RealTimeEditorUpdateEnabled
        {
            get { return _realTimeEditorUpdateEnabled; }
            set { _realTimeEditorUpdateEnabled = value; }
        }

        /// <summary>
        /// Multiplier for the number of realTimeUpdateChunks.
        /// </summary>
        [SerializeField]
        int _realTimeUpdateChunksMultiplier = 2;
        /// <summary>
        /// Gets or sets the real time update chunks multiplier.
        /// </summary>
        /// <value>
        /// The real time update chunks multiplier.
        /// </value>
        public int RealTimeUpdateChunksMultiplier
        {
            get { return _realTimeUpdateChunksMultiplier; }
            set { 
                    
                    for(int i = 1, previousI = 1; i < 2048; previousI = i,i*=2)
                    {
                        if( i == value ) 
                        {
                            _realTimeUpdateChunksMultiplier = value;
                            return;
                        }
                        else if( value < i )
                        {
                            _realTimeUpdateChunksMultiplier = previousI;
                            return;
                        }
                    }
                
                 _realTimeUpdateChunksMultiplier = 2048; 

            }
        }
        /// <summary>
        /// The time in seconds between each Real Time Update.
        /// </summary>
        [SerializeField]
        float _realTimeUpdateInterval = 0.5f;
        /// <summary>
        /// Gets or sets the real time update interval.
        /// </summary>
        /// <value>
        /// The real time update interval.
        /// </value>
        public float RealTimeUpdateInterval
        {
            get { return _realTimeUpdateInterval; }
            set { _realTimeUpdateInterval = Mathf.Abs(value); }
        }

        public int AlphaMapResolution
        {
            get
            {
                if (_terrain != null && _terrainData != null) return _terrainData.alphamapResolution;
                else return 0;
            }
        }
        /// <summary>
        /// If true, the Heatmap will be displayed and the reference Terrain object will be hidden.
        /// </summary>
        [SerializeField]
        bool _displayHeatmap;

        /// <summary>
        /// Gets or sets a value indicating whether [display heatmap].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [display heatmap]; otherwise, <c>false</c>.
        /// </value>
        public bool DisplayHeatmap
        {
            get { return _displayHeatmap; }
            set { _displayHeatmap = value; }
        }

        /// <summary>
        /// If true, a gizmo is displayed in the location of each data point.
        /// </summary>
        [SerializeField]
        bool _displayDataPointGizmos = false;
        /// <summary>
        /// Gets or sets a value indicating whether [display data point gizmos].
        /// </summary>
        /// <value>
        /// <c>true</c> if [display data point gizmos]; otherwise, <c>false</c>.
        /// </value>
        public bool DisplayDataPointGizmos
        {
            get { return _displayDataPointGizmos; }
            set { _displayDataPointGizmos = value; }
        }

        /// <summary>
        /// The color of the data point gizmos.
        /// </summary>
        [SerializeField]
        Color _dataPointGizmosColor = Color.green;
        /// <summary>
        /// Gets or sets the color of the data point gizmos.
        /// </summary>
        /// <value>
        /// The color of the data point gizmos.
        /// </value>
        public Color DataPointGizmoColor
        {
            get { return _dataPointGizmosColor; }
            set { _dataPointGizmosColor = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="HeatmapController"/> is initialised.
        /// </summary>
        /// <value>
        ///   <c>true</c> if initialised; otherwise, <c>false</c>.
        /// </value>
        public bool Initialised
        {
            get { return _initialised; }
        }

        /// <summary>
        /// The seledcted Heatmap index.
        /// </summary>
        [SerializeField]
        int _selectedHeatmapIndex = 0;

        /// <summary>
        /// Gets or sets the index of the <see cref="_selectedHeatmapIndex"/> 
        /// </summary>
        /// <value>
        /// The index of the selected heatmap.
        /// </value>
        public int SelectedHeatmapIndex
        {
            get { return _selectedHeatmapIndex; }
            set { _selectedHeatmapIndex = value; }
        }

        /// <summary>
        /// This <see cref="HeatmapController"/>'s Heatmaps.
        /// </summary>
        [SerializeField]
        List<Heatmap> _heatmaps;
        /// <summary>
        /// Gets the selected <see cref="Heatmap"/>.
        /// </summary>
        /// <value>
        /// The selected <see cref="Heatmap"/>.
        /// </value>
        public Heatmap SelectedHeatmap
        {
            get { return  _heatmaps != null ? _heatmaps[SelectedHeatmapIndex] : null; }
        }

        /// <summary>
        /// Gets number of <see cref="Heatmap"/> objects in the <see cref="_heatmaps"/> <see cref="List{T}"/>.
        /// </summary>
        /// <value>
        /// The heatmap count.
        /// </value>
        public int HeatmapCount
        {
            get
            {
                if (_heatmaps != null) return _heatmaps.Count;
                else return 0;
            }
        }

        /// <summary>
        /// Gets or sets this <see cref="HeatmapController"/>'s <see cref="TerrainData.splatPrototypes"/> array.
        /// </summary>
        /// <value>
        /// The <see cref="TerrainData.splatPrototypes"/>.
        /// </value>
        public SplatPrototype[] Splatprototypes
        {
            get { return _terrainData.splatPrototypes; }
            set { _terrainData.splatPrototypes = value; } 
        }

        /// <summary>
        /// Initialises this instance of the <see cref="HeatmapController"/>.
        /// </summary>
        public void Initialise()
        {
            _heatmaps = new List<Heatmap>();
            AddNewHeatmap();

            GenerateHeatmap();

            _initialised = true;
        }

        /// <summary>
        /// Adds a new <see cref="Heatmap"/>.
        /// </summary>
        public void AddNewHeatmap()
        {
            _heatmaps.Add(new Heatmap("Generated Default Heatmap"));
        }

        /// <summary>
        /// Removes the currently selected <see cref="Heatmap"/> from the <see cref="_heatmaps"/> <see cref="List{T}"/>.
        /// </summary>
        public void RemoveSelectedHeatmap()
        {
            _heatmaps.RemoveAt(SelectedHeatmapIndex);
            SelectedHeatmapIndex = SelectedHeatmapIndex >= _heatmaps.Count ? _heatmaps.Count - 1 : SelectedHeatmapIndex;
        }

        /// <summary>
        /// Generates the currently selected <see cref="Heatmap"/>.
        /// </summary>
        public void GenerateHeatmap()
        {

            Generator = Generator == null ? new HeatmapGenerator() : Generator;

            Terrain t = GetComponent<Terrain>();

            Generator.GenerateHeatMap(SelectedHeatmapIndex, ref _heatmaps, t, GetCustomDataPointArray(), this.transform.position);

            t.terrainData.splatPrototypes = SelectedHeatmap.splatPrototypes;
            t.terrainData.SetAlphamaps(0, 0, SelectedHeatmap.alphaMapData);
        }

        /// <summary>
        /// Returns the Custom Data Points of the currently selected <see cref="Heatmap"/> after it's <see cref="Heatmap.filter"/> has been applied.
        /// </summary>
        /// <returns><see cref="HeatmapNode[]"/></returns>
        HeatmapNode[] GetCustomDataPointArray()
        {
            HeatmapNode[] customDataPoints = FindObjectsOfType<HeatmapNode>();
            List<HeatmapNode> returnedPoints = new List<HeatmapNode>();

            for (int i = 0; i < customDataPoints.Length; i++)
            {
                if (customDataPoints[i].filter == SelectedHeatmap.filter) returnedPoints.Add(customDataPoints[i]);
            }

            foreach(HeatmapNode node in returnedPoints)
            {
                node.UpdatePosition();
            }

            return returnedPoints.ToArray();
        }


        /// <summary>
        /// Unity Default Start method, used for initialisation.
        /// </summary>
        void Start()
        {
            if (Initialised == false) Initialise();
        }

        /// <summary>
        /// Updates called once per frame.
        /// </summary>
        void Update()
        {
            if (_realTimeUpdateEnabled)
            {
                if (_realtimeHeatmapUpdate == null) _realtimeHeatmapUpdate = RealTimeHeatmapUpdate();
                _realtimeHeatmapUpdate.MoveNext(); 
            }
            CheckActiveInactiveTerrainComponents();
        }

        /// <summary>
        /// Editor Update.
        /// </summary>
        public void EditorUpdate()
        {
            if (_realTimeEditorUpdateEnabled)
            {
                if (_realtimeHeatmapUpdate == null) _realtimeHeatmapUpdate = RealTimeHeatmapUpdate();
                _realtimeHeatmapUpdate.MoveNext(); 
            }
            CheckActiveInactiveTerrainComponents();
        }

        /// <summary>
        /// Checks the <see cref="HeatmapController"/> and the <see cref="referenceTerrainObject"/>'s Terrain components and toggles between them depending on if <see cref="DisplayHeatmap"/> is true or false.
        /// </summary>
        void CheckActiveInactiveTerrainComponents()
        {
            if (this == null) return;
            if (GetComponent<Terrain>() == null) return;
            if (referenceTerrainObject == null) return;
            
                        
            GetComponent<Terrain>().enabled = DisplayHeatmap;
            referenceTerrainObject.GetComponent<Terrain>().enabled = !DisplayHeatmap;
        }

        /// <summary>
        /// Refreshes the reference terrain object and the <see cref="HeatmapController"/>'s <see cref="_terrainData"/>.
        /// </summary>
        public void RefreshReferenceTerrainObject()
        {
            if (referenceTerrainObject == null) return;

            this.transform.position = referenceTerrainObject.transform.position;
            this.transform.rotation = referenceTerrainObject.transform.rotation;
            this.transform.localScale = referenceTerrainObject.transform.localScale;

            _terrain = GetComponent<Terrain>();

            _terrainData = _terrain.terrainData != null ? _terrain.terrainData : new TerrainData();

            if (referenceTerrainObject.terrainData != null)
            {
                _terrainData.heightmapResolution = referenceTerrainObject.terrainData.heightmapResolution;
                _terrainData.SetHeights(0, 0, referenceTerrainObject.terrainData.GetHeights(0, 0, referenceTerrainObject.terrainData.heightmapResolution, referenceTerrainObject.terrainData.heightmapResolution));
                _terrainData.size = referenceTerrainObject.terrainData.size;

                _terrain.GetComponent<TerrainCollider>().terrainData = _terrainData;
                _terrain.terrainData = _terrainData;

            }
        }

        /// <summary>
        /// Unity default Gizmo drawing method.
        /// </summary>
        private void OnDrawGizmos()
        {
            if(DisplayHeatmap && DisplayDataPointGizmos)
            {
                Gizmos.color = DataPointGizmoColor;
                Terrain t = GetComponent<Terrain>();
                int heatMapResolution = SelectedHeatmap.heatmapResolution;
                float mapDataDistanceX = t.terrainData.size.x / heatMapResolution;
                float mapDataDistanceZ = t.terrainData.size.z / heatMapResolution;

                for (int x = 0; x <= heatMapResolution; x++)
                {
                    for(int z = 0; z <= heatMapResolution; z++)
                    {
                        Vector3 gizmoPosition = new Vector3(x * mapDataDistanceX, 0.0f, z * mapDataDistanceZ);
                        gizmoPosition.y = t.SampleHeight(gizmoPosition);

                        Gizmos.DrawWireCube(gizmoPosition + this.transform.position, Vector3.one);                   
                    }
                }
            }
        }

        /// <summary>
        /// Realtime <see cref="Heatmap"/> co-routine.
        /// </summary>
        /// <returns></returns>
        IEnumerator RealTimeHeatmapUpdate()
        {
            while(true) 
            {
                bool isEditorMode = false;
                bool isUpdateEnabled = false;

#if UNITY_EDITOR
                    isEditorMode = true;
#endif

                if (isEditorMode) 
                {
                #if UNITY_EDITOR
                    if (EditorApplication.isPlaying && RealTimeUpdateEnabled) isUpdateEnabled = true;
                else if (isEditorMode && ((!EditorApplication.isPlaying) && RealTimeEditorUpdateEnabled)) isUpdateEnabled = true;
                #endif
                }
                else if (isEditorMode == false && RealTimeUpdateEnabled) isUpdateEnabled = true;

                if(isUpdateEnabled)
                {
                    bool hasUpdatedIntervalElapsed = false;

                    if(isEditorMode)
                    {
#if UNITY_EDITOR
                        if (isEditorMode && EditorApplication.isPlaying) hasUpdatedIntervalElapsed = Time.time - _lastRealTimeUpdate >= RealTimeUpdateInterval ? true : false;
                        if (isEditorMode && !EditorApplication.isPlaying) hasUpdatedIntervalElapsed = Time.realtimeSinceStartup - _lastRealTimeUpdate >= RealTimeUpdateInterval ? true : false; 
#endif
                    }
                    else
                    {
                        hasUpdatedIntervalElapsed = Time.time - _lastRealTimeUpdate >= RealTimeUpdateInterval ? true : false;
                    }

                    if(hasUpdatedIntervalElapsed)
                    {
                        Generator = Generator == null ? new HeatmapGenerator() : Generator;

                        int chunkSize = (int)(_terrainData.alphamapResolution / RealTimeUpdateChunksMultiplier);
                        int RTChunks = RealTimeUpdateChunksMultiplier;
                        int splatPrototypeLength = _terrainData.splatPrototypes.Length;
                        HeatmapNode[] customDataArray = null;
                        int selectedHeatmap = SelectedHeatmapIndex;

                        if (SelectedHeatmap.dataType == HeatmapData.Custom)
                        {
                            customDataArray = GetCustomDataPointArray();
                        }
                        for (int x = 0; x < RTChunks; x++)
                        {
                            for (int y = 0; y < RTChunks; y++)
                            {
                                if (RTChunks != RealTimeUpdateChunksMultiplier) continue;
                                
                                if (GetComponent<HeatmapController>() == null) break;
                                float threadStartTime = Time.realtimeSinceStartup;
                                bool abortLoop = false;

                                if (SelectedHeatmap.splatPrototypes == null || (splatPrototypeLength != SelectedHeatmap.splatPrototypes.Length))
                                {
                                    GenerateHeatmap();
                                }
                                splatPrototypeLength = SelectedHeatmap.splatPrototypes.Length;


                                Vector3 position = transform.position;
                                Generator.ProcessHeatMapThreaded(selectedHeatmap, ref _heatmaps, _terrainData.GetHeights(0, 0, _terrainData.heightmapResolution, _terrainData.heightmapResolution), _terrainData.alphamapResolution, _terrainData.size, _terrain.terrainData.heightmapScale, y * chunkSize, x * chunkSize, chunkSize, chunkSize, customDataArray, position);

                                while (Generator.isGenerateHeatMapThreadFinished == false)
                                {
                                    if (Time.realtimeSinceStartup - threadStartTime >= _realTimeUpdateThreadTimeout)
                                    {
                                        Generator.Abort();
                                        abortLoop = true;
                                        break;
                                    }
                                    yield return null;
                                }

                                if (abortLoop) continue;
                                if (!DisplayHeatmap) { continue; }

                                if (SelectedHeatmap.splatPrototypes.Length != splatPrototypeLength) { continue; }
                                if (SelectedHeatmap.splatPrototypes.Length != Generator.processedAlphaMap.GetLength(2)) { continue; }
                                if (selectedHeatmap != SelectedHeatmapIndex) { continue; }

                                _terrain.terrainData.splatPrototypes = SelectedHeatmap.splatPrototypes;
                                _terrain.terrainData.SetAlphamaps(x * chunkSize, y * chunkSize, Generator.processedAlphaMap);

                                while(Time.realtimeSinceStartup - threadStartTime < RealTimeUpdateInterval)
                                {
                                    yield return null;
                                }

                                if (SelectedHeatmapIndex != selectedHeatmap) break;
                            }
                            if (SelectedHeatmapIndex != selectedHeatmap) break;
                        }


                         _lastRealTimeUpdate =  Time.time;
#if UNITY_EDITOR
                        if (isEditorMode && !EditorApplication.isPlaying) _lastRealTimeUpdate = Time.realtimeSinceStartup; 
#endif

                    }

                }
                else
                {
                    if (isEditorMode) yield return new WaitForSecondsRealtime(2.0f);
                    else if (isEditorMode == false) yield return new WaitForSeconds(2.0f);
                }
                yield return null;
            }
        }

        /// <summary>
        /// Stop co-routines and remove delegates.
        /// </summary>
        private void OnDestroy()
        {
            DisableEditorUpdates();
            StopAllCoroutines();
        }

        /// <summary>
        /// Remove EditorUpdate delegate from EditorApplication.update.
        /// </summary>
        void DisableEditorUpdates()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= EditorUpdate;
            #endif
        }


    }

    /// <summary>
    /// <see cref="HeatmapController"/> Custom Editor Inspector.
    /// </summary>
#if UNITY_EDITOR
    [CustomEditor(typeof(HeatmapController))]
    public class HeatmapControllerCustomInspector : Editor
    {
        HeatmapController _heatmapController;
        bool _isSceneViewUpdateRequired = false;
        int _selectedTextureGrid = 0;
        CustomTextureSettingsWindow _tWindow;

        static Texture s_tbLogo;

        /// <summary>
        /// Unity Editor Inspector OnEnable() method.
        /// </summary>
        void OnEnable()
        {
            if (s_tbLogo == null) s_tbLogo = ((Texture)EditorGUIUtility.Load("Torchbearer Interactive/TBLogo.png"));

            _heatmapController = target is HeatmapController ? target as HeatmapController : null;

            if (_heatmapController && _heatmapController.Initialised == false)
            {
                _heatmapController.Initialise();

            }
            if (_heatmapController && _heatmapController.EditorUpdateInitialised == false)
            {
                EditorApplication.update += _heatmapController.EditorUpdate;
                _heatmapController.EditorUpdateInitialised = true;
            }
        }

        /// <summary>
        /// Unity Method to draw the GUI.
        /// </summary>
        public override void OnInspectorGUI()
        {
            TBLogo();

            ReferenceTerrainObjectField();

            if (_heatmapController.referenceTerrainObject == null)
            {
                DisplayNoTerrainObjectWarning();
                return;
            }

            DisplayReferenceTerrainObjectToggle();

            DisplayDataPointGizmos();

            HorizontalLine();

            RealTimeUpdateGUI();

            HorizontalLine();

            SelectHeatmapButtons();

            HorizontalLine();

            HeatmapDataGUI();

            HorizontalLine();

            AddRemoveHeatmapButtons();

            HorizontalLine();

            DisplayGenerateHeatmapButton();

            UpdateSceneViewIfNeeded();

        }

        /// <summary>
        /// Draws the TBLogo.
        /// </summary>
        void TBLogo()
        {
            GUILayout.Label(s_tbLogo, GUILayout.Width(EditorGUIUtility.currentViewWidth - 40.0f), GUILayout.Height(60.0f));
        }
        /// <summary>
        /// Display the no terrain warning box.
        /// </summary>
        void DisplayNoTerrainObjectWarning()
        {
            EditorGUILayout.HelpBox("Select a reference Terrain object.", MessageType.Warning);
        }

        /// <summary>
        /// Draw the object field for the reference Terrain object.
        /// </summary>
        void ReferenceTerrainObjectField()
        {
            GUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            _heatmapController.referenceTerrainObject = (Terrain)EditorGUILayout.ObjectField("Reference Terrain", _heatmapController.referenceTerrainObject, typeof(Terrain), true);
            EditorGUI.EndChangeCheck();
            if (GUI.changed) _heatmapController.RefreshReferenceTerrainObject();

            if (GUILayout.Button("Refresh", GUILayout.Width(60.0f))) _heatmapController.RefreshReferenceTerrainObject(); 

            GUILayout.EndHorizontal();
        }
        /// <summary>
        /// Display the RealTimeUpdateGUI settings.
        /// </summary>
        void RealTimeUpdateGUI()
        {
            GUILayout.BeginHorizontal();
                DisplayRealTimeUpdateEnabledToggle();
                DisplayRealTimeEditorUpdateEnabledToggle();
            GUILayout.EndHorizontal();
            DisplayRealTimeUpdateChunks();
            DisplayRealTimeUpdateInterval();
        }

        /// <summary>
        /// Display the toggle for the RealTimeUpdateEnabled boolean.
        /// </summary>
        void DisplayRealTimeUpdateEnabledToggle()
        {
            _heatmapController.RealTimeUpdateEnabled =  EditorGUILayout.Toggle("Real Time Updates",_heatmapController.RealTimeUpdateEnabled);
        }

        /// <summary>
        /// Display the toggle for the RealTimeEditorUpdateEnabled boolean.
        /// </summary>
        void DisplayRealTimeEditorUpdateEnabledToggle()
        {
            _heatmapController.RealTimeEditorUpdateEnabled = EditorGUILayout.Toggle("Editor Updates", _heatmapController.RealTimeEditorUpdateEnabled);
        }

        /// <summary>
        /// Display the RealTimeUpdateInterval's float field.
        /// </summary>
        void DisplayRealTimeUpdateInterval()
        {
            GUILayout.BeginHorizontal();
            _heatmapController.RealTimeUpdateInterval = EditorGUILayout.FloatField("Update Interval (s)" ,_heatmapController.RealTimeUpdateInterval);
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Display the number of selected RealtimeUpdateChunks.
        /// </summary>
        void DisplayRealTimeUpdateChunks()
        {
            GUILayout.BeginHorizontal();
                GUILayout.Label("Real Time Update Chunks (Multiplier)");
                if (GUILayout.Button("<", GUILayout.Width(20.0f))) _heatmapController.RealTimeUpdateChunksMultiplier /= 2;
                GUILayout.Label(""+_heatmapController.RealTimeUpdateChunksMultiplier, GUILayout.Width(40.0f));
                if(GUILayout.Button(">", GUILayout.Width(20.0f))) _heatmapController.RealTimeUpdateChunksMultiplier *=2;
            GUILayout.EndHorizontal();
        }

        /// <summary>
        /// Displays the toggle that controls if the user is viewing the Heatmap or reference Terrain object.
        /// </summary>
        void DisplayReferenceTerrainObjectToggle()
        {
            _heatmapController.DisplayHeatmap = GUILayout.Toggle(_heatmapController.DisplayHeatmap, "Display Heatmap");
        }

        /// <summary>
        /// Display the Heatmap navigation buttons.
        /// </summary>
        void SelectHeatmapButtons()
        {
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField("Selected Heatmap");

            if (GUILayout.Button("<", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmapIndex--;
                _isSceneViewUpdateRequired = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("" + (_heatmapController.SelectedHeatmapIndex + 1), GUILayout.Width(40.0f));
            EditorGUI.EndChangeCheck();

            if (GUILayout.Button(">", GUILayout.Width(20.0f)))
            {
                _heatmapController.SelectedHeatmapIndex++;
                _isSceneViewUpdateRequired = true;
            }

            GUILayout.Label(" of " + _heatmapController.HeatmapCount, GUILayout.Width(40.0f));

            if (GUI.changed)
            {
                TBUnityLib.Generic.BoundedType<int> boundHeatmapIndex = new TBUnityLib.Generic.BoundedType<int>(_heatmapController.SelectedHeatmapIndex, 0, _heatmapController.HeatmapCount - 1);
                _heatmapController.SelectedHeatmapIndex = boundHeatmapIndex.Value;
            }


            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draw a horizontal line across the editor inspector.
        /// </summary>
        void HorizontalLine()
        {
            GUILayout.Box("", new GUILayoutOption[] { GUILayout.ExpandWidth(true), GUILayout.Height(1) });
        }

        /// <summary>
        /// Draw all the GUI elements for the custom inspector.
        /// </summary>
        void HeatmapDataGUI()
        {
            DisplayHeatmapName();
            DisplayBaseValue();
            DisplayFlipConstraints();
            DisplayHeatmapConstraints();
            DisplayHeatmapResolution();
            DisplayHeatmapInterpolationMode();
            DisplayHeatmapDataSource();
            DisplayHeatmapTextures();
        }

        /// <summary>
        /// Display the toggle to flip the Data constraints.
        /// </summary>
        void DisplayFlipConstraints()
        {
            _heatmapController.SelectedHeatmap.displayFlippedConstraints = EditorGUILayout.Toggle("Flip Textures", _heatmapController.SelectedHeatmap.displayFlippedConstraints);
        }


        /// <summary>
        /// Display the float field to change the base value of the Heatmap.
        /// </summary>
        void DisplayBaseValue()
        {
            _heatmapController.SelectedHeatmap.baseValue = EditorGUILayout.FloatField("Base Value",_heatmapController.SelectedHeatmap.baseValue);
        }

        /// <summary>
        /// Display the Heatmap constraints float fields.
        /// </summary>
        void DisplayHeatmapConstraints()
        {
            _heatmapController.SelectedHeatmap.autoConstrain = EditorGUILayout.Toggle("Auto Constrain",_heatmapController.SelectedHeatmap.autoConstrain);

            if(_heatmapController.SelectedHeatmap.autoConstrain == false)
            {
                _heatmapController.SelectedHeatmap.upperValueThreshold = EditorGUILayout.FloatField("    Highest Value Limit", _heatmapController.SelectedHeatmap.upperValueThreshold);
                _heatmapController.SelectedHeatmap.lowerValueThreshold = EditorGUILayout.FloatField("    Lower Value Limit", _heatmapController.SelectedHeatmap.lowerValueThreshold);
            }
        }

        /// <summary>
        /// Display the buttons to add a new heatmap or remove the currently selected one.
        /// </summary>
        void AddRemoveHeatmapButtons()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Add New Heatmap"))
            {
                _heatmapController.AddNewHeatmap();
            }
            GUILayout.Space(30.0f);
            GUI.enabled = _heatmapController.HeatmapCount > 1 ? true : false; ;
            if (GUILayout.Button("Remove Heatmap"))
            {
                 _heatmapController.RemoveSelectedHeatmap();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Display the selected Interpolation mode for the heatmap.
        /// </summary>
        void DisplayHeatmapInterpolationMode()
        {
            _heatmapController.SelectedHeatmap.interpolationMode = (InterpolationMode)EditorGUILayout.EnumPopup("Interpolation Mode", _heatmapController.SelectedHeatmap.interpolationMode);
        }

        /// <summary>
        /// Display toggle controlling the the data point gizmos should be displayed or not.
        /// </summary>
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

        /// <summary>
        /// Displays the field for the selected Heatmap's data source.
        /// </summary>
        void DisplayHeatmapDataSource()
        {
            _heatmapController.SelectedHeatmap.dataType = (HeatmapData)EditorGUILayout.EnumPopup("Data Source", _heatmapController.SelectedHeatmap.dataType);
            if (_heatmapController.SelectedHeatmap.dataType == HeatmapData.Custom)
            {
                _heatmapController.SelectedHeatmap.filter = EditorGUILayout.DelayedTextField("Custom Data Filter", _heatmapController.SelectedHeatmap.filter);
            }
        }

        /// <summary>
        /// Display the toggle to flip the constraints of the selected Heatmap.
        /// </summary>
        void DisplayHeatmapFlipTextures()
        {
            _heatmapController.SelectedHeatmap.displayFlippedConstraints = EditorGUILayout.Toggle("Flip Textures", _heatmapController.SelectedHeatmap.displayFlippedConstraints);
        }
    
        /// <summary>
        /// Display the custom textures of the selected Heatmap.
        /// </summary>
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
                if (_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps != null)
                {
                    _selectedTextureGrid = GUILayout.SelectionGrid(_selectedTextureGrid, GetSplatPrototypesAsTexture2DArray(_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps), gridRowCount, GUI.skin.customStyles[0]);
                }

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add Texture"))
                {
                    _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps = _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps == null ? new List<HeatmapSplatprototype>() : _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps;

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

                if (_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps == null ||_heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Count <= 0) DisplayNoHeatmapSplatprototypeWarning();
            }
        }

        /// <summary>
        /// Displays a warning message complaining about no custom textures being provided.
        /// </summary>
        void DisplayNoHeatmapSplatprototypeWarning()
        {
            EditorGUILayout.HelpBox("No textures have been provided, use the default textures or add some custom textures.", MessageType.Warning);
        }

        /// <summary>
        /// Displays the name of the Heatmap on the Inspector.
        /// </summary>
        void DisplayHeatmapName()
        {
            _heatmapController.SelectedHeatmap.name = EditorGUILayout.DelayedTextField("Name", _heatmapController.SelectedHeatmap.name);
        }

        /// <summary>
        /// Display the  selected Heatmap's resolution and the buttons to change it.
        /// </summary>
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
                _isSceneViewUpdateRequired = true;

            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Calls SceneView.RepaintAll if it's required.
        /// </summary>
        void UpdateSceneViewIfNeeded()
        {
            if (_isSceneViewUpdateRequired) SceneView.RepaintAll();
        }


        /// <summary>
        /// Returns the textures of all the HeatmapSplatPrototypes provided in an array.
        /// </summary>
        /// <param name="splatPrototypes"></param>
        /// <returns>Array of all textures used in the HeatmapSplatprototypes.</returns>
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

        /// <summary>
        /// Updates the properties of a Heatmap's splat prototypes.
        /// </summary>
        /// <param name="_metallic"></param>
        /// <param name="_smoothness"></param>
        /// <param name="_tileSize"></param>
        /// <param name="_tileOffset"></param>
        /// <param name="_textureMap"></param>
        /// <param name="_normalMap"></param>
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

        /// <summary>
        /// Adds a new custom HeatmapSplatPrototype to the selected Heatmap.
        /// </summary>
        /// <param name="newHeatmapSplatprototype"></param>
        public void AddHeatmapSplatPrototype(HeatmapSplatprototype newHeatmapSplatprototype)
        {
            _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Add(newHeatmapSplatprototype);
        }

        /// <summary>
        /// Updates the selected HeatmapSplatPrototype by replacing it with the newSplatPrototype.
        /// </summary>
        /// <param name="newSplatPrototype"></param>
        public void UpdateSelectedSplatPrototype(HeatmapSplatprototype newSplatPrototype)
        {
            _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.RemoveAt(_selectedTextureGrid);
            _heatmapController.SelectedHeatmap.dataVisualisaitonSplatMaps.Insert(_selectedTextureGrid,newSplatPrototype);
        }

        /// <summary>
        /// Displays the 'Generate Heatmap' button.
        /// </summary>
        void DisplayGenerateHeatmapButton()
        {
            if (GUILayout.Button("Generate Heatmap")) _heatmapController.GenerateHeatmap();
        }

        /// <summary>
        /// Generates a HeatmapSplatprototype of type TextureSplatMap from the given parameters.
        /// </summary>
        /// <param name="_albedo"></param>
        /// <param name="_normal"></param>
        /// <param name="_metallic"></param>
        /// <param name="_smoothness"></param>
        /// <param name="_tileOffset"></param>
        /// <param name="_tileSizing"></param>
        /// <returns>TextureSplatMap HeatmapSplatprototype.</returns>
        public HeatmapSplatprototype GenerateHeatmapSplatprototype(Texture2D _albedo, Texture2D _normal, float _metallic, float _smoothness, Vector2 _tileOffset, Vector2 _tileSizing)
        {
            var returnSplatPrototype = ScriptableObject.CreateInstance<TextureSplatMap>();

            returnSplatPrototype.texture = _albedo;
            returnSplatPrototype.normalMap = _normal;
            returnSplatPrototype.metallic = _metallic;
            returnSplatPrototype.smoothness = _smoothness;
            returnSplatPrototype.tileOffset = _tileOffset;
            returnSplatPrototype.tileSizing = _tileSizing;

            return returnSplatPrototype;
        }

        /// <summary>
        /// Generates a HeatmapSplatprototype of type ColorSplatMap from the given parameters.
        /// </summary>
        /// <param name="_color"></param>
        /// <param name="_normal"></param>
        /// <param name="_metallic"></param>
        /// <param name="_smoothness"></param>
        /// <param name="_tileOffset"></param>
        /// <param name="_tileSizing"></param>
        /// <returns>ColorSplatMap HeatmapSplatprototype.</returns>
        public HeatmapSplatprototype GenerateHeatmapSplatprototype(Color _color, Texture2D _normal, float _metallic, float _smoothness, Vector2 _tileOffset, Vector2 _tileSizing)
        {
            var returnSplatPrototype = ScriptableObject.CreateInstance<ColorSplatMap>();

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