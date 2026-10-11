//reference https://www.youtube.com/watch?v=TbGEKpdsmCI

using Cainos.Common;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Cainos.InteractivePixelWater
{
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    [HelpURL("https://docs.cainos.net/interactive-pixel-water/script-reference")]
    public class PixelWater : MonoBehaviour
    {
        private const int VERTEX_COUNT_X_PER_UNIT = 8;                                      //how many vertices to generate at x direction per world unit
        private const float VERTEX_SPACING_X = 1.0f / VERTEX_COUNT_X_PER_UNIT;          
        private const int VERTEX_COUNT_Y = 2;                                               //how many vertices to generate at y direction

        [Space]
        [FoldoutGroup("Basic")] public LayerMask interactionLayerMask;                      //defines non trigger colliders at which layer can interact with the water
        [FoldoutGroup("Basic")] public LayerMask interactionTriggerLayerMask;               //defines trigger colliders at which layer can interact with the water

        [FoldoutGroup("Rendering")] public bool waterColorEnabled                   = true;
        [FoldoutGroup("Rendering")] public Color waterColorShallow                  = new Color(0.74f, 0.93f, 0.82f, 0.0f);
        [FoldoutGroup("Rendering")] public Color waterColorDeep                     = new Color(0.44f, 0.61f, 0.52f, 0.38f);
        [Space]
        [FoldoutGroup("Rendering")] public bool underwaterTintEnabled               = true;
        [FoldoutGroup("Rendering")] public Color underwaterTintShallow              = new Color(0.9f, 1.0f, 0.97f, 1.0f);
        [FoldoutGroup("Rendering")] public Color underwaterTintDeep                 = new Color(0.36f, 0.58f, 0.45f, 1.0f);
        [Space]
        [FoldoutGroup("Rendering")] public bool surfaceEnabled                      = true;
        [FoldoutGroup("Rendering")] public Color surfaceColorUpper                  = new Color(0.9f, 1.0f, 1.0f, 0.2f);
        [FoldoutGroup("Rendering")] public Color surfaceColorLower                  = new Color(0.2f, 0.35f, 0.28f, 0.2f);
        [FoldoutGroup("Rendering")] public float surfaceThicknessUpper              = 1.0f;
        [FoldoutGroup("Rendering")] public float surfaceThicknessLower              = 1.0f;
        [FoldoutGroup("Rendering")] public float surfaceDistorionMul                = 5.0f;
        [Space]
        [FoldoutGroup("Rendering")] public bool distortionEnabled                   = true;
        [FoldoutGroup("Rendering")] public float distortionSpeed                    = 1.0f;
        [FoldoutGroup("Rendering")] public float distortionScale                    = 1.5f;
        [FoldoutGroup("Rendering")] public float distortionStrength                 = 0.5f;
        [Space]
        [FoldoutGroup("Rendering")] public bool blurEnabled                         = true;
        [FoldoutGroup("Rendering")] public float blurAmountShallow                  = 2.0f;
        [FoldoutGroup("Rendering")] public float blurAmountDeep                     = 12f;
        [Space]
        [FoldoutGroup("Rendering")] public bool lightShaftEnabled                   = true;
        [FoldoutGroup("Rendering")] public Color lightShaftColor                    = new Color(0.23f, 0.32f, 0.27f, 0.4f);
        [FoldoutGroup("Rendering")] public float lightShaftScale                    = 1.4f;
        [FoldoutGroup("Rendering")] public float lightShaftPower                    = 2.0f;
        [FoldoutGroup("Rendering")] public float lightShaftTilt                     = -0.2f;
        [FoldoutGroup("Rendering")] public float lightShaftDepth                    = 2.0f;
        [FoldoutGroup("Rendering")] public float lightShaftSpeed                    = 0.7f;
        [Space]
        [FoldoutGroup("Rendering")] public bool waveEnabled                         = true;
        [FoldoutGroup("Rendering")] public Vector2 waveInfluenceMul                 = new(0.25f, 1.0f);             //influence multiplier of horizontal and vertical speed to wave
        [FoldoutGroup("Rendering")] public float waveInfluenceDecayDepth            = 2.0f;                         //the depth at which the wave influence decay to 0
        [FoldoutGroup("Rendering")] public float waveTension                        = 1.4f;
        [FoldoutGroup("Rendering")] public float waveDamping                        = 0.5f;                         //intensity of the wave movement slowing down over time. avoid setting it to near or below zero.
        [FoldoutGroup("Rendering")] public float waveSpread                         = 6.5f;                         //how much to spread to nearby springs
        [FoldoutGroup("Rendering")] public int waveSpreadIteration                  = 8;
        [FoldoutGroup("Rendering")] public float waveSpeedMul                       = 5.5f;
        [FoldoutGroup("Rendering")] public float waveVelocityLimit                  = 1.0f;                         //wave velocity limit
        [FoldoutGroup("Rendering")] public float waveLimit                          = 0.5f;                         //wave amptitude limit
        [Space]
        [FoldoutGroup("Rendering")] public bool ambientWaveEnabled = true;                                          //whether to enable ambient wave effect
        [FoldoutGroup("Rendering")] public float ambientWaveMul = 1.0f;                                             //the ambient wave strength multiplier
        [FoldoutGroup("Rendering")] public Vector4 ambientWaveSpeed = new Vector4(4.0f, -7.0f, 10.0f, 0.5f);
        [FoldoutGroup("Rendering")] public Vector4 ambientWaveFrequency = new Vector4(2.0f, 4.0f, 8.0f, 6.0f);
        [FoldoutGroup("Rendering")] public Vector4 ambientWaveAmptitude = new Vector4(0.01f, 0.007f, 0.003f, 0.01f);

        [FoldoutGroup("FX")] public bool bubbleEnabled = true;                                                      //whether to generate bubble effect for objects fall into water
        [FoldoutGroup("FX")] public float bubbleDurationMul = 1.0f;
        [FoldoutGroup("FX")] public float bubbleAmountMul = 1.0f;
        [FoldoutGroup("FX")] public Color bubbleColorOutline = new Color(0.9f, 1.0f, 1.0f, 0.4f);
        [FoldoutGroup("FX")] public Color bubbleColorFill = new Color(0.24f, 0.54f, 0.38f, 0.27f);
        [FoldoutGroup("FX"), AssetsOnly] public GameObject bubblePrefab;
        [Space]
        [FoldoutGroup("FX")] public bool splashEnabled = true;                                                      //whether to generate splash effect for objects fall into water, the global toggle
        [FoldoutGroup("FX")] public bool splashOnEnter = true;                                                      //whether to generate splash effect when objects enter the water
        [FoldoutGroup("FX")] public bool splashOnExit = true;                                                       //whether to generate splash effect when objects exit the water
        [FoldoutGroup("FX")] public Color splashColorLight = new Color(0.64f, 0.89f, 0.91f, 0.27f);
        [FoldoutGroup("FX")] public Color splashColorDark = new Color(0.44f, 0.88f, 0.85f, 0.1f);
        [FoldoutGroup("FX")] public Color splashColorOutline = new Color(0.11f, 0.25f, 0.18f, 0.23f);
        [FoldoutGroup("FX")] public List<SplashConfig> splashConfigs;
        [Space]
        [FoldoutGroup("FX")] public bool surfaceParticleEnabled;                                             //whether to enable the particle system that sync its emit area to the surface of the water
        [FoldoutGroup("FX")] public List<ParticleConfig> surfaceParticleConfigs;
        [Space]
        [FoldoutGroup("FX")] public bool inWaterParticleEnabled;                                              //whether to enable the particle system that sync its emit area to the water area
        [FoldoutGroup("FX")] public List<ParticleConfig> inWaterParticleConfigs;


        [FoldoutGroup("Physics")] public bool dragEnabled = true;                                                   //whether to enable water drag. the buoyancy effector 2d of unity also has drag properties, but we do not use that, as it does not scale with object velocity, not so physically-correct.
        [FoldoutGroup("Physics")] public float dragLinear = 5.0f;
        [FoldoutGroup("Physics")] public float dragAngular = 0.05f;

        [FoldoutGroup("Event")] public UnityEvent<int, Vector3> onSplash;

        private Mesh mesh;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private int vertexCountX;                                                           //current vertex count in x direction
        private Vector3[] vertices;
        private SurfacePoint[] surfacePoints;                                               //surface points info

        private List<SplashInfo> splashInfos = new List<SplashInfo>();                      //record triggered splash to prevent generating too many splash at the same position and same time
        private int splashInfosIterIndex = 0;

        //size of the water area
        [FoldoutGroup("Basic"), ShowInInspector, PropertyOrder(-2)]
        public Vector2 Size
        {
            get { return size; }
            set
            {
                value.x = Mathf.Max(0.03125f, value.x);
                value.y = Mathf.Max(0.03125f, value.y);

                if (size == value) return;

                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(this, "Resize Water");
                #endif

                size = value;
                Refresh();
            }
        }
        [HideInInspector]
        public Vector2 size = new(10.0f, 4.0f);

        //vertial fill percent of the water
        [FoldoutGroup("Basic"), ShowInInspector, PropertyOrder(-1)]
        public float Fill
        {
            get { return fill; }
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(value,fill)) return;

                #if UNITY_EDITOR
                    UnityEditor.Undo.RecordObject(this, "Set Water Fill");
                #endif

                fill = value;
                Refresh();
            }
        }
        [HideInInspector]
        public float fill = 1.0f;

        //actual y size of the water
        public float Depth
        {
            get
            {
                return size.y * fill;
            }
        }


        //the world space x positon of the most left of the water
        public float LeftPos
        {
            get { return transform.position.x - size.x * 0.5f; }
        }

        //the local space x positon of the most left of the water
        public float LeftPosLocal
        {
            get { return -size.x * 0.5f; }
        }

        //the world space x positon of the most right of the water
        public float RightPos
        {
            get { return transform.position.x + size.x * 0.5f; }
        }

        //the local space x positon of the most right of the water
        public float RightPosLocal
        {
            get { return size.x * 0.5f; }
        }

        //the world space y positon of the surface of the water
        public float TopPos
        {
            get { return transform.position.y + size.y * fill; }
        }

        //the local space y positon of the surface of the water
        public float TopPosLocal
        {
            get { return size.y * fill; }
        }

        //the world space y positon of the bottom of the water
        public float BottomPos
        {
            get { return transform.position.y; }
        }

        //the local space y positon of the bottom of the water
        public float BottomPosLocal
        {
            get { return 0.0f; }
        }

        public BoxCollider2D BoxCollider
        {
            get
            {
                if (boxCollider == null)
                {
                    boxCollider = GetComponent<BoxCollider2D>();
                    if (boxCollider == null)
                    {
                        boxCollider = gameObject.AddComponent<BoxCollider2D>();
                        boxCollider.isTrigger = true;
                        boxCollider.usedByEffector = true;
                    }
                }
                return boxCollider;
            }
        }
        private BoxCollider2D boxCollider;

        public BuoyancyEffector2D BuoyancyEffector
        {
            get
            {
                if (buoyancyEffector == null)
                {
                    buoyancyEffector = GetComponent<BuoyancyEffector2D>();
                    if (buoyancyEffector == null)
                    {
                        buoyancyEffector = gameObject.AddComponent<BuoyancyEffector2D>();
                        buoyancyEffector.colliderMask = interactionLayerMask;
                    }
                }
                return buoyancyEffector;
            }
        }
        private BuoyancyEffector2D buoyancyEffector;

        public Material WaterMaterial
        {
            get
            {
                if ( waterMaterial == null)
                {
                    var shader = Shader.Find("Cainos/Interactive Pixel Water/Pixel Water");
                    if (shader == null) return null;

                    waterMaterial = new Material(shader);
                    waterMaterial.renderQueue = 3000;
                }
                return waterMaterial;
            }
        }
        private Material waterMaterial;

        private string GeneratedMeshName
        {
            get
            {
                #if UNITY_6000_4_OR_NEWER
                    return $"[Water Mesh] {GetEntityId()}";
                #else
                    return $"[Water Mesh] {GetInstanceID()}";
                #endif
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void Reset()
        {
            Refresh();
        }

        private void OnValidate()
        {
            ResetCollider();
            UpdateFx();
            UpdateMaterial();
        }

        [FoldoutGroup("Action"), Button("Refresh")]
        public void Refresh()
        {
            ResetCollider();
            UpdateFx();
            GenerateMesh();
            UpdateMaterial();
        }

        public void ResetCollider()
        {
            if (BoxCollider)
            {
                BoxCollider.size = new Vector2(Size.x, Size.y * fill);
                BoxCollider.offset = new Vector2(0.0f, Size.y * 0.5f * fill);
            }

            if (BuoyancyEffector)
            {
                BuoyancyEffector.surfaceLevel = Size.y * fill;
                BuoyancyEffector.colliderMask = interactionLayerMask;
            }
        }

        public void GenerateMesh()
        {
            //get x vertex count by size
            vertexCountX = Mathf.CeilToInt(size.x * VERTEX_COUNT_X_PER_UNIT);
            vertexCountX = Mathf.Max(VERTEX_COUNT_X_PER_UNIT, vertexCountX);

            //verices
            vertices = new Vector3[vertexCountX * VERTEX_COUNT_Y];

            //surface points
            surfacePoints = new SurfacePoint[vertexCountX];

            //vertices
            for (int y = 0; y < VERTEX_COUNT_Y; y++)
            {
                for (int x = 0; x < vertexCountX; x++)
                {
                    float xPos = (x / (float)(vertexCountX - 1)) * size.x - size.x * 0.5f;
                    float yPos = (y / (float)(VERTEX_COUNT_Y - 1)) * size.y * fill;
                    vertices[y * vertexCountX + x] = new Vector3(xPos, yPos, 0.0f);

                    //record surface point index
                    if (y == VERTEX_COUNT_Y - 1) surfacePoints[x].index = y * vertexCountX + x;
                }
            }

            //construct triangles
            int[] triangles = new int[(vertexCountX - 1) * (VERTEX_COUNT_Y - 1) * 6];
            int index = 0;

            for (int y = 0; y < VERTEX_COUNT_Y - 1; y++)
            {
                for (int x = 0; x < vertexCountX - 1; x++)
                {
                    int bl = y * vertexCountX + x;
                    int br = bl + 1;
                    int tl = bl + vertexCountX;
                    int tr = tl + 1;

                    //1st bottom-left triangle
                    triangles[index++] = bl;
                    triangles[index++] = tl;
                    triangles[index++] = br;

                    //2nd top-right triangle
                    triangles[index++] = br;
                    triangles[index++] = tl;
                    triangles[index++] = tr;
                }
            }

            //uv
            //on x: most left one unit [0,1], most right one unit [1,0], used for ambient wave mask
            //on y: top 0, bottom height, used for identifying water surface
            Vector2[] uv = new Vector2[vertices.Length];
            for ( int i = 0; i < uv.Length; i++)
            {
                float x = 1.0f;
                if (vertices[i].x - vertices[0].x < 1.0f) x = vertices[i].x - vertices[0].x;
                else if (vertices[^1].x - vertices[i].x < 1.0f) x = vertices[^1].x - vertices[i].x;

                uv[i] = new Vector2(x, -vertices[i].y + size.y * fill);
            }

            //uv2
            //on x: left 0, right 1
            //on y: top 0, bottom 1, used for water depth, ambient wave mask
            Vector2[] uv2 = new Vector2[vertices.Length];
            for (int i = 0; i < uv.Length; i++)
            {
                uv2[i] = new Vector2( uv[i].x/size.x, uv[i].y/size.y);
            }

            //surface points
            for (int i = 0; i < surfacePoints.Length; i++)
            {
                Vector3 p = vertices[surfacePoints[i].index];
                surfacePoints[i].pos = p;
                surfacePoints[i].originPos = p;
            }

            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (WaterMaterial) meshRenderer.material = WaterMaterial;

            DestroyGeneratedMesh();

            mesh = new Mesh();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.uv = uv;
            mesh.uv2 = uv2;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.name = GeneratedMeshName;

            meshFilter.mesh = mesh;
        }

        private void DestroyGeneratedMesh()
        {
            Mesh meshToDestroy = mesh;

            if (meshToDestroy == null || meshToDestroy.name != GeneratedMeshName)
            {
                mesh = null;
                return;
            }

            if (meshFilter && meshFilter.sharedMesh == meshToDestroy)
            {
                meshFilter.sharedMesh = null;
            }

            #if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                if (AssetDatabase.Contains(meshToDestroy) == false)
                {
                    DestroyImmediate(meshToDestroy);
                }

                mesh = null;
                return;
            }
            #endif

            Destroy(meshToDestroy);
            mesh = null;
        }

        public void UpdateMaterial()
        {
            //material property block is not supported in unity 6.3 with urp 2d
            //thus here we directly set params on the material and set material property block to null on the mesh renderer

            if (WaterMaterial == null) return;
            if (meshRenderer == null)
            {
                meshRenderer = GetComponent<MeshRenderer>();
                meshRenderer.SetPropertyBlock(null);
            }
            meshRenderer.material = WaterMaterial;

            WaterMaterial.SetColor("_WaterColorShallow", waterColorEnabled ? waterColorShallow : Color.clear);
            WaterMaterial.SetColor("_WaterColorDeep", waterColorEnabled ? waterColorDeep : Color.clear);

            WaterMaterial.SetColor("_UnderwaterTintShallow", underwaterTintEnabled ? underwaterTintShallow : Color.white);
            WaterMaterial.SetColor("_UnderwaterTintDeep", underwaterTintEnabled ? underwaterTintDeep : Color.white);

            WaterMaterial.SetColor("_SurfaceColorUpper", surfaceColorUpper);
            WaterMaterial.SetColor("_SurfaceColorLower", surfaceColorLower);
            WaterMaterial.SetFloat("_SurfaceThicknessUpper", surfaceEnabled ? surfaceThicknessUpper : 0.0f);
            WaterMaterial.SetFloat("_SurfaceThicknessLower", surfaceEnabled ? surfaceThicknessLower : 0.0f);
            WaterMaterial.SetFloat("_SurfaceDistortionMul", surfaceDistorionMul);

            WaterMaterial.SetFloat("_DistortionScale", distortionScale);
            WaterMaterial.SetFloat("_DistortionSpeed", distortionSpeed);
            WaterMaterial.SetFloat("_DistortionStrength", distortionEnabled ? distortionStrength : 0.0f);

            WaterMaterial.SetFloat("_BlurAmountShallow", blurEnabled ? blurAmountShallow : 0.0f);
            WaterMaterial.SetFloat("_BlurAmountDeep", blurEnabled ? blurAmountDeep : 0.0f);

            WaterMaterial.SetColor("_LightShaftColor", lightShaftEnabled ? lightShaftColor : Color.clear);
            WaterMaterial.SetFloat("_LightShaftScale", lightShaftScale);
            WaterMaterial.SetFloat("_LightShaftPower", lightShaftPower);
            WaterMaterial.SetFloat("_LightShaftTilt", lightShaftTilt);
            WaterMaterial.SetFloat("_LightShaftDepth", lightShaftDepth);
            WaterMaterial.SetFloat("_LightShaftSpeed", lightShaftSpeed);

            WaterMaterial.SetFloat("_AmbientWaveMul", ambientWaveEnabled ? ambientWaveMul : 0.0f);
            WaterMaterial.SetVector("_AmbientWaveSpeed", ambientWaveSpeed);
            WaterMaterial.SetVector("_AmbientWaveFrequency", ambientWaveFrequency);
            WaterMaterial.SetVector("_AmbientWaveAmptitude", ambientWaveAmptitude);
        }

        public void UpdateFx()
        {
            if (surfaceParticleConfigs != null)
            foreach ( ParticleConfig c in surfaceParticleConfigs )
            {
                if ( c == null) continue;
                if ( c.particleSystem == null) continue;

                var em = c.particleSystem.emission;
                em.enabled = surfaceParticleEnabled;
                em.rateOverTimeMultiplier = size.x * c.emitMulPerUnit;

                c.particleSystem.transform.localPosition = Vector3.zero;

                var sm = c.particleSystem.shape;
                sm.position = new Vector3(0.0f, TopPosLocal, 0.1f);
                sm.scale = new Vector3(size.x, 0.05f, 0.05f);
            }

            if (inWaterParticleConfigs != null)
            foreach (ParticleConfig c in inWaterParticleConfigs )
            {
                if (c == null) continue;
                if (c.particleSystem == null) continue;

                var em = c.particleSystem.emission;
                em.enabled = inWaterParticleEnabled;
                em.rateOverTimeMultiplier = size.x * size.y * c.emitMulPerUnit;

                c.particleSystem.transform.localPosition = Vector3.zero;

                var sm = c.particleSystem.shape;
                sm.position = new Vector3(0.0f, size.y * 0.5f, -0.1f);
                sm.scale = new Vector3(size.x, size.y, 0.05f);
            }
        }


        private void OnTriggerEnter2D(Collider2D collider)
        {
            if (collider.isTrigger)
            {
                if (interactionTriggerLayerMask.Contains(collider.gameObject.layer) == false) return;
            }
            else
            {
                if (interactionLayerMask.Contains(collider.gameObject.layer) == false) return;
            }

            Rigidbody2D rb = collider.attachedRigidbody;
            if (rb == null) return;

            AddWave(collider, rb);
            AddBubble(collider);
            if (splashOnEnter) AddSplash(collider, rb);
        }

        private void OnTriggerStay2D(Collider2D collider)
        {
            if (collider.isTrigger)
            {
                if (interactionTriggerLayerMask.Contains(collider.gameObject.layer) == false) return;
            }
            else
            {
                if (interactionLayerMask.Contains(collider.gameObject.layer) == false) return;
            }

            Rigidbody2D rb = collider.attachedRigidbody;
            if (rb == null) return;

            //wave
            //the effect of the velocity in y direction is greatly reduced for object already in water
            AddWave(collider, rb, Time.fixedDeltaTime, Time.fixedDeltaTime * 0.1f);

            //handle water drag
            if ( dragEnabled)
            {
                //linear drag
                Vector2 velocity = rb.linearVelocity;
                if (velocity.sqrMagnitude > 0.0001f)
                {
                    Vector2 dragForce = -dragLinear * velocity;
                    rb.AddForce(dragForce, ForceMode2D.Force);
                }

                //angular drag
                float angularVelocity = rb.angularVelocity;
                if (Mathf.Abs(angularVelocity) > 0.001f)
                {
                    float dragTorque = -dragAngular * angularVelocity;
                    rb.AddTorque(dragTorque, ForceMode2D.Force);
                }
            }
        }

        private void OnTriggerExit2D(Collider2D collider)
        {
            if (collider.isTrigger)
            {
                if (interactionTriggerLayerMask.Contains(collider.gameObject.layer) == false) return;
            }
            else
            {
                if (interactionLayerMask.Contains(collider.gameObject.layer) == false) return;
            }

            Rigidbody2D rb = collider.attachedRigidbody;
            if (rb == null) return;

            AddWave(collider, rb);
            if (splashOnExit) AddSplash(collider, rb);
        }

        //try add a splash effect for the given collider
        private void AddSplash( Collider2D collider, Rigidbody2D rb )
        {
            //no splash config, return
            if (splashEnabled == false) return;
            if (splashConfigs == null) return;
            if (splashConfigs.Count == 0) return;

            //get size, position and velocity of the collider that enters the water
            Bounds bounds = collider.bounds;
            Vector2 pos = bounds.center;
            float size = bounds.size.x;
            Vector2 vel = rb ? rb.linearVelocity : Vector2.zero;

            //check if vertical range of the collider overlap with the surface of the water
            //to prevent generating splash for objects appear underwater
            //allow 0.1 second of tolerance to handle fast moving objects
            var yRange = new Vector2(bounds.min.y, bounds.max.y);
            yRange.x -= Mathf.Abs(vel.y * 0.1f);
            yRange.y += Mathf.Abs(vel.y * 0.1f);
            if ((yRange.x < TopPos && TopPos < yRange.y) == false) return;

            AddSplash(pos, size, Mathf.Abs(vel.y));
        }

        //add a splash at the given pos with the given size
        public void AddSplash ( Vector3 pos, float size, float speed)
        {
            //no splash configs, return
            if (splashConfigs == null || splashConfigs.Count == 0) return;

            //check position
            if (pos.x + size < LeftPos) return;
            if (pos.x - size > RightPos) return;

            var splashConfig = splashConfigs.Find(x => (x.sizeRange.x <= size && size <= x.sizeRange.y));
            if (splashConfig == null)
            {
                if (size < splashConfigs[0].sizeRange.x) splashConfig = splashConfigs[0];
                else
                if (size > splashConfigs[^1].sizeRange.y) splashConfig = splashConfigs[^1];
            }
            if (splashConfig == null) splashConfig = splashConfigs[0];
            int level = splashConfigs.IndexOf(splashConfig);

            //check minimum speed and depth
            if (speed < splashConfig.minSpeed) return;
            if (Depth < splashConfig.minDepth) return;

            //check if there is existing bigger splash at the same position
            //if yes, skip adding this one
            foreach (SplashInfo info in splashInfos)
            {
                if (info.range.x < pos.x && pos.x < info.range.y && info.level >= level) return;
            }

            //event
            onSplash?.Invoke(level, pos);

            //get the splash fx prefab
            var splashFxPrefab = splashConfig.splashPrefab;
            if (splashFxPrefab == null) return;

            //add splash
            var splash = Instantiate(splashFxPrefab, transform).GetComponent<PixelWaterSplash>();
            splash.Init(this, pos, splashColorLight, splashColorDark, splashColorOutline);

            //record splash info
            splashInfos.Add(new SplashInfo
            {
                level = level,
                range = splash.Range,
                splash = splash
            });
        }







        //add bubble effecet at the given location
        public void AddBubble( Vector3 pos , float duration = 1.0f, float burstCount = 15.0f)
        {
            if (bubbleEnabled == false) return;
            if (bubblePrefab == null) return;

            var bubble = Instantiate(bubblePrefab, transform).GetComponent<PixelWaterBubble>();
            bubble.Init(this, pos, bubbleColorOutline, bubbleColorFill, duration, burstCount);
        }

        //add bubble effect for the given transform
        public void AddBubble(Transform followTarget, float duration = 1.0f, float burstCount = 15.0f, float rateOverTime = 20.0f, float startSpeedMax = 1.5f)
        {
            if (bubbleEnabled == false) return;
            if (bubblePrefab == null) return;

            var bubble = Instantiate(bubblePrefab, transform).GetComponent<PixelWaterBubble>();
            bubble.Init(this, followTarget, bubbleColorOutline, bubbleColorFill, duration, burstCount, rateOverTime, startSpeedMax);
        }

        //add bubble effect for the given collider
        public void AddBubble(Collider2D collider)
        {
            float mul = collider.bounds.size.x * collider.bounds.size.y;
            AddBubble(collider.transform, 1.0f * bubbleDurationMul, 10 * mul * bubbleAmountMul, 20 * mul * bubbleAmountMul);
        }






        private void AddWave (Collider2D collider, Rigidbody2D rb, float mulX = 1.0f, float mulY = 1.0f)
        {
            if (waveEnabled == false) return;

            float depthDecay = GetWaveDepthDecayForCollider(collider);
            if (depthDecay < 0.01f) return;

            //vertical
            float vel = rb.linearVelocity.y * waveInfluenceMul.y * mulY * depthDecay;
            float radius = collider.bounds.extents.x;
            Vector2 center = collider.bounds.center;
            AddWave(center, radius, vel);

            //left
            vel = -rb.linearVelocity.x * waveInfluenceMul.x * mulX * depthDecay;
            radius = 0.2f;
            center = collider.bounds.center - new Vector3(collider.bounds.extents.x, 0.0f, 0.0f);
            AddWave(center, radius, vel);

            //right
            vel = rb.linearVelocity.x * waveInfluenceMul.x * mulX * depthDecay;
            radius = 0.2f;
            center = collider.bounds.center + new Vector3(collider.bounds.extents.x, 0.0f, 0.0f);
            AddWave(center, radius, vel);
        }

        //add a wave
        //given a world position center, radius of the trigger source and wave velocity
        //decayDis: the distance to the water surface where the wave effect will decay to none, set to 0.0 to disable decay
        public void AddWave( Vector2 center, float radius, float vel, float decayDis = 0.0f)
        {
            if (surfacePoints == null || surfacePoints.Length == 0) return;

            //caculate decay
            if (decayDis > 0.01f)
            {
                float yDis = Mathf.Abs(TopPos - center.y);
                float decay = 1.0f - Mathf.Clamp01(yDis / decayDis);
                vel *= decay;
            }

            float minX = center.x - radius;
            float maxX = center.x + radius;

            int indexMin;
            int indexMax;

            //no enough surface point to trigger wave
            if (surfacePoints.Length <= 1)
            {
                indexMin = 0;
                indexMax = 0;
            }
            //wave trigger source outside water area
            if ( maxX < LeftPos || minX > RightPos )
            {
                indexMin = 0;
                indexMax = 0;
            }
            //find the min index and max index of the surface points affected by this trigger
            else
            {
                float start = (minX - LeftPos) / VERTEX_SPACING_X;
                float end = (maxX - LeftPos) / VERTEX_SPACING_X;

                indexMin = Mathf.Max(0, (int)Math.Ceiling(start));
                indexMax = Mathf.Min(surfacePoints.Length - 1, (int)Math.Floor(end));
            }

            //apply wave velocity
            for ( int i = indexMin; i <= indexMax; i++)
            {
                surfacePoints[i].vel += vel;
                surfacePoints[i].vel = Mathf.Clamp(surfacePoints[i].vel, -waveVelocityLimit, waveVelocityLimit);
            }
        }

        private float GetWaveDepthDecayForCollider( Collider2D collider)
        {
            float decay;
            float top = collider.bounds.max.y;
            float bot = collider.bounds.min.y;

            if (bot < TopPos && top > TopPos) decay = 1.0f;
            else
            if (bot > TopPos || top < BottomPos) decay = 0.0f;
            else
            {
                float distance = Mathf.Abs(TopPos - top);
                decay = Mathf.Clamp01( (waveInfluenceDecayDepth - distance) / waveInfluenceDecayDepth);
            }

            return decay;
        }

        //get actual wave y position by x position in local space
        //xPos: local space x position
        //t: how much sec to advance in time to predict the wave position
        public float GetWavePosLocal( float xPos, float t = 0.0f)
        {
            int L = Mathf.FloorToInt((xPos - LeftPosLocal) / VERTEX_SPACING_X);
            int R = L + 1;

            if ( L < 0 || R >= surfacePoints.Length) return TopPosLocal;

            float posL = surfacePoints[L].pos.y + surfacePoints[L].vel * t;
            float posR = surfacePoints[R].pos.y + surfacePoints[R].vel * t;

            return (posL + posR) * 0.5f;
        }

        public float GetWavePos ( float xPos, float t = 0.0f)
        {
            var localPos = transform.InverseTransformPoint(new Vector3(xPos, 0, 0));
            var wavePosLocal = GetWavePosLocal(localPos.x, t);

            return transform.TransformPoint(new Vector3(0.0f, wavePosLocal, 0.0f)).y;
        }

        //doing wave sim here
        private void FixedUpdate()
        {
            if (mesh == null || vertices == null || surfacePoints == null) return;

            //update all surface points
            //the left-most and right-most point is excluded
            for ( int i = 1; i < surfacePoints.Length - 1; i++ )
            {
                float x = surfacePoints[i].pos.y - surfacePoints[i].originPos.y;
                float acc = -waveTension * x - waveDamping * surfacePoints[i].vel;
                surfacePoints[i].pos.y += surfacePoints[i].vel * waveSpeedMul * Time.fixedDeltaTime;
                surfacePoints[i].vel += acc * waveSpeedMul * Time.fixedDeltaTime;

                //limit wave amptitude
                surfacePoints[i].pos.y = Mathf.Clamp(surfacePoints[i].pos.y, surfacePoints[i].originPos.y - waveLimit, surfacePoints[i].originPos.y + waveLimit);

                //move the actual mesh point
                vertices[surfacePoints[i].index].y = surfacePoints[i].pos.y;
            }

            //wave spread
            for (int j = 0; j < waveSpreadIteration; j++)
            {
                for (int i = 1; i < surfacePoints.Length - 1; i++)
                {
                    float leftDelta = waveSpread * (surfacePoints[i].pos.y - surfacePoints[i - 1].pos.y) * Time.fixedDeltaTime;
                    surfacePoints[i - 1].vel += leftDelta * waveSpeedMul;

                    float rightDelta = waveSpread * (surfacePoints[i].pos.y - surfacePoints[i + 1].pos.y) * Time.fixedDeltaTime;
                    surfacePoints[i + 1].vel += rightDelta * waveSpeedMul;
                }
            }

            mesh.vertices = vertices;
        }

        private void Update()
        {
            //remove splash info that has safe time passed
            if (splashInfos.Count > 0)
            {
                if (splashInfosIterIndex >= splashInfos.Count) splashInfosIterIndex = 0;

                var s = splashInfos[splashInfosIterIndex].splash;
                if (s == null || s.IsSafeTimePassed )
                    splashInfos.RemoveAt(splashInfosIterIndex);

                splashInfosIterIndex++;
            }
        }


        //water surface point information for wave sim
        private struct SurfacePoint
        {
            public int index;               //index of the corresponding vertex in vertices
            public float vel;               //current velocity
            public float acc;               //current acceleration
            public Vector2 pos;             //object space position, the same as the top vertices of the mesh
            public Vector2 originPos;       //origin pos
        }

        //splash config data
        [System.Serializable]
        public class SplashConfig
        {
            public Vector2 sizeRange;                           //the x size range of the object that enters the water that will trigger this level of splash
            public float minSpeed;                              //minimum speed required to trigger the splash
            public float minDepth;                              //minimum water depth required to trigger the splash
            [AssetsOnly] public GameObject splashPrefab;        //the splash fx prefab to show
        }


        //ongoing splash info
        private struct SplashInfo
        {
            public int level;                           //splash level
            public Vector2 range;                       //splash x range;
            public PixelWaterSplash splash;             //the splash object
        }

        //particle system config
        [System.Serializable]
        public class ParticleConfig
        {
            public ParticleSystem particleSystem;                   //particle system
            public float emitMulPerUnit = 1.0f;                     //emit rate multiplier per unit, used to make the emit rate sync with the size with the water area
        }
    }
}
