using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Dots.Misc
{
    public class DotSelectAnimationController : MonoBehaviour
    {
        private RectTransform _rt;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            _rt.sizeDelta = Vector2.zero;
        }

        public void Animate(Vector2 defaultSize, Vector2 finalSize, float resizeTime = 0.6f, float disappearThreshold = 0.1f, float disappearTime = 0.5f)
        {
            IEnumerator DisappearAnimator()
            {
                float timer = 0;

                Image image = GetComponent<Image>();
                Color transparentColor = new Color(0, 0, 0, 0);
                Color defaultColor = image.color;

                while (timer < disappearTime)
                {
                    image.color = Color.Lerp(defaultColor, transparentColor, timer / disappearTime);

                    timer += Time.deltaTime;
                    yield return null;
                }

                image.color = transparentColor;

                Destroy(gameObject);
            }

            IEnumerator Animator()
            {
                bool disappearCalled = false;

                float timer = 0;
                while (timer < resizeTime)
                {
                    _rt.sizeDelta = Vector2.Lerp(Vector2.zero, finalSize, timer / resizeTime);

                    if (timer > disappearThreshold && !disappearCalled)
                    {
                        disappearCalled = true;

                        StartCoroutine(DisappearAnimator());
                    }

                    timer += Time.deltaTime;
                    yield return null;
                }

                _rt.sizeDelta = finalSize;
            }

            StartCoroutine(Animator());
        }
    }
}