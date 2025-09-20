using UnityEngine;

/// <summary>
/// Simple runtime camera controller: WASD/QE/Space/Ctrl to move, RMB to look, Shift to boost.
/// Attach to a Camera GameObject.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
  [Header("Movement")]
  public float moveSpeed = 5.0f;
  public float boostMultiplier = 3.0f;

  [Header("Discrete Step")]
  public float stepDistance = 50.0f;

  [Header("Look")]
  public float mouseSensitivity = 2.0f;
  public bool holdRightMouseToLook = true;

  private float _yaw;
  private float _pitch;

  private void Start()
  {
    var euler = transform.eulerAngles;
    _yaw = euler.y;
    _pitch = euler.x;
  }

  private void Update()
  {
    HandleLook();
    HandleMove();
  }

  private void HandleLook()
  {
    bool looking = holdRightMouseToLook ? Input.GetMouseButton(1) : true;
    if (!looking) return;

    float mouseX = Input.GetAxis("Mouse X");
    float mouseY = Input.GetAxis("Mouse Y");

    _yaw += mouseX * mouseSensitivity;
    _pitch -= mouseY * mouseSensitivity;
    _pitch = Mathf.Clamp(_pitch, -89.0f, 89.0f);

    transform.rotation = Quaternion.Euler(_pitch, _yaw, 0.0f);
  }

  private void HandleMove()
  {
    float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? boostMultiplier : 1.0f);
    float dt = Time.deltaTime;

    Vector3 move = Vector3.zero;
    if (Input.GetKey(KeyCode.W)) move += Vector3.forward;
    if (Input.GetKey(KeyCode.S)) move += Vector3.back;
    if (Input.GetKey(KeyCode.A)) move += Vector3.left;
    if (Input.GetKey(KeyCode.D)) move += Vector3.right;
    if (Input.GetKey(KeyCode.Space)) move += Vector3.up;
    if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C)) move += Vector3.down;
    if (Input.GetKey(KeyCode.Q)) move += Vector3.down;
    if (Input.GetKey(KeyCode.E)) move += Vector3.up;

    if (move.sqrMagnitude > 0.0f)
    {
      move = move.normalized * speed * dt;
      transform.Translate(move, Space.Self);
    }

    // Discrete left/right steps with J/K
    if (Input.GetKeyDown(KeyCode.J))
    {
      transform.Translate(Vector3.left * stepDistance, Space.Self);
    }
    if (Input.GetKeyDown(KeyCode.K))
    {
      transform.Translate(Vector3.right * stepDistance, Space.Self);
    }
  }
}


