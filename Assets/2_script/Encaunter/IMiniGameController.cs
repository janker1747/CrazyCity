using System;

/// <summary>
/// Common contract used by MiniGameSceneManager to run any mini-game panel.
/// </summary>
public interface IMiniGameController
{
    event Action<bool> Finished;

    void Begin();
}
