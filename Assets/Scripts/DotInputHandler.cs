using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class DotInputHandler : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
    {
        public void OnPointerEnter(PointerEventData eventData)
        {
#if UNITY_EDITOR
            if (!Input.GetMouseButton(0)) return;
#endif
            DotsController.Instance.OnDotSelected(this);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnPointerEnter(eventData);
        }
    }
}