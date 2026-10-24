using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

public class PlayerController : NetworkBehaviour, IAfterSpawned
{
    private static readonly int YVelocityHash = Animator.StringToHash("Y_Velocity");
    private static readonly int XVelocityHash = Animator.StringToHash("X_Velocity");

    #region Networked Properties
    [Networked] private PlayerInput PlayerInput { get; set; }
    [Networked] private NetworkButtons PreviousButtons { get; set; }
    #endregion

    #region Player Animation
    [SerializeField] private Animator _playerAnimator;
    #endregion

    #region Camera References
    [SerializeField] private Transform _cameraTransform;
    #endregion

    #region Player Settings
    [SerializeField] private float _movementSpeed = 5f;
    [SerializeField] private float _jumpForce = 8f;
    [SerializeField] private bool _requireGrounded = true;
    [SerializeField] private float _groundCheckDistance = 0.3f;
    #endregion

    #region Private Variables
    private Rigidbody _networkRigidbody;
    #endregion

    #region Fusion Callbacks
    public override void Spawned()
    {
        _networkRigidbody = GetComponent<Rigidbody>();
    }

    public async void AfterSpawned()
    {
        if (Object.HasInputAuthority)
        {
            while (Camera.main == null)
                await System.Threading.Tasks.Task.Yield();
            Camera.main.transform.SetParent(_cameraTransform);
            Camera.main.transform.SetLocalPositionAndRotation(new Vector3(0f, 4f, -5f), Quaternion.Euler(10f, 0f, 0f));
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out PlayerInput input))
        {
            PlayerInput = input;

            // Call movement and jump logic only for entities processing valid inputs
            Movement();
            HandlingJump(input);

            // Store current buttons as previous for next tick comparison
            PreviousButtons = input._buttons;
        }
    }

    public override void Render()
    {
        UpdateAnimation();
    }
    #endregion

    #region Player Movement
    private void Movement()
    {
        Vector3 _movementVector = (transform.right * PlayerInput._moveInput.x) + (transform.forward * PlayerInput._moveInput.y);

        _movementVector.Normalize();

        // CORRECT PHYSICS MOVEMENT: Apply velocity instead of modifying transform.position directly.
        // We preserve the current Y velocity so gravity still works naturally.
        Vector3 targetVelocity = _movementVector * _movementSpeed;
        _networkRigidbody.linearVelocity = new Vector3(targetVelocity.x, _networkRigidbody.linearVelocity.y, targetVelocity.z);
    }
    #endregion

    #region Player Animation
    private void UpdateAnimation()
    {
        if (_playerAnimator == null) return;

        _playerAnimator.SetFloat(YVelocityHash, PlayerInput._moveInput.y);
        _playerAnimator.SetFloat(XVelocityHash, PlayerInput._moveInput.x);
    }

    private void HandlingJump(PlayerInput input)
    {
        // 1. Compare current buttons against previous tick's buttons (ignores held buttons)
        NetworkButtons pressedButtons = input._buttons.GetPressed(PreviousButtons);

        // 2. Check if the Jump button was JUST pressed down this tick
        bool jumpPressed = pressedButtons.IsSet(PlayerButtons.Jump);

        if (jumpPressed)
        {
            // Reset Y velocity before jumping so downward momentum doesn't cancel or weaken the jump
            _networkRigidbody.linearVelocity = new Vector3(_networkRigidbody.linearVelocity.x, 0f, _networkRigidbody.linearVelocity.z);
            _networkRigidbody.AddForce(Vector3.up * _jumpForce, ForceMode.Impulse);
        }
    }

    #endregion
}
