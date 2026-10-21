using FarmFuryStampede.UI;
using UnityEditor;
using UnityEngine;

namespace FarmFuryStampede.EditorTools
{
    /// <summary>
    /// The F1 debug panel's controls as a dockable editor tab (menu Farm Fury Stampede > Debug > Test Panel), so they
    /// can be used without the Game view having focus. Same list as the overlay (DebugPanel.DrawRows): unlocks, paid
    /// worlds bought / re-locked, coins, rare pellets, the cherry power, reset save, cosmetics. Works in Play mode.
    /// </summary>
    public class StampedeTestPanel : EditorWindow
    {
        private Vector2 _scroll;

        [MenuItem("Farm Fury Stampede/Debug/Test Panel")]
        public static void Open()
        {
            GetWindow<StampedeTestPanel>("Stampede Test Panel").Show();
        }

        private void OnInspectorUpdate()
        {
            if (Application.isPlaying) { Repaint(); }   // keeps coin counts and toggles current
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Press Play: these controls change the running game's save.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            System.Action pending = null;   // run after drawing, so a refreshed screen doesn't change the layout mid-draw
            DebugPanel.DrawRows(
                (text, action) => { if (GUILayout.Button(text, GUILayout.Height(26))) { pending = action; } },
                text => { EditorGUILayout.Space(6); EditorGUILayout.LabelField(text, EditorStyles.boldLabel); });
            EditorGUILayout.EndScrollView();

            if (pending != null)
            {
                DebugPanel.Run(pending);
            }
        }
    }
}
