using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInputController : NetworkBehaviour, IBeforeTick, INetworkRunnerCallbacks
{
    #region Enums
    private enum InputAxis
    {
        Horizontal,
        Vertical,
    }
    #endregion

    #region Private Variables
    private Vector2 _movementVector;
    private Quaternion _rotationQuaternion;
    private bool _fire1Pressed;
    private bool _fire2Pressed;
    private bool _jump;
    #endregion

    #region Unity Callbacks
    private void Update()
    {
        // Only sample hardware input on the local client that has input authority
        if (Object == null || !Object.HasInputAuthority) return;

        _movementVector = new Vector2(
            Input.GetAxis(InputAxis.Horizontal.ToString()),
            Input.GetAxis(InputAxis.Vertical.ToString())
        );

        // Accumulate edge-triggered button presses in Update so they aren't missed between ticks
        _fire1Pressed |= Input.GetMouseButtonDown(0);
        _fire2Pressed |= Input.GetMouseButtonDown(1);
        _jump |= Input.GetKeyDown(KeyCode.Space);
    }
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        // Only register runner input callbacks on the client with input authority
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        runner.RemoveCallbacks(this);
    }
    #endregion

    #region Providing Input
    void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
    {
        PlayerInput playerInput = new()
        {
            _moveInput = _movementVector.normalized,
            _rotatingTurrent = _rotationQuaternion.eulerAngles,
            _isBoostActivated = Input.GetKey(KeyCode.LeftShift),
            _buttons = new NetworkButtons()
        };

        playerInput._buttons.Set(PlayerButtons.Fire1, _fire1Pressed);
        playerInput._buttons.Set(PlayerButtons.Fire2, _fire2Pressed);
        playerInput._buttons.Set(PlayerButtons.Jump, _jump);

        input.Set(playerInput);

        // Reset one-shot button flags after sending them to Fusion
        _fire1Pressed = false;
        _fire2Pressed = false;
        _jump = false;
    }
    #endregion

    #region INetworkRunnerCallbacks Implementation
    public void BeforeTick()
    {
    }

    void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
    }

    void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
    }

    void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
    }

    void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }

    void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
    {
    }

    void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
    {
    }

    void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
    {
    }

    #endregion
}
