using UnityEngine;

namespace Bomber.Gameplay
{
    public sealed class MatchHud : MonoBehaviour
    {
        private MatchController matchController;
        private GUIStyle panelStyle;
        private GUIStyle titleStyle;
        private GUIStyle textStyle;

        public void Initialize(MatchController controller)
        {
            matchController = controller;
        }

        private void OnGUI()
        {
            if (matchController == null || matchController.Player == null)
            {
                return;
            }

            EnsureStyles();

            Rect panelRect = new Rect(16f, 16f, 300f, 170f);
            GUI.Box(panelRect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(panelRect);
            GUILayout.Space(10f);
            GUILayout.Label("Bomber Prototype", titleStyle);
            GUILayout.Label("Lives: " + matchController.Player.Lives + "/" + matchController.Player.MaxLives, textStyle);
            GUILayout.Label("Bombs: " + matchController.Player.MaxBombs, textStyle);
            GUILayout.Label("Blast Range: " + matchController.Player.ExplosionRange, textStyle);
            GUILayout.Label("Move Speed: " + matchController.Player.MoveSpeed.ToString("0.0"), textStyle);
            GUILayout.Label("Enemies Left: " + matchController.RemainingEnemies, textStyle);
            GUILayout.Label("Crates Cleared: " + matchController.CratesDestroyed, textStyle);
            GUILayout.EndArea();

            if (matchController.MatchFinished)
            {
                DrawEndScreen();
            }
        }

        private void DrawEndScreen()
        {
            Rect boxRect = new Rect((Screen.width * 0.5f) - 170f, (Screen.height * 0.5f) - 80f, 340f, 160f);
            GUI.Box(boxRect, GUIContent.none, panelStyle);

            GUILayout.BeginArea(boxRect);
            GUILayout.Space(18f);
            GUILayout.Label(matchController.MatchWon ? "Victory" : "Defeat", titleStyle);
            GUILayout.Label(matchController.MatchWon ? "All enemies eliminated." : "You are out of lives.", textStyle);
            GUILayout.Label("Enemies defeated: " + matchController.EnemiesDefeated, textStyle);
            GUILayout.Label("Press R to restart", textStyle);
            GUILayout.EndArea();
        }

        private void EnsureStyles()
        {
            if (panelStyle != null)
            {
                return;
            }

            panelStyle = new GUIStyle(GUI.skin.box);
            panelStyle.normal.background = MakeTexture(new Color(0.07f, 0.09f, 0.08f, 0.82f));
            panelStyle.border = new RectOffset(12, 12, 12, 12);
            panelStyle.padding = new RectOffset(14, 14, 14, 14);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = new Color(0.92f, 0.94f, 0.86f);

            textStyle = new GUIStyle(GUI.skin.label);
            textStyle.fontSize = 14;
            textStyle.normal.textColor = new Color(0.82f, 0.88f, 0.78f);
        }

        private static Texture2D MakeTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }
}
