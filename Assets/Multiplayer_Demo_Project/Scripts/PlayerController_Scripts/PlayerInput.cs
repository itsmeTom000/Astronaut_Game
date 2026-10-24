using Fusion;
using UnityEngine;

public enum PlayerButtons
{
    Fire1,
    Fire2,
    Jump,
}

public struct PlayerInput : INetworkInput
{
    public Vector2 _moveInput;
    public Vector3 _rotatingTurrent;
    public NetworkButtons _buttons;
    public NetworkBool _isBoostActivated;
    public NetworkBool _isGrounded;
    public float _mouseHorizontalInput;
    public float _mouseVerticleInput;
}