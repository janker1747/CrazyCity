using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ensures that the MiniGame canvas always owns its central manager, including
/// when the scene is opened additively by MiniGameLauncher.
/// </summary>
public static class MiniGameSceneBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != MiniGameLauncher.SceneName)
            return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Canvas canvas = roots[i].GetComponentInChildren<Canvas>(true);
            if (canvas == null)
                continue;

            if (canvas.GetComponent<MiniGameSceneManager>() == null)
                canvas.gameObject.AddComponent<MiniGameSceneManager>();

            return;
        }

        Debug.LogError("MiniGame scene does not contain a Canvas.");
    }
}
