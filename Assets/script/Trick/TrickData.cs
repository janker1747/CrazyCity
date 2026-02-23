using UnityEngine;

[CreateAssetMenu(fileName = "NewTrick", menuName = "Tricks/Trick")]
public class TrickData : ScriptableObject
{
    public string trickName;
    public string animatorTrigger;
    public Sprite trickIcon;
    public int score;
    public float duration;
}
