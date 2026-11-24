using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace Cars
{
    // Opt-in automation only; normal players never create this component or use a test profile.
    public sealed class StandaloneQuitSmoke : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void EnableWhenRequested()
        {
            if (Array.IndexOf(System.Environment.GetCommandLineArgs(), "-cars-quit-smoke") < 0) return;
            GameSession.SaveDirectoryOverride = Path.Combine(Application.temporaryCachePath,"CarsQuitSmoke-" + Guid.NewGuid().ToString("N"));
            var root = new GameObject("Standalone Quit Smoke", typeof(StandaloneQuitSmoke)); DontDestroyOnLoad(root);
        }
        IEnumerator Start()
        {
            float until = Time.realtimeSinceStartup + 25;
            GameSession game = null;
            while (Time.realtimeSinceStartup < until)
            {
                game = FindObjectOfType<GameSession>();
                if (game != null && game.Saves != null && game.Saves.Ready && game.Player != null) break;
                yield return null;
            }
            if (game == null || game.Player == null || game.audioEffects == null || game.audioEffects.engineClip == null)
            { Debug.LogError("CARS_PLAYER_QUIT_SMOKE_FAILED: game or audio did not initialize."); Application.Quit(2); yield break; }
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null)
            { Debug.LogError("CARS_PLAYER_QUIT_SMOKE_FAILED: no render pipeline."); Application.Quit(2); yield break; }
            yield return null;
            Debug.Log("CARS_PLAYER_QUIT_SMOKE_UI_CLICK: initialized with audio and URP; invoking actual Quit button.");
            game.ui.transform.Find("Screen_Main/Quit").GetComponent<Button>().onClick.Invoke();
            yield return new WaitForSecondsRealtime(8);
            Debug.LogError("CARS_PLAYER_QUIT_SMOKE_FAILED: Quit did not exit within 8 seconds.");
        }
    }
}
