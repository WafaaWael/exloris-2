using UnityEngine;

namespace Cainos.InteractivePixelWater
{
    //water bubble fx control
    public class PixelWaterBubble : MonoBehaviour
    {
        private Transform followTarget;
        private PixelWater water;
        private ParticleSystem ps;

        public void Init(
            PixelWater pixelWater, Vector3 worldPos,
            Color bubbleColorOutline, Color bubbleColorFill,
            float duration = 1.0f, float burstCount = 15.0f, float rateOverTime = 20.0f,
            float startSpeedMax = 1.5f)
        {
            water = pixelWater;

            Vector3 pos = water.transform.InverseTransformPoint(worldPos);
            pos.z = -0.1f;
            transform.localPosition = pos;

            ps = GetComponent<ParticleSystem>();
            if (ps)
            {
                var main = ps.main;
                main.duration = duration;
                main.startDelay = 0.1f;                                                             //set a small start delay to wait for the position modification to take effect
                main.startSpeed = new ParticleSystem.MinMaxCurve(0f, startSpeedMax);

                var emission = ps.emission;
                emission.rateOverTimeMultiplier = rateOverTime;
                emission.SetBurst(0, new ParticleSystem.Burst(0.05f, burstCount));

                //set material
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer)
                {
                    var mpb = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_BubbleColorOutline", bubbleColorOutline);
                    mpb.SetColor("_BubbleColorFill", bubbleColorFill);
                    renderer.SetPropertyBlock(mpb);
                }
            }
            ps.Play();
        }

        public void Init(PixelWater pixelWater, Transform followTarget, Color bubbleColorOutline, Color bubbleColorFill, float duration = 1.0f, float burstCount = 15.0f, float rateOverTime = 20.0f, float startSpeedMax = 1.5f)
        {
            this.followTarget = followTarget;
            Init ( pixelWater, followTarget.position, bubbleColorOutline, bubbleColorFill, duration, burstCount, rateOverTime, startSpeedMax);
        }


        private void Update()
        {
            //follow the target collider
            if (followTarget )
            {
                Vector3 pos = followTarget.position;
                pos = water.transform.InverseTransformPoint(pos);
                pos.z = -0.1f;
                transform.localPosition = pos;
            }
        }
    }
}
