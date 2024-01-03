using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

namespace UnityEditorEx
{
    public static partial class ModEditorUtils
    {
        private static System.Func<EditorWindow> _GetMainGameView;
        public static EditorWindow GetMainGameView()
        {
            if (_GetMainGameView == null)
            {
                System.Type tGameView = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                System.Reflection.MethodInfo mGetMainGameView = tGameView.GetMethod("GetMainGameView", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                _GetMainGameView = (System.Func<EditorWindow>)System.Delegate.CreateDelegate(typeof(System.Func<EditorWindow>), mGetMainGameView);
            }
            return _GetMainGameView();
        }
        private static System.Func<EditorWindow, Vector2> _GetGameViewSize;
        private static Vector2 GetGameViewSize(EditorWindow win)
        {
            if (_GetGameViewSize == null)
            {
                System.Type tGameView = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                System.Reflection.PropertyInfo pGameViewSize = tGameView.GetProperty("currentGameViewSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                System.Type tGameViewSize = System.Type.GetType("UnityEditor.GameViewSize,UnityEditor");
                System.Reflection.PropertyInfo pWidth = tGameViewSize.GetProperty("width", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                System.Reflection.PropertyInfo pHeight = tGameViewSize.GetProperty("height", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                var pwin = System.Linq.Expressions.Expression.Parameter(typeof(EditorWindow), "win");
                var vsize = System.Linq.Expressions.Expression.Parameter(tGameViewSize, "size");
                var assign = System.Linq.Expressions.Expression.Assign(vsize, System.Linq.Expressions.Expression.Property(System.Linq.Expressions.Expression.Convert(pwin, tGameView), pGameViewSize));
                var result = System.Linq.Expressions.Expression.Parameter(typeof(Vector2), "result");
                var resultinit = System.Linq.Expressions.Expression.Assign(result, System.Linq.Expressions.Expression.New(typeof(Vector2)));
                var setwidth = System.Linq.Expressions.Expression.Assign(System.Linq.Expressions.Expression.Field(result, "x"), System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.Property(vsize, pWidth), typeof(float)));
                var setheight = System.Linq.Expressions.Expression.Assign(System.Linq.Expressions.Expression.Field(result, "y"), System.Linq.Expressions.Expression.Convert(System.Linq.Expressions.Expression.Property(vsize, pHeight), typeof(float)));

                _GetGameViewSize = System.Linq.Expressions.Expression.Lambda<System.Func<EditorWindow, Vector2>>(System.Linq.Expressions.Expression.Block(
                    typeof(Vector2),
                    new[] { vsize, result },
                    assign,
                    resultinit,
                    setwidth,
                    setheight,
                    result
                    ), pwin).Compile();
            }
            return _GetGameViewSize(win);
        }
        public static Vector2 GetMainGameViewSize()
        {
            return GetGameViewSize(GetMainGameView());
        }
        private static System.Func<Vector2> _GetMainGameViewTargetSize;
        public static Vector2 GetMainGameViewTargetSize()
        {
            if (_GetMainGameViewTargetSize == null)
            {
                System.Type tGameView = System.Type.GetType("UnityEditor.GameView,UnityEditor");
                System.Reflection.MethodInfo mGetMainGameViewTargetSize = tGameView.GetMethod("GetMainGameViewTargetSize", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                _GetMainGameViewTargetSize = (System.Func<Vector2>)System.Delegate.CreateDelegate(typeof(System.Func<Vector2>), mGetMainGameViewTargetSize);
            }
            return _GetMainGameViewTargetSize();
        }
    }
}