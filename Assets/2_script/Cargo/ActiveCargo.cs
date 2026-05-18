using UnityEngine;

[System.Serializable]
public class ActiveCargo
{
    public Cargo Cargo;
    public float ElapsedTime;
    public float DamageMultiplier = 1f;
    public float EffectTimer;
    public float SecondaryTimer;
    public int State;

    public ActiveCargo(Cargo cargo)
    {
        Cargo = cargo;
    }

    public int ComboAmount => Cargo != null ? Mathf.Max(1, Cargo.ComboAmount) : 0;
}
