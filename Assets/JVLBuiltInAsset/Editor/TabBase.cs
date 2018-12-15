using UnityEngine;

namespace JVLBuiltInAsset.Editor {
    public abstract class TabBase {
        public abstract void Draw();
        public abstract void ProccessEvent(Event e);
        protected abstract void Init();
    }
}