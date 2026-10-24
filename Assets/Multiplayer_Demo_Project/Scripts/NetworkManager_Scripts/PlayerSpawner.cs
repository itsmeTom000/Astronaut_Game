using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    #region Inspector Variables
    [SerializeField] private NetworkPrefabRef _networkPlayerRef;
    [SerializeField] private bool _isDebugEnabled = false;
    #endregion

    #region Private Fields
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    #endregion

    #region Unity Callbacks
    private void OnEnable()
    {
        FusionSessionManager.OnPlayerJoinedEvent += OnPlayerJoined;
        FusionSessionManager.OnPlayerLeftEvent += OnPlayerLeft;
    }

    private void OnDisable()
    {
        FusionSessionManager.OnPlayerJoinedEvent -= OnPlayerJoined;
        FusionSessionManager.OnPlayerLeftEvent -= OnPlayerLeft;
    }
    #endregion

    private void OnPlayerJoined(NetworkRunner networkRunner, PlayerRef player)
    {
        if (FusionSessionManager.Instance._runnerInstance.IsServer)
        {
            NetworkObject playerObject = FusionSessionManager.Instance._runnerInstance.Spawn(_networkPlayerRef, Vector3.zero, Quaternion.identity, player);

            FusionSessionManager.Instance._runnerInstance.SetPlayerObject(player, playerObject);

            _spawnedPlayers.Add(player, playerObject);

            if (_isDebugEnabled) Debug.Log($"Player {player.PlayerId} joined and spawned.");
        }
    }

    private void OnPlayerLeft(NetworkRunner networkRunner, PlayerRef player)
    {
        if (FusionSessionManager.Instance._runnerInstance.IsServer)
        {
            if (_spawnedPlayers.TryGetValue(player, out var playerObject))
            {
                FusionSessionManager.Instance._runnerInstance.Despawn(playerObject);
                _spawnedPlayers.Remove(player);

                if (_isDebugEnabled) Debug.Log($"Player {player.PlayerId} left and despawned.");
            }
        }
    }
}
