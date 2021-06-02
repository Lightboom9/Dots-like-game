using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class DotsParentResizer : MonoBehaviour
{
    private void Awake()
    {
        float side = Mathf.Min(Screen.width, Screen.height);
        side *= 900 / 1080f;

        GetComponent<RectTransform>().sizeDelta = new Vector2(side, side);
    }
}
