using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dots.Misc
{
    public class CoroutineRunner : MonoBehaviour
    {
        public void RunCoroutine(IEnumerator coroutine)
        {
            IEnumerator Animator()
            {
                yield return coroutine;

                Destroy(this);
            }

            StartCoroutine(Animator());
        }
    }
}