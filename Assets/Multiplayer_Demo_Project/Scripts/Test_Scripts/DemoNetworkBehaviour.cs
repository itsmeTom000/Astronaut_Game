using UnityEngine;
using Fusion;

public class DemoNetworkBehaviour : NetworkBehaviour, IAfterSpawned
{

    public override void Spawned()
    {
        Debug.Log($"Spawned: {gameObject.name} with NetworkObject ID: {Object.InputAuthority}");
    }

    void IAfterSpawned.AfterSpawned()
    {
        Debug.Log($"AfterSpawned: {gameObject.name} with NetworkObject ID: {Object.InputAuthority}");
    }
}
