using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Cainos.Common
{
    public static class LayerUtils
    {
        //if the layerMask contains the given layer
        public static bool Contains(this LayerMask layerMask, int layer)
        {
            return (layerMask.value & 1 << layer) > 0;
        }

        //set the Tranform and all its children to the given layer
        public static void SetLayerAllChildren(this Transform root, int layer)
        {
            var children = root.GetComponentsInChildren<Transform>(includeInactive: true);
            foreach (var child in children)
            {
                child.gameObject.layer = layer;
            }
        }
    }
}
