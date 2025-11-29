using UnityEngine;
using UnityEngine.XR.Hands;
using UnityEngine.XR.Management;
using Unity.Netcode;

[RequireComponent(typeof(CharacterController))]
public class HandFistMoveWithPhysics : NetworkBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 2.0f;
    public float rayDistance = 1.5f;
    public bool useRightHand = true;
    
    [Header("Obstacle Detection Settings")]
    public float maxAutoJumpHeight = 2f;
    public float detectionRadius = 0.4f;
    public LayerMask obstacleLayer = ~0;
    public LayerMask groundLayer = ~0;
    
    [Header("Advanced Detection")]
    public bool useMultipleRays = true;
    public int rayCount = 5;
    public float raySpread = 0.6f;

    [Header("Jump Settings")]
    public float jumpForce = 5.0f;
    public float autoJumpForce = 4.5f;
    public float jumpCooldown = 0.5f;
    public float gravity = 9.81f;
    
    [Header("Ground Check Settings")]
    public float groundCheckDistance = 0.3f;
    public float minGroundedTime = 0.1f;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool showDebugRays = true;
    
    private XRHandSubsystem handSubsystem;
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded = true;
    private float lastJumpTime = 0f;
    private float lastGroundedTime = 0f;
    private bool wasOpenHandUp = false;
    private bool canAutoJump = true;

    // NetworkVariables para sincronización
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );
    
    private NetworkVariable<Vector3> networkVelocity = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Owner
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        controller = GetComponent<CharacterController>();

        // Solo el owner detecta las manos
        if (IsOwner)
        {
            var xrManager = XRGeneralSettings.Instance?.Manager;
            if (xrManager != null)
            {
                foreach (var loader in xrManager.activeLoaders)
                {
                    handSubsystem = loader.GetLoadedSubsystem<XRHandSubsystem>();
                    if (handSubsystem != null)
                    {
                        Debug.Log("✅ XRHandSubsystem detectado.");
                        break;
                    }
                }
            }

            if (handSubsystem == null)
                Debug.LogWarning("⚠️ No se encontró XRHandSubsystem (puede que no estés en VR).");
        }
    }

    void Update()
    {
        // Solo el owner controla el movimiento
        if (!IsOwner)
        {
            // Los clientes remotos interpolan la posición
            if (controller != null)
            {
                Vector3 targetPosition = networkPosition.Value;
                Vector3 currentPosition = transform.position;
                
                // Interpolación suave
                if (Vector3.Distance(currentPosition, targetPosition) > 0.1f)
                {
                    Vector3 newPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * 10f);
                    controller.enabled = false;
                    transform.position = newPosition;
                    controller.enabled = true;
                }
            }
            return;
        }

        // LÓGICA DEL OWNER
        if (handSubsystem == null)
        {
            ApplyGravity();
            controller.Move(velocity * Time.deltaTime);
            UpdateNetworkVariables();
            return;
        }

        XRHand hand = useRightHand ? handSubsystem.rightHand : handSubsystem.leftHand;
        if (!hand.isTracked)
        {
            wasOpenHandUp = false;
            ApplyGravity();
            controller.Move(velocity * Time.deltaTime);
            UpdateNetworkVariables();
            return;
        }

        bool fistDetected = IsFist(hand);
        bool openHandUp = IsOpenHandPointingUp(hand);
        bool palmUpMenuGesture = IsPalmUpThumbRingGesture(hand);

        if (palmUpMenuGesture && isGrounded)
        {
            if (transform.position.y >= GameSettings.numberOfPlanes - 2)
            {
                RequestVictoryServerRpc();
            }
        }

        CheckGroundStatus();

        if (openHandUp && !wasOpenHandUp && isGrounded && Time.time - lastJumpTime > jumpCooldown)
        {
            Jump(jumpForce);
            lastJumpTime = Time.time;
        }
        wasOpenHandUp = openHandUp;

        if (fistDetected && !openHandUp)
        {
            TryMoveForward();
        }
        else
        {
            velocity.x = 0f;
            velocity.z = 0f;
            ApplyGravity();
        }

        controller.Move(velocity * Time.deltaTime);
        UpdateNetworkVariables();
    }

    void UpdateNetworkVariables()
    {
        if (IsOwner)
        {
            networkPosition.Value = transform.position;
            networkVelocity.Value = velocity;
        }
    }

    [ServerRpc]
    private void RequestVictoryServerRpc()
    {
        Debug.Log($"🏆 Jugador {OwnerClientId} alcanzó el objetivo!");
        
        // Notificar a todos los clientes
        NotifyVictoryClientRpc(OwnerClientId);
    }

    [ClientRpc]
    private void NotifyVictoryClientRpc(ulong winnerClientId)
    {
        Debug.Log($"🏆 Victoria del jugador {winnerClientId}!");
        
        if (GameManager.gm != null)
        {
            GameManager.gm.Victory();
        }
    }

    void CheckGroundStatus()
    {
        isGrounded = controller.isGrounded;
        
        if (!isGrounded)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit centerHit, groundCheckDistance + 0.2f, groundLayer))
            {
                if (IsStaticObject(centerHit.collider))
                {
                    isGrounded = true;
                }
            }
            
            if (!isGrounded)
            {
                float radius = controller.radius * 0.8f;
                Vector3[] offsets = new Vector3[]
                {
                    new Vector3(radius, 0, 0),
                    new Vector3(-radius, 0, 0),
                    new Vector3(0, 0, radius),
                    new Vector3(0, 0, -radius)
                };
                
                foreach (Vector3 offset in offsets)
                {
                    if (Physics.Raycast(rayOrigin + offset, Vector3.down, out RaycastHit hit, groundCheckDistance + 0.2f, groundLayer))
                    {
                        if (IsStaticObject(hit.collider))
                        {
                            isGrounded = true;
                            break;
                        }
                    }
                }
            }
        }

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            canAutoJump = true;
        }
        else
        {
            if (Time.time - lastGroundedTime > 0.5f)
            {
                canAutoJump = false;
            }
        }
    }

    bool IsStaticObject(Collider collider)
    {
        Rigidbody rb = collider.GetComponent<Rigidbody>();
        return rb == null || rb.isKinematic;
    }

    void Jump(float force)
    {
        velocity.y = force;
        isGrounded = false;
        canAutoJump = false;
        
        if (showDebugLogs)
            Debug.Log($"🦘 Salto activado! Fuerza: {force}");
        
        // Notificar el salto a través de la red
        if (IsOwner)
        {
            BroadcastJumpServerRpc(force);
        }
    }

    [ServerRpc]
    private void BroadcastJumpServerRpc(float force)
    {
        // Propagar el evento de salto a todos los clientes (excepto el owner)
        BroadcastJumpClientRpc(force);
    }

    [ClientRpc]
    private void BroadcastJumpClientRpc(float force)
    {
        // Los clientes remotos pueden reproducir efectos visuales/sonoros aquí
        if (!IsOwner && showDebugLogs)
        {
            Debug.Log($"🦘 Jugador remoto saltó con fuerza: {force}");
        }
    }

    void ApplyGravity()
    {
        if (!isGrounded)
        {
            velocity.y -= gravity * Time.deltaTime;
        }
        else
        {
            velocity.y = -0.5f;
        }
    }

    void TryMoveForward()
    {
        Transform cam = Camera.main.transform;
        Vector3 direction = cam.forward;
        direction.y = 0f;
        direction.Normalize();

        if (direction.magnitude < 0.1f)
        {
            velocity.x = 0f;
            velocity.z = 0f;
            ApplyGravity();
            return;
        }

        bool obstacleDetected = false;
        float obstacleHeight = 0f;
        float closestObstacleDistance = float.MaxValue;

        float playerBaseY = transform.position.y - (controller.height * 0.5f);

        if (useMultipleRays)
        {
            float[] heightOffsets = new float[] 
            { 
                controller.height * 0.25f,
                controller.height * 0.5f,
                controller.height * 0.75f
            };

            foreach (float heightOffset in heightOffsets)
            {
                Vector3 rayOrigin = transform.position + Vector3.up * (heightOffset - controller.height * 0.5f);
                Vector3 rightDir = Vector3.Cross(Vector3.up, direction).normalized;

                for (int i = 0; i < rayCount; i++)
                {
                    float t = (float)i / (rayCount - 1);
                    float lateralOffset = (t - 0.5f) * raySpread * 2f;
                    Vector3 offsetRayOrigin = rayOrigin + rightDir * lateralOffset;
                    
                    if (Physics.SphereCast(offsetRayOrigin, detectionRadius, direction, out RaycastHit hit, rayDistance, obstacleLayer))
                    {
                        ProcessObstacleHit(hit, direction, playerBaseY, ref obstacleDetected, ref obstacleHeight, ref closestObstacleDistance);
                    }

                    if (showDebugRays)
                    {
                        Debug.DrawRay(offsetRayOrigin, direction * rayDistance, obstacleDetected ? Color.red : Color.yellow, 0.1f);
                    }
                }
            }

            Vector3 boxCenter = transform.position + direction * (controller.radius + 0.1f);
            Vector3 halfExtents = new Vector3(controller.radius * 0.9f, controller.height * 0.4f, controller.radius * 0.5f);
            
            if (Physics.BoxCast(boxCenter, halfExtents, direction, out RaycastHit boxHit, Quaternion.LookRotation(direction), rayDistance * 0.8f, obstacleLayer))
            {
                ProcessObstacleHit(boxHit, direction, playerBaseY, ref obstacleDetected, ref obstacleHeight, ref closestObstacleDistance);
                
                if (showDebugRays)
                {
                    Debug.DrawLine(boxCenter, boxCenter + direction * rayDistance * 0.8f, Color.magenta, 0.1f);
                }
            }
        }
        else
        {
            Vector3 rayOrigin = transform.position;
            
            if (Physics.SphereCast(rayOrigin, detectionRadius, direction, out RaycastHit hit, rayDistance, obstacleLayer))
            {
                ProcessObstacleHit(hit, direction, playerBaseY, ref obstacleDetected, ref obstacleHeight, ref closestObstacleDistance);
            }
        }

        if (obstacleDetected && isGrounded && canAutoJump && Time.time - lastJumpTime > jumpCooldown)
        {
            float adjustedJumpForce = Mathf.Lerp(autoJumpForce * 0.8f, autoJumpForce * 1.3f, obstacleHeight / maxAutoJumpHeight);
            Jump(adjustedJumpForce);
            lastJumpTime = Time.time;
            
            if (showDebugLogs)
                Debug.Log($"🦘 Auto-salto! Altura obstáculo: {obstacleHeight:F2}m | Distancia: {closestObstacleDistance:F2}m | Fuerza: {adjustedJumpForce:F2}");
        }

        Vector3 horizontalMove = direction * moveSpeed;
        velocity.x = horizontalMove.x;
        velocity.z = horizontalMove.z;
        
        ApplyGravity();
    }

    void ProcessObstacleHit(RaycastHit hit, Vector3 moveDirection, float playerBaseY, ref bool obstacleDetected, ref float obstacleHeight, ref float closestDistance)
    {
        if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
            return;

        if (!IsStaticObject(hit.collider))
        {
            if (showDebugLogs)
                Debug.Log($"⚠️ Ignorando objeto dinámico: {hit.collider.name}");
            return;
        }

        if (hit.distance < closestDistance)
        {
            closestDistance = hit.distance;
        }

        Vector3 toObstacle = hit.point - transform.position;
        toObstacle.y = 0;
        float dotProduct = Vector3.Dot(toObstacle.normalized, moveDirection);

        if (dotProduct < 0.5f)
        {
            if (showDebugLogs)
                Debug.Log($"⏩ Ignorando objeto lateral: {hit.collider.name} (dot: {dotProduct:F2})");
            return;
        }

        Bounds obstacleBounds = hit.collider.bounds;
        float obstacleTop = obstacleBounds.max.y;
        float obstacleBottom = obstacleBounds.min.y;
        
        float heightDifference = obstacleTop - playerBaseY;
        
        float playerTop = playerBaseY + controller.height;
        bool isInVerticalPath = obstacleBottom < playerTop && obstacleTop > playerBaseY;

        if (showDebugLogs)
        {
            Debug.Log($"📦 Obstáculo: {hit.collider.name}\n" +
                      $"   PlayerBase: {playerBaseY:F2} | PlayerTop: {playerTop:F2}\n" +
                      $"   ObstacleBottom: {obstacleBottom:F2} | ObstacleTop: {obstacleTop:F2}\n" +
                      $"   Altura a saltar: {heightDifference:F2}m | Distancia: {hit.distance:F2}m\n" +
                      $"   Dot: {dotProduct:F2} | EnCaminoVertical: {isInVerticalPath}");
        }

        if (!isInVerticalPath)
        {
            if (showDebugLogs)
                Debug.Log($"⬆️ Obstáculo fuera del camino vertical");
            return;
        }

        if (heightDifference > 0.15f && heightDifference <= maxAutoJumpHeight && hit.distance < rayDistance * 0.7f)
        {
            obstacleDetected = true;
            obstacleHeight = Mathf.Max(obstacleHeight, heightDifference);
        }
        else if (heightDifference > maxAutoJumpHeight)
        {
            velocity.x = 0f;
            velocity.z = 0f;
            
            if (showDebugLogs)
                Debug.Log($"🛑 Obstáculo demasiado alto: {heightDifference:F2}m");
        }
    }

    bool IsFist(XRHand hand)
    {
        return IsFingerBent(hand.GetJoint(XRHandJointID.IndexTip), hand.GetJoint(XRHandJointID.IndexProximal)) &&
               IsFingerBent(hand.GetJoint(XRHandJointID.MiddleTip), hand.GetJoint(XRHandJointID.MiddleProximal)) &&
               IsFingerBent(hand.GetJoint(XRHandJointID.RingTip), hand.GetJoint(XRHandJointID.RingProximal)) &&
               IsFingerBent(hand.GetJoint(XRHandJointID.LittleTip), hand.GetJoint(XRHandJointID.LittleProximal)) &&
               IsFingerBent(hand.GetJoint(XRHandJointID.ThumbTip), hand.GetJoint(XRHandJointID.ThumbProximal), 0.07f);
    }

    bool IsOpenHandPointingUp(XRHand hand)
    {
        bool fingersExtended = 
            !IsFingerBent(hand.GetJoint(XRHandJointID.IndexTip), hand.GetJoint(XRHandJointID.IndexProximal), 0.08f) &&
            !IsFingerBent(hand.GetJoint(XRHandJointID.MiddleTip), hand.GetJoint(XRHandJointID.MiddleProximal), 0.08f) &&
            !IsFingerBent(hand.GetJoint(XRHandJointID.RingTip), hand.GetJoint(XRHandJointID.RingProximal), 0.08f) &&
            !IsFingerBent(hand.GetJoint(XRHandJointID.LittleTip), hand.GetJoint(XRHandJointID.LittleProximal), 0.08f);

        if (!fingersExtended)
            return false;

        XRHandJoint wrist = hand.GetJoint(XRHandJointID.Wrist);
        XRHandJoint middleProximal = hand.GetJoint(XRHandJointID.MiddleProximal);

        if (!wrist.TryGetPose(out Pose wristPose) || !middleProximal.TryGetPose(out Pose middlePose))
            return false;

        Vector3 handUpVector = (middlePose.position - wristPose.position).normalized;
        return Vector3.Dot(handUpVector, Vector3.up) > 0.7f;
    }

    bool IsPalmUpThumbRingGesture(XRHand hand)
    {
        XRHandJoint wrist = hand.GetJoint(XRHandJointID.Wrist);
        XRHandJoint middleProximal = hand.GetJoint(XRHandJointID.MiddleProximal);

        if (!wrist.TryGetPose(out Pose wristPose) || !middleProximal.TryGetPose(out Pose middlePose))
            return false;

        Vector3 handUpVector = (middlePose.position - wristPose.position).normalized;
        bool isPalmFacingUp = Vector3.Dot(handUpVector, Vector3.up) > 0.7f;

        if (!isPalmFacingUp)
            return false;

        XRHandJoint thumbTip = hand.GetJoint(XRHandJointID.ThumbTip);
        XRHandJoint ringTip = hand.GetJoint(XRHandJointID.RingTip);

        if (!thumbTip.TryGetPose(out Pose thumbPose) || !ringTip.TryGetPose(out Pose ringPose))
            return false;

        float thumbRingDistance = Vector3.Distance(thumbPose.position, ringPose.position);
        return thumbRingDistance < 0.03f;
    }

    bool IsFingerBent(XRHandJoint tip, XRHandJoint baseJoint, float threshold = 0.05f)
    {
        if (!tip.TryGetPose(out Pose tipPose) || !baseJoint.TryGetPose(out Pose basePose))
            return false;

        float distance = Vector3.Distance(tipPose.position, basePose.position);
        return distance < threshold;
    }

    void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        
        Transform cam = Camera.main.transform;
        Vector3 dir = cam.forward;
        dir.y = 0f;
        dir.Normalize();

        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null) return;

        float playerBaseY = transform.position.y - (cc.height * 0.5f);

        float[] heightOffsets = new float[] { cc.height * 0.25f, cc.height * 0.5f, cc.height * 0.75f };
        Color[] colors = new Color[] { Color.cyan, Color.yellow, Color.magenta };

        for (int h = 0; h < heightOffsets.Length; h++)
        {
            Vector3 rayOrigin = transform.position + Vector3.up * (heightOffsets[h] - cc.height * 0.5f);
            Gizmos.color = colors[h];

            if (useMultipleRays)
            {
                Vector3 rightDir = Vector3.Cross(Vector3.up, dir).normalized;
                
                for (int i = 0; i < rayCount; i++)
                {
                    float t = (float)i / (rayCount - 1);
                    float lateralOffset = (t - 0.5f) * raySpread * 2f;
                    Vector3 offsetRayOrigin = rayOrigin + rightDir * lateralOffset;
                    
                    Gizmos.DrawWireSphere(offsetRayOrigin, detectionRadius);
                    Gizmos.DrawLine(offsetRayOrigin, offsetRayOrigin + dir * rayDistance);
                }
            }
            else
            {
                Gizmos.DrawWireSphere(rayOrigin, detectionRadius);
                Gizmos.DrawLine(rayOrigin, rayOrigin + dir * rayDistance);
            }
        }

        Gizmos.color = Color.red;
        Vector3 boxCenter = transform.position + dir * (cc.radius + 0.1f);
        Vector3 boxSize = new Vector3(cc.radius * 1.8f, cc.height * 0.8f, cc.radius);
        Gizmos.DrawWireCube(boxCenter + dir * (rayDistance * 0.4f), boxSize);
        
        Gizmos.color = Color.green;
        Vector3 jumpVisualization = new Vector3(transform.position.x, playerBaseY + maxAutoJumpHeight * 0.5f, transform.position.z);
        Gizmos.DrawWireCube(jumpVisualization, new Vector3(0.5f, maxAutoJumpHeight, 0.5f));
        
        Gizmos.color = Color.blue;
        Vector3 groundCheckOrigin = transform.position + Vector3.up * 0.1f;
        Gizmos.DrawLine(groundCheckOrigin, groundCheckOrigin + Vector3.down * (groundCheckDistance + 0.2f));
        
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(new Vector3(transform.position.x, playerBaseY, transform.position.z), 0.1f);
    }
}