using UnityEngine;
using UnityEngine.EventSystems;

namespace Assets.Scripts
{
    public class LookAtTest : MonoBehaviour, IPointerMoveHandler
    {
        public Transform transform;

        public void OnPointerMove(PointerEventData eventData)
        {
            transform.rotation = Quaternion.Euler(0, 0, AngleBetweenTwoPoints(transform.position, eventData.position) - 90);
        }

        private float AngleBetweenTwoPoints(Vector3 a, Vector3 b)
        {
            return Mathf.Atan2(a.y - b.y, a.x - b.x) * Mathf.Rad2Deg;
        }
    }
}