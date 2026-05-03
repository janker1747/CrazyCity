using UnityEngine;

public class PlayerTrickLoadout : MonoBehaviour
{
    public TrickData trickQ;
    public TrickData trickE;

    public TrickData GetTrickForKey(KeyCode key)
    {
        return key switch
        {
            KeyCode.Q => trickQ,
            KeyCode.E => trickE,
            _ => null
        };
    }

    public void SetTrick(KeyCode key, TrickData trick)
    {
        switch (key)
        {
            case KeyCode.Q: trickQ = trick; break;
            case KeyCode.E: trickE = trick; break;
        }
    }
}
