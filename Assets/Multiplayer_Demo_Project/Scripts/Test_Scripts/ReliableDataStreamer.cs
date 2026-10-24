using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ReliableDataStreamer : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Dependencies")]
    [Tooltip("Drag the LocalMeshDeformer script here")]
    public LocalMeshDeformer meshDeformer;

    [Header("Debug")]
    public bool showNetworkDebug = true;

    // Unique key so we don't mix up data streams if multiple meshes are deforming
    private ReliableKey _vertexDataKey;

    public override void Spawned()
    {
        _vertexDataKey = ReliableKey.FromInts((int)Object.Id.Raw, 0, 0, 0);
        Runner.AddCallbacks(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        Runner.RemoveCallbacks(this);
    }

    /// <summary>
    /// Called by LocalMeshDeformer to send the vertex changes over the network.
    /// </summary>
    public void QueueDataForStreaming(VertexDeltaData[] vertexData)
    {
        if (Runner == null || !Runner.IsRunning) return;

        // 1. Serialize the struct array into a raw byte[]
        byte[] payload = Serialize(vertexData);

        // --- DEBUG LOGGING ---
        if (showNetworkDebug)
        {
            float payloadSizeKB = payload.Length / 1024f;
            Debug.Log($"[MeshStreamer] Sending {vertexData.Length} vertices. Payload Size: {payloadSizeKB:F2} KB per player.");
        }

        // 2. Send to all other players in the Shared session
        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            if (player != Runner.LocalPlayer)
            {
                Runner.SendReliableDataToPlayer(player, _vertexDataKey, payload);
            }
        }
    }

    /// <summary>
    /// Callback triggered by Fusion 2.1 when reliable data is fully received.
    /// </summary>
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ReadOnlySpan<byte> data)
    {
        if (key == _vertexDataKey)
        {
            // --- DEBUG LOGGING ---
            if (showNetworkDebug)
            {
                float receivedSizeKB = data.Length / 1024f;
                Debug.Log($"[MeshStreamer] Received payload from Player {player}. Size: {receivedSizeKB:F2} KB.");
            }

            // Deserialize the byte span back into our struct array
            VertexDeltaData[] receivedDeltas = Deserialize(data.ToArray());

            // Apply it to the mesh visually
            meshDeformer.ApplyIncomingNetworkData(receivedDeltas);
        }
    }

    // -----------------------------------------------------------
    // SERIALIZATION UTILITIES
    // -----------------------------------------------------------

    private byte[] Serialize(VertexDeltaData[] data)
    {
        using MemoryStream stream = new();
        using BinaryWriter writer = new(stream);
        writer.Write(data.Length);
        foreach (VertexDeltaData delta in data)
        {
            writer.Write(delta.vertexIndex);     // 4 bytes
            writer.Write(delta.newPosition.x);   // 4 bytes
            writer.Write(delta.newPosition.y);   // 4 bytes
            writer.Write(delta.newPosition.z);   // 4 bytes
        }
        return stream.ToArray(); // Total: ~16 bytes per modified vertex + 4 bytes for array length
    }

    private VertexDeltaData[] Deserialize(byte[] bytes)
    {
        using MemoryStream stream = new(bytes);
        using BinaryReader reader = new(stream);
        int length = reader.ReadInt32();
        VertexDeltaData[] data = new VertexDeltaData[length];
        for (int i = 0; i < length; i++)
        {
            data[i] = new VertexDeltaData
            {
                vertexIndex = reader.ReadInt32(),
                newPosition = new Vector3(
                    reader.ReadSingle(),
                    reader.ReadSingle(),
                    reader.ReadSingle()
                )
            };
        }
        return data;
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}