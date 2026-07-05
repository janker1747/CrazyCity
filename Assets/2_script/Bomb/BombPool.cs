using UnityEngine;

public class BombPool : ObjectPool<Bomb>
{
    public Bomb SpawnBomb(Transform spawnPoint)
    {
        Bomb bomb = GetObject(spawnPoint);
        bomb.Initialize(this);
        return bomb;
    }
}