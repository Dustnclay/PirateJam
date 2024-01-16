using System;
using UnityEngine;

public class LogansCharacterContoller : MonoBehaviour
{
    
    public Camera mainCamera;

    // Movement Variables
    [SerializeField] private float topSpeed = 12f;
    [SerializeField] private float sprintSpeed = 16f;
    [SerializeField] private int accelerationFrames = 22;
    [SerializeField] private float reverseMultiplier = 2;
   
    // Jump Variables
    [SerializeField] private float jumpHeight = 4f;
    [SerializeField] private float timeToJumpApex = 0.4f;
    [SerializeField] private float timeToJumpDescent = 0.2f;
    [SerializeField] private bool limitVerticalSpeedToJumpVelocity = true;
    private float _jumpVelocity;
    private float _jumpGravity;
    private float _fallGravity;
   
    // State Variables
    [SerializeField] private bool isGrounded = false;
   
    // Physics Variables
    [SerializeField] private float collisionTolerance = 0.002f;
   
    [SerializeField] private Vector2 _velocity = Vector2.zero;
   
   
    private Rigidbody2D rb;
    private BoxCollider2D boxCollider;

    private void InitializeComponents()
    {
        // Initialize the Rigidbody2D component for physics operations.
        rb = GetComponent<Rigidbody2D>();
        boxCollider = GetComponent<BoxCollider2D>();
        
        // Calculate the gravity and jump velocity based on the jump height and time to apex.
        _jumpVelocity = (2f * jumpHeight) / timeToJumpApex;
        _jumpGravity = (-2f * jumpHeight) / Mathf.Pow(timeToJumpApex, 2);
        _fallGravity = (-2f * jumpHeight) / Mathf.Pow(timeToJumpDescent, 2);
       
        // Debug the calculated values.
        Debug.Log($"Jump Velocity: {_jumpVelocity}");
        Debug.Log($"Jump Gravity: {_jumpGravity}");
        Debug.Log($"Fall Gravity: {_fallGravity}");
    }
    void Start()
    {
        InitializeComponents();
    }

    private void OnValidate()
    {
        InitializeComponents();
    }

    private void OnDrawGizmos()
    {
        if (!rb || !boxCollider)
        {
            Debug.LogError("Missing Rigidbody2D or BoxCollider2D component. Cannot draw Gizmos.");
            return;
        }
        Gizmos.color = Color.red;

       Vector2 pos = CollideAndSlide(_velocity, rb.position + boxCollider.offset * transform.lossyScale, boxCollider.size * transform.lossyScale,
            ~LayerMask.GetMask("Player"), true);
        Gizmos.DrawWireCube(pos, boxCollider.size * transform.lossyScale);

    }
   
    private float CalculateHypotenuseLength(Vector2 a, Vector2 b)
    {
        float angle = Vector2.SignedAngle(a, b);
        float hypotenuse_length = a.magnitude / Mathf.Cos(angle * Mathf.Deg2Rad);
        return hypotenuse_length;
    }
   
    private Vector2 NormalToRectEdgeDistance(Vector2 normal, Vector2 size)
    {
        // Scale the direction vector to fit within the rectangle's bounds
        float scaleX = size.x / 2 / Mathf.Abs(normal.x);
        float scaleY = size.y / 2 / Mathf.Abs(normal.y);

        // Scale factor is the minimum of scaleX and scaleY
        float scaleFactor = Mathf.Min(scaleX, scaleY);

        // Scaled direction vector
        Vector2 distance = normal * scaleFactor;
        return distance;
    }

    private Vector2 CollideAndSlide(Vector2 velocity, Vector2 position, Vector2 size, LayerMask layerMask, bool drawGizmos = false, int depth = 5)
    {
        // Return the final position of the object
        if (depth <= 0 || velocity == Vector2.zero)
        {
            return position;

        }
        // Shrink any side of the bounding box that is facing away from the direction of movement by the tolerance.
        // Single axis movement shrinks 3 sides, diagonal movement shrinks 2 sides
        Vector2 offset = Vector2.one * (collisionTolerance * 2);
        Vector2 colliderSize = size - offset;
        // Vector2 colliderOffset = Vector2.one * (collisionTolerance * 2);
        // if (Mathf.Abs(velocity.x) > collisionTolerance)
        // {
        //     colliderSize.x -= collisionTolerance;
        //     colliderOffset.x += collisionTolerance * Mathf.Sign(velocity.x) / 2;
        // }
        // else
        // {
        //     colliderSize.x -= collisionTolerance * 2;
        // }
        // if (Mathf.Abs(velocity.y) > collisionTolerance)
        // {
        //     colliderSize.y -= collisionTolerance;
        //     colliderOffset.y += collisionTolerance * Mathf.Sign(velocity.y) / 2;
        // }
        // else
        // {
        //     colliderSize.y -= collisionTolerance * 2;
        // }
        //
        // position += colliderOffset;
       
        // Cast the box to detect potential collisions in the predicted position
        RaycastHit2D hit = Physics2D.BoxCast(position, colliderSize, 0f, velocity.normalized, velocity.magnitude, layerMask);
       
       
        // // If a collision is detected
        if (hit.collider != null && !hit.collider.isTrigger)
        {
            // Draw the collision normal
            Vector2 normalRadius = NormalToRectEdgeDistance(hit.normal, colliderSize);
            Vector2 normalBorderRadius = NormalToRectEdgeDistance(hit.normal, offset);
            Vector2 velocityRadius = NormalToRectEdgeDistance(velocity.normalized, offset);
           
            normalBorderRadius = normalBorderRadius.normalized * (normalBorderRadius.magnitude + collisionTolerance);

            float walkbackDistance = CalculateHypotenuseLength(normalBorderRadius, -velocity);

            if (walkbackDistance < 0)
            {
                // We are already inside the collider, so we can't walk back
                Vector2 newPosition = position + hit.normal * collisionTolerance;
                return CollideAndSlide(velocity, newPosition, size, layerMask, drawGizmos, depth - 1);
            }
           
            float velocityToHitMagnitude = Mathf.Max(0, velocity.magnitude * hit.fraction - walkbackDistance);
            float leftoverVelocityMagnitude = velocity.magnitude - velocityToHitMagnitude;

            // Calculate the slide vector along the surface of the collision
            Vector2 slideDirection = Vector2.Perpendicular(hit.normal).normalized;
           
            // Scale reversing the moved velocity by creating a triangle between the hit normal and inverse of the velocity
            // The hypotenuse is the leftover velocity, and the adjacent side is the offsetReverse

            float slideDotProduct = Vector2.Dot(velocity.normalized, slideDirection.normalized);
            float slideMagnitude = leftoverVelocityMagnitude * slideDotProduct;
            Vector2 slideVector = slideDirection * slideMagnitude;
           
            Vector2 hitOrigin = position + velocity.normalized * velocityToHitMagnitude;
           
            // if (colliderOffset.y == colliderOffset.x)
            // {
            //     // Shrinking on an axis won't affect the hit origin, but if we shrink on both axes, we need to adjust the hit origin
            //     // remove offset on the same axis as the slideDirection
            //    
            //     hitOrigin -= slideDirection * colliderOffset;
            //     Debug.Log($"{slideDirection * colliderOffset}");
            // }
           
            if (drawGizmos)
            {
                // Draw the boxcast to hit
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(position + velocity.normalized * (velocity.magnitude * hit.fraction), colliderSize);
                // Draw velocity
                Gizmos.DrawRay(position, velocity.normalized * velocityToHitMagnitude);
                Gizmos.color = Color.magenta;
                Gizmos.DrawRay(hitOrigin, velocity.normalized * leftoverVelocityMagnitude);
                Gizmos.color = Color.green;
                // Draw offset overshoot
                Gizmos.color = Color.yellow;
                Gizmos.DrawRay(hit.point, -velocity.normalized * walkbackDistance);
                // Draw the boxcast hit
                Gizmos.color = Color.blue;
                Gizmos.DrawWireCube(hitOrigin, size);
                // Draw the boxcast hit normal
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(hit.point, slideDirection * slideMagnitude);
            }
            return CollideAndSlide(slideVector, hitOrigin, size, layerMask, depth: depth - 1);
        }

        return position + velocity;

    }
   
    private void CheckSurroundingCollisions()
    {
        // NOTE: This doesn't work with tilemaps. Since the tilemap collider is one large collider, it won't detect collisions with individual tiles.
        isGrounded = false;
        Vector2 colliderSize = boxCollider.size * transform.lossyScale + Vector2.one * (collisionTolerance * 2);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(rb.position, colliderSize, 0f, Vector2.zero, 0f, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                Debug.Log(hit.collider.name);
                Debug.DrawRay(hit.point, hit.normal, Color.yellow);
                if (Vector2.Dot(hit.normal, Vector2.up) > 0.5f)
                {
                    isGrounded = true;
                }
            }
        }
    }

    private void CheckGroundCollision()
    {
        isGrounded = false;
        Vector2 colliderSize = boxCollider.size * transform.lossyScale + new Vector2(-collisionTolerance * 4, 0);
        Vector2 colliderPosition = rb.position + boxCollider.offset * transform.lossyScale + new Vector2(0, -collisionTolerance * 2);
        RaycastHit2D[] hits = Physics2D.BoxCastAll(colliderPosition, colliderSize, 0f, Vector2.zero, 0f, ~LayerMask.GetMask("Player"));
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && !hit.collider.isTrigger)
            {
                Debug.DrawRay(hit.point, hit.normal, Color.yellow);
                if (Vector2.Dot(hit.normal, Vector2.up) > 0.5f)
                {
                    isGrounded = true;
                }
            }
        }
    }

    void Update()
    {
        // Camera follow
        if (mainCamera)
        {
            Vector3 cameraPos = mainCamera.transform.position;
            cameraPos.x = transform.position.x;
            mainCamera.transform.position = cameraPos;
        }
    }

    void FixedUpdate()
    {
        CheckGroundCollision();
       
        // Collect the current state of input keys.
        bool isMovingLeft = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool isMovingRight = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool isJumping = Input.GetKey(KeyCode.Space);

        // Calculate input direction (-1 for left, 1 for right, 0 for no input).
        int xInputDirection = isMovingRight.CompareTo(isMovingLeft);
       
        // Determine the desired velocity based on input and whether the sprint key is held.
        float targetSpeed = (isSprinting ? sprintSpeed : topSpeed) * xInputDirection;

        // Calculate velocity difference and its sign.
        float currentVelocityX = _velocity.x;
        float velocityDifferenceX = targetSpeed - currentVelocityX;
        int velocityDifferenceSignX = (int)Mathf.Sign(velocityDifferenceX);

        // Check if the player is reversing direction or has stopped input.
        bool isReversing = (velocityDifferenceSignX != (int)Mathf.Sign(currentVelocityX) && currentVelocityX != 0) || xInputDirection == 0;

        // Calculate the acceleration rate.
        float effectiveAcceleration = (topSpeed / accelerationFrames) * (isReversing ? reverseMultiplier : 1);
        float xAcceleration = velocityDifferenceSignX * Mathf.Min(effectiveAcceleration, Mathf.Abs(velocityDifferenceX));
       
        // Calculate the gravity. NOTE: We apply gravity overtime, so we need to multiply by the fixed delta time here.
        float gravity = 0f;
        if (!isGrounded)
        {
            gravity = (isJumping ? _jumpGravity : _fallGravity) * Time.fixedDeltaTime;
        }
        if(isJumping && isGrounded)
        {
            // If the player is jumping and on the ground, set the y velocity to the jump velocity.
            _velocity.y = _jumpVelocity;
        }

        // TODO: ADD ACCELERATION SIMPLE INTEGRATION
        Vector2 acceleration = new Vector2(xAcceleration, gravity);
        _velocity += acceleration;
       
        if (limitVerticalSpeedToJumpVelocity)
        {
            // Limit the vertical velocity to the jump velocity.
            _velocity.y = Mathf.Clamp(_velocity.y, -_jumpVelocity, _jumpVelocity);
        }
        Vector2 colliderOffset = boxCollider.offset * transform.lossyScale;
       
        Vector2 newPosition = CollideAndSlide(_velocity * Time.fixedDeltaTime,
            rb.position + colliderOffset,
            boxCollider.size * transform.lossyScale,
            ~LayerMask.GetMask("Player"));

        _velocity = (newPosition - colliderOffset - rb.position) / Time.fixedDeltaTime;


        rb.MovePosition(newPosition);
       
        // // Calculate predicted position based on current velocity and time
        // Vector2 predictedPosition = rb.position + _velocity * Time.fixedDeltaTime;
        //
        // // Calculate the size of the boxcast based on the player's collider size
        // Vector2 boxSize = boxCollider.size * transform.lossyScale * 0.99f; // Adjust size for the object's scale
        //
        // // Cast the box to detect potential collisions in the predicted position
        // LayerMask layerMask = ~LayerMask.GetMask("Player");
        // RaycastHit2D hit = Physics2D.BoxCast(rb.position, boxSize, 0f, _velocity.normalized, _velocity.magnitude * Time.fixedDeltaTime, layerMask);
        //
        // // Debug the boxcast
        // Debug.DrawRay(rb.position, _velocity.normalized * (_velocity.magnitude * Time.fixedDeltaTime), Color.red);
        //
        // Vector2 newPosition;
        //
        // // // If a collision is detected
        // if (hit.collider != null && !hit.collider.isTrigger)
        // {
        //     // Draw the collision normal
        //     Debug.DrawRay(hit.point, hit.normal, Color.blue);
        //     // Calculate the slide vector along the surface of the collision
        //     Vector2 slideDirection = Vector2.Perpendicular(hit.normal).normalized;
        //     float slideMagnitude = Vector2.Dot(_velocity.normalized * (_velocity.magnitude * Time.fixedDeltaTime), slideDirection);
        //     // Draw the slide vector
        //     Debug.DrawRay(hit.point, slideDirection * slideMagnitude, Color.cyan);
        //     // Apply the slide movement, only along the tangent of the collision
        //     _velocity = slideDirection * slideMagnitude / Time.fixedDeltaTime;
        //     newPosition = hit.point + hit.normal.normalized * (boxCollider.size.x / 2) + _velocity * Time.fixedDeltaTime;
        // }
        // else
        // {
        //     newPosition = rb.position + _velocity * Time.fixedDeltaTime;
        // }
        //
        // // Check if the player is on the ground.
        // bool grounded = newPosition.y <= groundLevel;
        // if (grounded)
        // {
        //     // If the player is on the ground, set the y velocity to 1 and set the y position to the ground level.
        //     _velocity.y = 0;
        //     newPosition.y = groundLevel;
        // }
    }
}
