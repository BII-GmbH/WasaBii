using UnityEngine;

namespace BII.Utilities.Unity {
    public static class LineRendererExtensions {

        public static Vector3[] GetPositions(this LineRenderer renderer) {
            var ret = new Vector3[renderer.positionCount];
            renderer.GetPositions(ret);
            return ret;
        }
    }
}