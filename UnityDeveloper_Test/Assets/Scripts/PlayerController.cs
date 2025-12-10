using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 6f;
    public float jumpForce = 8f;
    public float rotateSpeed = 5f; 

    [Header("Gravity Settings")]
    public Transform hologramObject; 
    public float gravityMagnitude = 9.81f;
    
    // ---  HOLOGRAM SETTINGS ---
    [Header("Hologram Projection")]
    public float maxProjectionRange = 10f;
    public float defaultHoverDistance = 2f;
    public Vector3 visualOffset = Vector3.zero; 

    [Header("Hologram Movement")]
    public float hologramMoveSpeed = 8f;
    // Target position/rotation the hologram wants to reach
    private Vector3 holoTargetPos;
    private Quaternion holoTargetRot;
    private bool hologramInitialized = false;

    [Header("Ground Detection")]
    public Transform groundCheck;
    public float groundDistance = 0.2f;
    public LayerMask groundMask;

    [Header("Fail Conditions")]
    public float freeFallTimeToDie = 1.5f; // seconds of no contact before Game Over
    private float fallTimer = 0f;

    [Header("Game Session")]
    public int score = 0;

    [Header("Animation")]
    public Animator animator; 

    private Rigidbody rb;
    private bool isGrounded;
    private Vector3 moveInput;
    
    private Vector3 currentGravityDir = Vector3.down;
    private Vector3 proposedGravityDir = Vector3.down;
    private bool isSelectingGravity = false;

    // Cached reference to GameManager
    private GameManager gm;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false; 
        
        if (hologramObject != null) 
            hologramObject.gameObject.SetActive(false);

        gm = FindObjectOfType<GameManager>();
    }

    void Update()
    {
        //  Ground Check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);

        // Free-fall detection (for Game Over)
        if (!isGrounded)
        {
            fallTimer += Time.deltaTime;

            if (fallTimer >= freeFallTimeToDie && gm != null)
            {
                gm.PlayerFell();
            }
        }
        else
        {
            fallTimer = 0f; // reset when touching any surface again
        }

        //  WASD Movement
        float x = 0f; 
        float z = 0f;
        if (Input.GetKey(KeyCode.W)) z = +1f;
        if (Input.GetKey(KeyCode.S)) z = -1f;
        if (Input.GetKey(KeyCode.A)) x = -1f;
        if (Input.GetKey(KeyCode.D)) x = +1f;
        moveInput = new Vector3(x, 0f, z).normalized;

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded) 
            Jump();

        // Gravity Input
        HandleGravityInput();

        //  Update Hologram 
        if (isSelectingGravity) 
            SnapHologramToSurface();

        // Confirm Gravity
        if (isSelectingGravity && Input.GetKeyDown(KeyCode.Return)) 
            ApplyGravityChange();

        // Update Animator
        if (animator != null)
        {
            // We use moveInput.magnitude to know if we are pressing WASD
            animator.SetFloat("Speed", moveInput.magnitude);
        }
    }

    void FixedUpdate()
    {
        rb.AddForce(currentGravityDir * gravityMagnitude, ForceMode.Acceleration);
        Move();
        AlignToGravity();
    }

    void HandleGravityInput()
    {
        Vector3 direction = Vector3.zero;
        if (Input.GetKeyDown(KeyCode.UpArrow))    direction = transform.forward; 
        if (Input.GetKeyDown(KeyCode.DownArrow))  direction = -transform.forward; 
        if (Input.GetKeyDown(KeyCode.LeftArrow))  direction = -transform.right;   
        if (Input.GetKeyDown(KeyCode.RightArrow)) direction = transform.right;    

        if (direction != Vector3.zero)
        {
            proposedGravityDir = direction.normalized;
            isSelectingGravity = true;

            if (hologramObject != null)
            {
                hologramObject.gameObject.SetActive(true);

                ComputeHologramTarget();
                hologramInitialized = false; // first frame snaps to target cleanly
            }
        }
    }

    void ComputeHologramTarget()
    {
        if (hologramObject == null) return;

        RaycastHit hit;
        Vector3 rayOrigin = transform.position + (transform.up * 1.0f);
        
        if (Physics.Raycast(rayOrigin, proposedGravityDir, out hit, maxProjectionRange, groundMask))
        {
            holoTargetPos = hit.point;
            Vector3 targetUp = hit.normal;
            Vector3 targetForward = Vector3.ProjectOnPlane(transform.forward, hit.normal);
            if (targetForward.sqrMagnitude < 0.001f) 
                targetForward = transform.up;
            holoTargetRot = Quaternion.LookRotation(targetForward, targetUp);
        }
        else
        {
            holoTargetPos = transform.position + (proposedGravityDir * defaultHoverDistance);
            Quaternion gravityAlignment = Quaternion.FromToRotation(Vector3.up, -proposedGravityDir);
            holoTargetRot = gravityAlignment * transform.rotation;
        }

        // apply your visual offset in hologram local space
        holoTargetPos += holoTargetRot * visualOffset;
    }


    void SnapHologramToSurface()
    {
        if (hologramObject == null) return;

        if (!hologramInitialized)
        {
            hologramObject.position = holoTargetPos;
            hologramObject.rotation = holoTargetRot;
            hologramInitialized = true;
            return;
        }

        float t = hologramMoveSpeed * Time.deltaTime;

        hologramObject.position = Vector3.Lerp(
            hologramObject.position,
            holoTargetPos,
            t
        );

        hologramObject.rotation = Quaternion.Slerp(
            hologramObject.rotation,
            holoTargetRot,
            t
        );
    }

    void ApplyGravityChange()
    {
        currentGravityDir = proposedGravityDir;
        isSelectingGravity = false;
        hologramInitialized = false;

        if (hologramObject != null) 
            hologramObject.gameObject.SetActive(false);
    }

void Move()
{
    // Move relative to the player's local axes (forward/right), no extra rotation
    Vector3 moveDir = transform.TransformDirection(moveInput) * moveSpeed;

    // Keep existing gravity/fall velocity along currentGravityDir
    Vector3 verticalVel = Vector3.Project(rb.linearVelocity, currentGravityDir);

    // Apply lateral movement on the surface
    rb.linearVelocity = verticalVel + moveDir;
}
void AlignToGravity()
{
    Quaternion targetUp = Quaternion.FromToRotation(transform.up, -currentGravityDir) * transform.rotation;
    transform.rotation = Quaternion.Slerp(transform.rotation, targetUp, rotateSpeed * Time.fixedDeltaTime);
    
}
void Jump()
{
    rb.AddForce(-currentGravityDir * jumpForce, ForceMode.Impulse);
}

private void OnTriggerEnter(Collider other)
{
    if (other.CompareTag("Collectible"))
    {
        if (gm == null)
            gm = FindObjectOfType<GameManager>();

            if (gm != null) 
                gm.CubeCollected();

            score++;
            Destroy(other.gameObject);
        }
    }
}
