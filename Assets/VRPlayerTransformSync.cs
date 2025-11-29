using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Sincroniza la posición del jugador VR a través de la red
/// Coloca este script en el GameObject raíz del prefab VRPlayer
/// </summary>
public class VRPlayerTransformSync : NetworkBehaviour
{
    [Header("Sync Settings")]
    [Tooltip("Qué tan frecuentemente sincronizar (segundos)")]
    public float syncInterval = 0.05f; // 20 veces por segundo
    
    [Header("Head Tracking")]
    public bool syncHeadPosition = true;
    public bool syncHeadRotation = true;
    
    [Header("Hand Tracking")]
    public bool syncHandsPosition = true;
    public bool syncHandsRotation = true;
    
    // Network Variables para posición y rotación
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>();
    
    // Network Variables para cabeza
    private NetworkVariable<Vector3> networkHeadPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkHeadRotation = new NetworkVariable<Quaternion>();
    
    // Network Variables para manos
    private NetworkVariable<Vector3> networkLeftHandPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkLeftHandRotation = new NetworkVariable<Quaternion>();
    private NetworkVariable<Vector3> networkRightHandPosition = new NetworkVariable<Vector3>();
    private NetworkVariable<Quaternion> networkRightHandRotation = new NetworkVariable<Quaternion>();
    
    private float syncTimer = 0f;
    
    // Referencias
    private Transform xrOrigin;
    private Transform cameraTransform;
    private Transform leftHandTransform;
    private Transform rightHandTransform;

    void Awake()
    {
        FindXRReferences();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            Debug.Log("✅ [TransformSync] Jugador VR local - Enviando posiciones");
        }
        else
        {
            Debug.Log("✅ [TransformSync] Jugador VR remoto - Recibiendo posiciones");
        }
    }

    void Update()
    {
        if (!IsSpawned) return;
        
        if (IsOwner)
        {
            // Si somos el owner, enviar nuestra posición al servidor
            syncTimer += Time.deltaTime;
            if (syncTimer >= syncInterval)
            {
                SendTransformDataServerRpc(
                    transform.position,
                    transform.rotation,
                    cameraTransform != null ? cameraTransform.position : Vector3.zero,
                    cameraTransform != null ? cameraTransform.rotation : Quaternion.identity,
                    leftHandTransform != null ? leftHandTransform.position : Vector3.zero,
                    leftHandTransform != null ? leftHandTransform.rotation : Quaternion.identity,
                    rightHandTransform != null ? rightHandTransform.position : Vector3.zero,
                    rightHandTransform != null ? rightHandTransform.rotation : Quaternion.identity
                );
                syncTimer = 0f;
            }
        }
        else
        {
            // Si NO somos el owner, interpolar hacia la posición sincronizada
            transform.position = Vector3.Lerp(transform.position, networkPosition.Value, Time.deltaTime * 10f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation.Value, Time.deltaTime * 10f);
        }
    }

    [ServerRpc]
    private void SendTransformDataServerRpc(
        Vector3 position, Quaternion rotation,
        Vector3 headPos, Quaternion headRot,
        Vector3 leftHandPos, Quaternion leftHandRot,
        Vector3 rightHandPos, Quaternion rightHandRot)
    {
        // Actualizar variables de red (el servidor las propaga a todos los clientes)
        networkPosition.Value = position;
        networkRotation.Value = rotation;
        
        if (syncHeadPosition) networkHeadPosition.Value = headPos;
        if (syncHeadRotation) networkHeadRotation.Value = headRot;
        
        if (syncHandsPosition)
        {
            networkLeftHandPosition.Value = leftHandPos;
            networkRightHandPosition.Value = rightHandPos;
        }
        
        if (syncHandsRotation)
        {
            networkLeftHandRotation.Value = leftHandRot;
            networkRightHandRotation.Value = rightHandRot;
        }
    }

    private void FindXRReferences()
    {
        xrOrigin = transform.Find("XR Origin");
        
        if (xrOrigin != null)
        {
            Transform cameraOffset = xrOrigin.Find("Camera Offset");
            if (cameraOffset != null)
            {
                cameraTransform = cameraOffset.Find("Main Camera");
            }
            
            leftHandTransform = xrOrigin.Find("Left Hand");
            rightHandTransform = xrOrigin.Find("Right Hand");
        }
    }
    
    // Getters para que VRPlayerVisualSync pueda usar estos datos
    public Vector3 GetNetworkHeadPosition() => networkHeadPosition.Value;
    public Quaternion GetNetworkHeadRotation() => networkHeadRotation.Value;
    public Vector3 GetNetworkLeftHandPosition() => networkLeftHandPosition.Value;
    public Quaternion GetNetworkLeftHandRotation() => networkLeftHandRotation.Value;
    public Vector3 GetNetworkRightHandPosition() => networkRightHandPosition.Value;
    public Quaternion GetNetworkRightHandRotation() => networkRightHandRotation.Value;
}