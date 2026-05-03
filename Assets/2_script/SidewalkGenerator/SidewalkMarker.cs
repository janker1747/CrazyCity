using UnityEngine;

[DisallowMultipleComponent]
public sealed class SidewalkMarker : MonoBehaviour
{
    private const string GeneratorId = "SidewalkGenerator";

    [SerializeField] private string generatorId = GeneratorId;
    [SerializeField] private bool container;

    public bool IsContainer
    {
        get { return container; }
    }

    public bool IsSidewalkGeneratorMarker
    {
        get { return generatorId == GeneratorId; }
    }

    public void Configure(bool isContainer)
    {
        generatorId = GeneratorId;
        container = isContainer;
    }
}
