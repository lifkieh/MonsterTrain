using UnityEngine;
using UnityEngine.UI;

namespace MTA.Battle
{
    // Free-form mesh-deform sprite (the "animator" — code, no new art). Renders a monster
    // sprite on an N×M grid whose vertices are pushed every frame by procedural fields —
    // breathing, idle limb-sway, attack bend, hit wobble. Because the GEOMETRY changes each
    // frame (not just a transform), a single-frame sprite now reads as ANIMATED even paused:
    // the top of the body leans/sways while the feet stay planted, limbs ripple. Works on any
    // sprite (non-readable Resources textures included) — it deforms the quad, never the pixels.
    //
    // Deform params are pushed in by UnitView so the body + its outline + its flash copy all
    // share the SAME shape. Cosmetic only — never touches the sim.
    [RequireComponent(typeof(CanvasRenderer))]
    public class DeformSprite : Graphic
    {
        [System.NonSerialized] public Sprite sprite;
        const int CX = 5, CY = 7;                 // grid cells (verts = (CX+1)×(CY+1))

        // Live params (set by UnitView each frame; local space, px).
        public float phase;                       // per-unit phase seed
        public float breathe;                     // vertical breathing scale at the top (±)
        public float sway;                        // idle horizontal sway of the upper body (px)
        public float lean;                        // attack/anticipation bend of the upper body (px)
        public float wobbleAmp, wobbleT;          // hit ripple
        public float limb = 1f;                   // idle limb-ripple strength (0 when busy)
        public float squashX = 1f, squashY = 1f;  // external squash impulse (from UnitView.Squash)

        public override Texture mainTexture => sprite != null ? sprite.texture : base.mainTexture;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (sprite == null) return;

            var r = GetPixelAdjustedRect();
            float x0 = r.xMin, y0 = r.yMin, w = r.width, h = r.height;

            // UV rect of the sprite within its texture (handles tight/atlas sprites).
            var tr = sprite.textureRect; var tex = sprite.texture;
            float uMin = tr.xMin / tex.width, vMin = tr.yMin / tex.height;
            float uW = tr.width / tex.width, vH = tr.height / tex.height;

            float t = animTime;
            for (int yi = 0; yi <= CY; yi++)
            {
                float v = yi / (float)CY;                       // 0 feet → 1 head
                for (int xi = 0; xi <= CX; xi++)
                {
                    float u = xi / (float)CX;                   // 0 left → 1 right
                    float cx = (u - 0.5f);                      // -0.5..0.5 from centre

                    // --- procedural deform (all scaled by height so feet stay planted) ---
                    float up = v * v;                           // weight toward the top
                    float dx = 0f, dy = 0f;

                    dx += (sway + lean) * up;                                   // sway + attack bend at the top
                    dx += Mathf.Sin(v * 6.28318f + t * 3.0f + phase) * 2.2f * limb * v;   // travelling limb ripple
                    dx += Mathf.Sin(t * 22f + v * 9f) * wobbleAmp * up;        // hit wobble
                    dy += breathe * up;                                        // chest rises on the breath
                    dx += cx * (squashX - 1f) * w * 0.5f;                      // squash widens
                    dy += (v - 0.5f) * (squashY - 1f) * h;                     // squash shortens

                    float px = x0 + u * w * (1f + (squashX - 1f) * 0f) + dx;
                    float py = y0 + v * h + dy;
                    AddVert(vh, px, py, uMin + u * uW, vMin + v * vH);
                }
            }
            int stride = CX + 1;
            for (int yi = 0; yi < CY; yi++)
                for (int xi = 0; xi < CX; xi++)
                {
                    int i0 = yi * stride + xi, i1 = i0 + 1, i2 = i0 + stride, i3 = i2 + 1;
                    vh.AddTriangle(i0, i2, i1);
                    vh.AddTriangle(i1, i2, i3);
                }
        }

        void AddVert(VertexHelper vh, float x, float y, float u, float v)
        {
            var vert = UIVertex.simpleVert;
            vert.color = color;
            vert.position = new Vector3(x, y, 0f);
            vert.uv0 = new Vector4(u, v, 0f, 0f);
            vh.AddVert(vert);
        }

        float animTime;
        void Update()
        {
            animTime += Time.deltaTime;
            if (wobbleT > 0f) wobbleT -= Time.deltaTime;
            SetVerticesDirty();   // rebuild the deformed mesh every frame
        }
    }
}
