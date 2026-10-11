using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Cainos.InteractablePixelWater
{
    // URP Renderer Feature for rendering pixel water
    public class PixelWaterRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("Layer mask of objects that will rendered behind the water")]
        public LayerMask behindWaterMask;

        [Tooltip("The color to use for behind water content if nothing is there")]
        public Color backgroundColor = new (0.1f, 0.1f, 0.1f, 1.0f);

        [Tooltip("Downsample for capturing the behind water content to a texture. Increase this value will improve performance but lower visual quality"), Range(1, 8)]
        public int downsample = 1;

        private PixelWaterRenderPass pixelWaterRenderPass;

        //initializes the render feature and creates the render pass
        public override void Create()
        {
            pixelWaterRenderPass = new PixelWaterRenderPass(behindWaterMask, backgroundColor, downsample);
        }

        //adds the render pass to the URP render pipeline
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            renderer.EnqueuePass(pixelWaterRenderPass);
        }
    }


    //RENDER PASS
    public class PixelWaterRenderPass : ScriptableRenderPass2D
    {
        static readonly int BehindWaterTexId = Shader.PropertyToID("_BehindWaterTex");

        LayerMask behindWaterMask;
        Color backgroundColor;
        int downsample = 1;

        public PixelWaterRenderPass(LayerMask behindWaterMask, Color backgroundColor, int downsample)
        {
            this.behindWaterMask = behindWaterMask;
            this.backgroundColor = backgroundColor;
            this.downsample = Mathf.Max(1, downsample);

            renderPassEvent2D = RenderPassEvent2D.BeforeRenderingSprites;
            renderPassSortingLayerID = 0;
        }

        private class PassData
        {
            public RendererListHandle objectsToDraw;
            public TextureHandle behindWaterTex;
            public TextureHandle behindWaterDepthTex;
            public Color backgroundColor;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            // Fetch camera and resource data
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            //color rt
            var texDesc = cameraData.cameraTargetDescriptor;
            texDesc.colorFormat = RenderTextureFormat.ARGB32;
            texDesc.depthStencilFormat = GraphicsFormat.None;
            texDesc.msaaSamples = 1;
            texDesc.bindMS = false;
            texDesc.width = Mathf.Max(1, cameraData.cameraTargetDescriptor.width / downsample);
            texDesc.height = Mathf.Max(1, cameraData.cameraTargetDescriptor.height / downsample);
            var behindWaterTex = UniversalRenderer.CreateRenderGraphTexture(renderGraph, texDesc, "Behind Water Tex", false, FilterMode.Bilinear, TextureWrapMode.Clamp);

            //depth rt
            var depthDesc = texDesc;
            depthDesc.graphicsFormat = GraphicsFormat.None;
            depthDesc.depthStencilFormat = GraphicsFormat.D24_UNorm_S8_UInt;
            var behindWaterDepthTex = UniversalRenderer.CreateRenderGraphTexture( renderGraph, depthDesc, "Behind Water Depth Tex", true, FilterMode.Point, TextureWrapMode.Clamp);


            //pass 1
            //render objects on behindWaterMask into a behindWaterTex
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Render Behind Water Content", out var passData))
            {
                passData.behindWaterTex = behindWaterTex;
                passData.behindWaterDepthTex = behindWaterDepthTex;
                passData.backgroundColor = backgroundColor;

                // Set up drawing settings and filter for the terrain layer
                var sortingSettings = new SortingSettings(cameraData.camera) { criteria = SortingCriteria.CommonTransparent };
                var shaderTags = new List<ShaderTagId>
                {
                    new ShaderTagId("UniversalForward"),        // Regular 3D objects
                    new ShaderTagId("UniversalForwardOnly"),
                    new ShaderTagId("SRPDefaultUnlit"),         // Unlit materials
                    new ShaderTagId("Universal2D"),             // 2D URP Sprites
                    new ShaderTagId("Sprite"),                  // Legacy Sprite shader
                };
                var drawSettings = RenderingUtils.CreateDrawingSettings(shaderTags, renderingData, cameraData, lightData, sortingSettings.criteria);
                drawSettings.overrideMaterial = null;
                var filterSettings = FilteringSettings.defaultValue;
                filterSettings.renderQueueRange = RenderQueueRange.all;
                filterSettings.layerMask = behindWaterMask;
                filterSettings.renderingLayerMask = uint.MaxValue;
                filterSettings.sortingLayerRange = SortingLayerRange.all;

                var rendererListParams = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);
                passData.objectsToDraw = renderGraph.CreateRendererList(rendererListParams);

                builder.UseRendererList(passData.objectsToDraw);
                builder.UseAllGlobalTextures(true);
                builder.SetRenderAttachment(passData.behindWaterTex, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(passData.behindWaterDepthTex, AccessFlags.ReadWrite);
                builder.SetGlobalTextureAfterPass(passData.behindWaterTex, BehindWaterTexId);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    context.cmd.ClearRenderTarget(true, true, data.backgroundColor);
                    context.cmd.DrawRendererList(data.objectsToDraw);
                });
            }
        }
    }
}


