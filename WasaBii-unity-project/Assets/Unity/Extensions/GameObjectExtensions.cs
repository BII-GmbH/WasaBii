﻿using System;
using BII.WasaBii.Unity;
using UnityEngine;

namespace BII.Utilities.Unity {
    public static class GameObjectExtensions {
    
        public static T GetOrAddComponent<T>(this GameObject go, Action<T> onAdd = null, Search where = Search.InObjectOnly) where T : Component {
            var res = go.As<T>(where);
            if (res == null) {
                res = go.AddComponent<T>();
                onAdd?.Invoke(res);
            }
            return res;
        }

        public static T GetOrAddIfAbsent<T>(this GameObject go, ref T t, Action<T> onAdd = null, Search where = Search.InObjectOnly) where T : Component {
            if (t.IsNull()) t = go.GetOrAddComponent(onAdd, where);
            return t;
        }

        public static T GetOrAssignIfAbsent<T>(this GameObject go, ref T t, Search where = Search.InObjectOnly) where T : Component {
            if (t.IsNull()) t = go.As<T>(where);
            return t;
        }
    }
}
