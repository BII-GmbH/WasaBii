#nullable enable

using System;
using UnityEngine;

namespace BII.WasaBii.Unity {
    public static class GameObjectExtensions {
    
        public static T GetOrAddComponent<T>(
            this GameObject go, Action<T>? onAdd = null, 
            Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : Component {
            if (go.IsComponent<T>(out var res, where, includeInactive)) {
                res = go.AddComponent<T>();
                onAdd?.Invoke(res);
            }
            return res;
        }

        public static T GetOrAddIfAbsent<T>(
            this GameObject go, ref T? t, Action<T>? onAdd = null, 
            Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : Component {
            if (t.IsNull()) t = go.GetOrAddComponent(onAdd, where, includeInactive);
            return t!;
        }

        public static T? GetOrAssignIfAbsent<T>(
            this GameObject go, ref T? t,
            Search where = Search.InObjectOnly, bool includeInactive = false
        ) where T : Component {
            if (t.IsNull()) t = go.AsComponent<T>(where, includeInactive).GetOrElse(() => null!);
            return t;
        }

    }
}