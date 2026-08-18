using System.Collections;
using UnityEngine;

namespace MTA.App
{
    // Small scale-punch on button press. Attached by UIFactory to every button.
    public class ButtonPunch : MonoBehaviour
    {
        public void Punch()
        {
            if (!isActiveAndEnabled) return;
            StopAllCoroutines();
            StartCoroutine(Anim());
        }

        IEnumerator Anim()
        {
            var rt = transform as RectTransform; if (rt == null) yield break;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / 0.16f;
                rt.localScale = Vector3.one * (1f - Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.09f);
                yield return null;
            }
            rt.localScale = Vector3.one;
        }
    }
}
