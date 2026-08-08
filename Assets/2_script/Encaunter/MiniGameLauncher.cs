using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Public asynchronous entry point for mini-games.
/// Call from gameplay: await MiniGameLauncher.PlayAsync(game, player).
/// </summary>
public static class MiniGameLauncher
{
    public const string SceneName = "MiniGame";

    private static bool isRunning;

    public static bool IsRunning => isRunning;

    public static Task<MiniGameResult> PlayAsync(MiniGameId game)
    {
        Player player = UnityEngine.Object.FindObjectOfType<Player>();
        return PlayAsync(game, player);
    }

    public static async Task<MiniGameResult> PlayAsync(
        MiniGameId game,
        Player player)
    {
        if (isRunning)
            throw new InvalidOperationException("A mini-game is already running.");

        if (player == null)
            throw new ArgumentNullException(nameof(player));

        isRunning = true;

        float previousTimeScale = Time.timeScale;
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene miniGameScene = default;
        List<Canvas> hiddenCanvases = new List<Canvas>();
        List<EventSystem> disabledEventSystems = new List<EventSystem>();

        try
        {
            Time.timeScale = 0f;
            HideSourceSceneUi(hiddenCanvases, disabledEventSystems);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                SceneName,
                LoadSceneMode.Additive);

            if (loadOperation == null)
                throw new InvalidOperationException($"Could not load scene '{SceneName}'.");

            while (!loadOperation.isDone)
                await Task.Yield();

            miniGameScene = SceneManager.GetSceneByName(SceneName);

            // Let sceneLoaded bootstrapping finish before looking for the manager.
            await Task.Yield();

            MiniGameSceneManager manager = FindManager(miniGameScene);
            if (manager == null)
                throw new InvalidOperationException(
                    $"{nameof(MiniGameSceneManager)} was not created in scene '{SceneName}'.");

            SceneManager.SetActiveScene(miniGameScene);
            return await manager.PlayAsync(game, player);
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            if (miniGameScene.IsValid() && miniGameScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(miniGameScene);
                if (unloadOperation != null)
                {
                    while (!unloadOperation.isDone)
                        await Task.Yield();
                }
            }

            Time.timeScale = previousTimeScale;
            RestoreSourceSceneUi(hiddenCanvases, disabledEventSystems);
            isRunning = false;
        }
    }

    /// <summary>
    /// Opens the existing MiniGame reward panel without starting a mini-game.
    /// The gameplay scene remains paused until the panel is closed.
    /// </summary>
    public static async Task ShowRewardAsync(Player player)
    {
        if (isRunning)
            throw new InvalidOperationException("A mini-game or reward panel is already running.");

        if (player == null)
            throw new ArgumentNullException(nameof(player));

        isRunning = true;

        float previousTimeScale = Time.timeScale;
        Scene previousActiveScene = SceneManager.GetActiveScene();
        Scene miniGameScene = default;
        List<Canvas> hiddenCanvases = new List<Canvas>();
        List<EventSystem> disabledEventSystems = new List<EventSystem>();

        try
        {
            Time.timeScale = 0f;
            HideSourceSceneUi(hiddenCanvases, disabledEventSystems);

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(
                SceneName,
                LoadSceneMode.Additive);

            if (loadOperation == null)
                throw new InvalidOperationException($"Could not load scene '{SceneName}'.");

            while (!loadOperation.isDone)
                await Task.Yield();

            miniGameScene = SceneManager.GetSceneByName(SceneName);
            await Task.Yield();

            MiniGameSceneManager manager = FindManager(miniGameScene);
            if (manager == null)
                throw new InvalidOperationException(
                    $"{nameof(MiniGameSceneManager)} was not created in scene '{SceneName}'.");

            SceneManager.SetActiveScene(miniGameScene);
            await manager.ShowSuccessRewardAsync(player);
        }
        finally
        {
            if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                SceneManager.SetActiveScene(previousActiveScene);

            if (miniGameScene.IsValid() && miniGameScene.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(miniGameScene);
                if (unloadOperation != null)
                {
                    while (!unloadOperation.isDone)
                        await Task.Yield();
                }
            }

            Time.timeScale = previousTimeScale;
            RestoreSourceSceneUi(hiddenCanvases, disabledEventSystems);
            isRunning = false;
        }
    }

    private static void HideSourceSceneUi(
        List<Canvas> hiddenCanvases,
        List<EventSystem> disabledEventSystems)
    {
        Canvas[] canvases = UnityEngine.Object.FindObjectsOfType<Canvas>(true);
        for (int i = 0; i < canvases.Length; i++)
        {
            if (!canvases[i].enabled)
                continue;

            canvases[i].enabled = false;
            hiddenCanvases.Add(canvases[i]);
        }

        EventSystem[] eventSystems =
            UnityEngine.Object.FindObjectsOfType<EventSystem>(true);

        for (int i = 0; i < eventSystems.Length; i++)
        {
            if (!eventSystems[i].enabled)
                continue;

            eventSystems[i].enabled = false;
            disabledEventSystems.Add(eventSystems[i]);
        }
    }

    private static void RestoreSourceSceneUi(
        List<Canvas> hiddenCanvases,
        List<EventSystem> disabledEventSystems)
    {
        for (int i = 0; i < hiddenCanvases.Count; i++)
        {
            if (hiddenCanvases[i] != null)
                hiddenCanvases[i].enabled = true;
        }

        for (int i = 0; i < disabledEventSystems.Count; i++)
        {
            if (disabledEventSystems[i] != null)
                disabledEventSystems[i].enabled = true;
        }
    }

    private static MiniGameSceneManager FindManager(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
            return null;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            MiniGameSceneManager manager =
                roots[i].GetComponentInChildren<MiniGameSceneManager>(true);

            if (manager != null)
                return manager;
        }

        return null;
    }
}
