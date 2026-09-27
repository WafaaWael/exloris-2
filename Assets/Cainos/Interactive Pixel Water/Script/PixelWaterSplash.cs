using UnityEngine;

namespace Cainos.InteractivePixelWater
{
    //water splash effect control
    public class PixelWaterSplash : MonoBehaviour
    {
        public float lifetime = 2.0f;                       //time before auto destroy
        public float safeTime = 0.5f;                       //time when it is ok to add another splash at the same position
        [Space]
        public ParticleSystem splashLight;
        public ParticleSystem splashDark;
        public ParticleSystem sparkLight;
        public ParticleSystem sparkDark;

        private float timer;

        //get the world space x range occupied by this splash effect 
        //fetch from the BoxCollider2D attached to this object, if not found, return a default range of size 1
        public Vector2 Range
        {
            get
            {
                float halfSizeX = 0.5f;

                if ( TryGetComponent( out BoxCollider2D boxCollider2D))
                {
                    halfSizeX = boxCollider2D.size.x * 0.5f;
                }

                return new Vector2(transform.position.x - halfSizeX, transform.position.x + halfSizeX);
            }
        }

        //is safe time passed
        public bool IsSafeTimePassed
        {
            get { return timer > safeTime; }
        }

        public void Init(PixelWater pixelWater, Vector2 pos, Color splashColorLight, Color splashColorDark, Color splashColorOutline)
        {
            Vector3 localPos = pixelWater.transform.InverseTransformPoint(pos);

            //snap to wave position if very close
            float wavePos = pixelWater.GetWavePosLocal(localPos.x, 0.5f);
            if ( Mathf.Abs( wavePos - localPos.y) < 1.0f) localPos.y = wavePos;

            //also limit y pos to water area
            localPos.y = Mathf.Clamp(localPos.y, pixelWater.BottomPosLocal, pixelWater.TopPosLocal);

            //the splash need to be in front of the bubble, so in urp 2d, it will render after the bubble to cover up the bubble
            localPos.z = -0.2f;

            timer = 0.0f;
            transform.localPosition = localPos;

            //set material
            var mpb = new MaterialPropertyBlock();
            if (splashLight)
            {
                ParticleSystemRenderer renderer = splashLight.GetComponent<ParticleSystemRenderer>();
                if (renderer)
                {
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_SplashColor", splashColorLight);
                    mpb.SetColor("_OutlineColor", splashColorOutline);
                    renderer.SetPropertyBlock(mpb);
                }
            }
            if (splashDark)
            {
                ParticleSystemRenderer renderer = splashDark.GetComponent<ParticleSystemRenderer>();
                if (renderer)
                {
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_SplashColor", splashColorDark);
                    mpb.SetColor("_OutlineColor", splashColorOutline);
                    renderer.SetPropertyBlock(mpb);
                }
            }
            if (sparkLight)
            {
                ParticleSystemRenderer renderer = sparkLight.GetComponent<ParticleSystemRenderer>();
                if (renderer)
                {
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_SparkColor", splashColorLight);
                    mpb.SetColor("_OutlineColor", splashColorOutline);
                    renderer.SetPropertyBlock(mpb);
                }
            }
            if (sparkDark)
            {
                ParticleSystemRenderer renderer = sparkDark.GetComponent<ParticleSystemRenderer>();
                if (renderer)
                {
                    renderer.GetPropertyBlock(mpb);
                    mpb.SetColor("_SparkColor", splashColorDark);
                    mpb.SetColor("_OutlineColor", splashColorOutline);
                    renderer.SetPropertyBlock(mpb);
                }
            }
        }

        private void Update()
        {
            timer += Time.deltaTime;
            if ( timer > lifetime) Destroy(gameObject);
        }
    }
}
