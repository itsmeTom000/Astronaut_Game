using UnityEngine;
using System.Collections.Generic;
using Fusion;

// Shared struct defining what data we send
[System.Serializable]
public struct VertexDeltaData
{
    public int vertexIndex;
    public Vector3 newPosition;
}

[RequireComponent(typeof(MeshFilter))]
public class LocalMeshDeformer : MonoBehaviour
{
    private Mesh _mesh;
    private Vector3[] _currentVertices;
    private Vector3[] _lastSyncedVertices;

    [Header("Dependencies")]
    [Tooltip("Drag the ReliableDataStreamer script attached to this object here")]
    public ReliableDataStreamer networkStreamer;

    [Header("Settings")]
    [Tooltip("Minimum distance a vertex must move to be sent over the network")]
    public float changeThreshold = 0.001f;

    void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
        _mesh.MarkDynamic(); // Optimize for frequent CPU updates

        _currentVertices = _mesh.vertices;
        _lastSyncedVertices = _mesh.vertices;
    }

    [ContextMenu("Deform Randomly")]
    public void DeformRandomly()
    {
        float intensity = .1f;
        for (int i = 0; i < _currentVertices.Length; i++)
        {
            Vector3 randomOffset = new Vector3(
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f),
                Random.Range(-1f, 1f)
            ) * intensity;

            _currentVertices[i] += randomOffset;
        }

        _mesh.vertices = _currentVertices;
        _mesh.RecalculateNormals();

        ExtractAndPassChangesToNetwork();
    }

    private void ExtractAndPassChangesToNetwork()
    {
        List<VertexDeltaData> changedVertices = new();
        float sqrThreshold = changeThreshold * changeThreshold;

        for (int i = 0; i < _currentVertices.Length; i++)
        {
            if ((_currentVertices[i] - _lastSyncedVertices[i]).sqrMagnitude > sqrThreshold)
            {
                changedVertices.Add(new VertexDeltaData
                {
                    vertexIndex = i,
                    newPosition = _currentVertices[i]
                });

                _lastSyncedVertices[i] = _currentVertices[i];
            }
        }

        if (changedVertices.Count > 0)
        {
            networkStreamer.QueueDataForStreaming(changedVertices.ToArray());
        }
    }

    public void ApplyIncomingNetworkData(VertexDeltaData[] incomingDeltas)
    {
        foreach (VertexDeltaData delta in incomingDeltas)
        {
            _currentVertices[delta.vertexIndex] = delta.newPosition;
            _lastSyncedVertices[delta.vertexIndex] = delta.newPosition;
        }

        _mesh.vertices = _currentVertices;
        _mesh.RecalculateNormals();
    }
}