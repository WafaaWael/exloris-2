using UnityEngine;
using UnityEditor;
using Cainos.Common;

namespace Cainos.InteractivePixelWater
{
    [CustomEditor(typeof(PixelWater))]
    public class PixelWaterEditor : InspectorEditor
    {
        private PixelWater water;

        protected override void OnEnable()
        {
            base.OnEnable();

            water = (PixelWater)target;

            Undo.undoRedoPerformed += OnUndoRedo;

            //set tooltips
            SetTooltip("Size", "Controlling the width (X) and maximum height (Y) of the water area. Modifying this regenerates the mesh.");
            SetTooltip("Fill", "A float (0 to 1) representing the vertical fill percentage. 1.0 is full height defined by Size.y, while lower values lower the water surface level.");
            SetTooltip("interactionLayerMask", "Defines which layers contain non-trigger colliders that can physically interact with the water (create waves, splashes, etc.).");
            SetTooltip("interactionTriggerLayerMask", "Defines which layers contain trigger colliders that can interact with the water.");

            SetTooltip("waterColorEnabled", "Toggles the water color. When off the water itself will be transparent.");
            SetTooltip("waterColorShallow", "The color gradient from the water surface (shallow) to the bottom (deep). The color is blended using alpha blend.");
            SetTooltip("waterColorDeep", "The color gradient from the water surface (shallow) to the bottom (deep). The color is blended using alpha blend.");

            SetTooltip("underwaterTintEnabled", "Toggles the tint applied to the content behind the water.");
            SetTooltip("underwaterTintShallow", "The color tint applied to content behind the water, grading from surface (shallow) to bottom (deep). The color is blended using multiply.");
            SetTooltip("underwaterTintDeep", "The color tint applied to content behind the water, grading from surface (shallow) to bottom (deep). The color is blended using multiply.");

            SetTooltip("surfaceEnabled", "Toggles the rendering of the surface line at the top of the water. The surface line is divided into upper and lower part.");
            SetTooltip("surfaceColorUpper", "Color of the upper part of the surface line.");
            SetTooltip("surfaceColorLower", "Color of the lower part of the surface line.");
            SetTooltip("surfaceThicknessUpper", "Controls how thick the upper surface line is rendered.");
            SetTooltip("surfaceThicknessLower", "Controls how thick the lower surface line is rendered.");
            SetTooltip("surfaceDistortionMul", "Multiplier for the distortion effect of the surface line.");

            SetTooltip("distortionEnabled", "Toggles the refraction/distortion effect of the behind water content.");
            SetTooltip("distortionSpeed", "Controls the animation speed of the effect.");
            SetTooltip("distortionScale", "Controls the texture scale of the effect.");
            SetTooltip("distortionStrength", "Controls the intensity of the effect.");

            SetTooltip("blurEnabled", "Toggles the blur effect for the behind water content.");
            SetTooltip("blurAmountShallow", "The blur effect intensity, grading from surface (shallow) to bottom (deep).");
            SetTooltip("blurAmountDeep", "The blur effect intensity, grading from surface (shallow) to bottom (deep).");

            SetTooltip("lightShaftEnabled", "Toggles the effect of light beams shining down into the water.");
            SetTooltip("lightShaftColor", "The color and transparency of the light beams. The color is blended using add mode.");
            SetTooltip("lightShaftScale", "Controls the size of the light rays.");
            SetTooltip("lightShaftPower", "The power applied to the alpha of the light rays.");
            SetTooltip("lightShaftTilt", "Controls the angle of the light rays.");
            SetTooltip("lightShaftDepth", "Controls the fade-depth of the light rays.");
            SetTooltip("lightShaftSpeed", "Controls the animation speed of the light rays.");

            SetTooltip("waveEnabled", "Toggles the physics-based spring simulation for surface waves.");
            SetTooltip("waveInfluenceMul", "Multiplier for how much an object's velocity (X and Y) impacts the water surface to create waves.");
            SetTooltip("waveInfluenceDecayDepth", "The depth at which an object no longer creates surface waves (objects deep underwater won't disturb the surface).");
            SetTooltip("waveTension", "Controls the stiffness of the wave. Higher values make the water surface snap back to rest position faster.");
            SetTooltip("waveDamping", "Controls friction of the wave. Higher values make waves settle and stop oscillating faster. Avoid setting it to near zero.");
            SetTooltip("waveSpread", "Controls how fast a wave ripple travels to neighboring vertices.");
            SetTooltip("waveSpreadIteration", "The quality/smoothness of the wave propagation calculation (higher is more accurate but more expensive).");
            SetTooltip("waveSpeedMul", "Global speed multiplier for the wave physics simulation.");
            SetTooltip("waveVelocityLimit", "Clamps the maximum speed of the wave.");
            SetTooltip("waveLimit", "Clamps the maximum amplitude of the wave.");

            SetTooltip("ambientWaveEnabled", "Toggles the constant background waves.");
            SetTooltip("ambientWaveMul", "Global intensity multiplier for the ambient wave.");
            SetTooltip("ambientWaveSpeed", "Parameters that allow layering up to 4 different wave noises (x, y, z, w) for complex, non-repeating surface wave motion.");
            SetTooltip("ambientWaveFrequency", "Parameters that allow layering up to 4 different wave noises (x, y, z, w) for complex, non-repeating surface wave motion.");
            SetTooltip("ambientWaveAmplitude", "Parameters that allow layering up to 4 different wave noises (x, y, z, w) for complex, non-repeating surface wave motion.");

            SetTooltip("bubbleEnabled", "Toggles bubble effect generation.");
            SetTooltip("bubbleDurationMul", "Multiplier for bubble emit duration.");
            SetTooltip("bubbleAmountMul", "Multiplier for bubble emit amount.");
            SetTooltip("bubbleColorOutline", "The outline color of the generated bubble.");
            SetTooltip("bubbleColorFill", "The fill color of the generated bubble.");
            SetTooltip("bubblePrefab", "The prefab instantiated to create bubbles. (must contain PixelWaterBubble script).");

            SetTooltip("splashEnabled", "Toggles splash effect generation.");
            SetTooltip("splashOnEnter", "Whether to generate splash when objects enter the water.");
            SetTooltip("splashOnExit", "Whether to generate splash when objects exit the water.");
            SetTooltip("splashColorLight", "The color of the lighter part of the splash.");
            SetTooltip("splashColorDark", "The color of the darker part of the splash.");
            SetTooltip("splashColorOutline", "The outline color of the splash.");
            SetTooltip("splashConfigs", "A list of SplashConfig objects defining different splash types based on object size.");

            SetTooltip("surfaceParticleEnabled", "Toggles continuous particles on the water surface.");
            SetTooltip("surfaceParticleConfigs", "A list of ParticleConfig objects defining particle systems used as the surface particle.");
            SetTooltip("inWaterParticleEnabled", "Toggles continuous particles inside the water volume.");
            SetTooltip("inWaterParticleConfigs", "A list of ParticleConfig objects defining particle systems used as the surface particle.");

            SetTooltip("dragEnabled", "Toggles the custom drag force.");
            SetTooltip("dragLinear", "How much linear velocity is slowed down.");
            SetTooltip("dragAngular", "How much rotation (angular velocity) is slowed down.");
        }


        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            // Regenerate mesh after undo/redo operations
            if (water) water.GenerateMesh();
        }

        private void OnSceneGUI()
        {
            //wireframe cube
            Handles.color = Color.white;
            Vector3 center = water.transform.position + new Vector3(0.0f, water.size.y * 0.5f, 0.0f);
            Vector3 size = new Vector3(water.size.x, water.size.y, 0.1f);
            Handles.DrawWireCube(center, size);

            //handles for width and height
            float handleSize = HandleUtility.GetHandleSize(center) * 0.1f;
            Vector3 snap = Vector3.one * 0.1f;

            //corner handle position
            Vector3[] handlePos = new Vector3[4];
            handlePos[0] = center + new Vector3(-water.size.x * 0.5f, -water.size.y * 0.5f, 0.0f);        //BL
            handlePos[1] = center + new Vector3(water.size.x * 0.5f, -water.size.y * 0.5f, 0.0f);        //BR
            handlePos[2] = center + new Vector3(-water.size.x * 0.5f, water.size.y * 0.5f, 0.0f);        //TL
            handlePos[3] = center + new Vector3(water.size.x * 0.5f, water.size.y * 0.5f, 0.0f);        //TR

            //bottom left handle
            EditorGUI.BeginChangeCheck();
            Vector3 newBL = Handles.FreeMoveHandle(handlePos[0], handleSize, snap, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(water, "Resize Water");
                Undo.RecordObject(water.transform, "Move Water");

                water.Size = new Vector2(handlePos[1].x - newBL.x, handlePos[2].y - newBL.y);
                water.transform.position += new Vector3((newBL.x - handlePos[0].x) * 0.5f, (newBL.y - handlePos[0].y), 0.0f);
            }

            //bottom right handle
            EditorGUI.BeginChangeCheck();
            Vector3 newBR = Handles.FreeMoveHandle(handlePos[1], handleSize, snap, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(water, "Resize Water");
                Undo.RecordObject(water.transform, "Move Water");

                water.Size = new Vector2(newBR.x - handlePos[0].x, handlePos[3].y - newBR.y);
                water.transform.position += new Vector3((newBR.x - handlePos[1].x) * 0.5f, (newBR.y - handlePos[1].y), 0.0f);
            }

            //top left handle
            EditorGUI.BeginChangeCheck();
            Vector3 newTL = Handles.FreeMoveHandle(handlePos[2], handleSize, snap, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(water, "Resize Water");
                Undo.RecordObject(water.transform, "Move Water");

                water.Size = new Vector2(handlePos[3].x - newTL.x, newTL.y - handlePos[0].y);
                water.transform.position += new Vector3((newTL.x - handlePos[2].x) * 0.5f, 0.0f, 0.0f);
            }

            //top right
            EditorGUI.BeginChangeCheck();
            Vector3 newTR = Handles.FreeMoveHandle(handlePos[3], handleSize, snap, Handles.CubeHandleCap);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(water, "Resize Water");
                Undo.RecordObject(water.transform, "Move Water");

                water.Size = new Vector2(newTR.x - handlePos[2].x, newTR.y - handlePos[1].y);
                water.transform.position += new Vector3((newTR.x - handlePos[3].x) * 0.5f, 0.0f, 0.0f);
            }


            if (GUI.changed)
            {
                water.GenerateMesh();
            }
        }
    }
}
