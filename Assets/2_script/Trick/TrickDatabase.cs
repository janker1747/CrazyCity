using UnityEngine;

[CreateAssetMenu(fileName = "TrickDatabase", menuName = "Tricks/Database")]
public class TrickDatabase : ScriptableObject
{
    public TrickData[] allTricks;
}
