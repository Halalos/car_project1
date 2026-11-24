using UnityEngine;

namespace Cars
{
    public sealed class ChaseCamera : MonoBehaviour
    {
        public ArcadeCarController target;
        Camera view;
        public bool menu = true;
        void Awake() { view = GetComponent<Camera>(); }
        void LateUpdate()
        {
            if (target == null) return;
            float dt = Time.unscaledDeltaTime;
            float speed = target.Speed;
            Vector3 desired = target.transform.position + new Vector3(0, 6.8f + speed * .018f, -11.5f - speed * .03f);
            float shake = target.HitFlash * .12f;
            desired += new Vector3(Mathf.Sin(Time.unscaledTime * 83), Mathf.Cos(Time.unscaledTime * 71), 0) * shake;
            if (menu) desired = target.transform.position + new Vector3(9, 7, -12);
            transform.position = Vector3.Lerp(transform.position, desired, 1 - Mathf.Exp(-8 * dt));
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(target.transform.position + Vector3.up * .8f + Vector3.forward * (menu ? 0 : 6) - transform.position),
                1 - Mathf.Exp(-12 * dt));
            if (view != null) view.fieldOfView = Mathf.Lerp(view.fieldOfView, 56 + speed * .30f, 1 - Mathf.Exp(-3 * dt));
        }
        public void Snap()
        { if (target != null) transform.position = target.transform.position + new Vector3(0, 7, -13); }
    }
}
