using UnityEngine;

namespace radar.ui
{
    public abstract class Panel : MonoBehaviour
    {
        public abstract void Initialize();
        public virtual void Show() => gameObject.SetActive(true);
        public virtual void Hide() => gameObject.SetActive(false);
        public virtual void Update() { }
    }
}