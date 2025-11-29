using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controla la visualización del jugador VR para otros jugadores
/// Se coloca en el GameObject raíz del prefab VRPlayer
/// </summary>
public class VRPlayerVisualSync : NetworkBehaviour
{
    [Header("Visual Settings")]
    [Tooltip("Color del avatar para este jugador")]
    public Color playerColor = new Color(0.2f, 0.5f, 1f, 0.7f); // Azul semi-transparente
    
    [Header("References (Auto-created if null)")]
    public GameObject bodyVisual;
    public GameObject headVisual;
    public GameObject leftHandVisual;
    public GameObject rightHandVisual;
    
    private Transform xrOrigin;
    private Transform cameraTransform;
    private Transform leftHandTransform;
    private Transform rightHandTransform;
    private VRPlayerTransformSync transformSync;

    void Awake()
    {
        // Buscar referencias del XR Origin
        FindXRReferences();
        transformSync = GetComponent<VRPlayerTransformSync>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (IsOwner)
        {
            // Si soy el dueño (jugador VR local), ocultar mi propia representación visual
            Debug.Log("✅ [VRVisual] Soy el jugador VR local - Ocultando visual propio");
            SetVisualsActive(false);
        }
        else
        {
            // Si NO soy el dueño, mostrar la representación visual del otro jugador
            Debug.Log("✅ [VRVisual] Jugador VR remoto - Mostrando visual");
            CreateVisuals();
            SetVisualsActive(true);
        }
    }

    void Update()
    {
        // Solo actualizar visuals si NO somos el owner
        if (!IsOwner && IsSpawned)
        {
            UpdateVisualPositions();
        }
    }

    private void FindXRReferences()
    {
        // Buscar XR Origin en los hijos
        xrOrigin = transform.Find("XR Origin");
        
        if (xrOrigin != null)
        {
            // Buscar cámara
            Transform cameraOffset = xrOrigin.Find("Camera Offset");
            if (cameraOffset != null)
            {
                cameraTransform = cameraOffset.Find("Main Camera");
            }
            
            // Buscar manos
            leftHandTransform = xrOrigin.Find("Left Hand");
            rightHandTransform = xrOrigin.Find("Right Hand");
            
            Debug.Log($"📍 XR References encontradas - Camera: {cameraTransform != null}, LeftHand: {leftHandTransform != null}, RightHand: {rightHandTransform != null}");
        }
        else
        {
            Debug.LogWarning("⚠️ No se encontró XR Origin en el prefab VRPlayer");
        }
    }

    private void CreateVisuals()
    {
        // Crear visual del cuerpo (cápsula)
        if (bodyVisual == null)
        {
            bodyVisual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyVisual.name = "Body Visual";
            bodyVisual.transform.SetParent(transform);
            bodyVisual.transform.localPosition = new Vector3(0, 1, 0); // Altura del cuerpo
            bodyVisual.transform.localRotation = Quaternion.identity;
            bodyVisual.transform.localScale = new Vector3(0.5f, 1f, 0.5f);
            
            // Remover collider (ya tenemos CharacterController)
            Destroy(bodyVisual.GetComponent<Collider>());
            
            // Aplicar color
            Renderer renderer = bodyVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = playerColor;
            }
        }
        
        // Crear visual de la cabeza (esfera)
        if (headVisual == null && cameraTransform != null)
        {
            headVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headVisual.name = "Head Visual";
            headVisual.transform.SetParent(transform);
            headVisual.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            
            Destroy(headVisual.GetComponent<Collider>());
            
            Renderer renderer = headVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = playerColor;
            }
        }
        
        // Crear visual de manos (cubos pequeños)
        if (leftHandVisual == null && leftHandTransform != null)
        {
            leftHandVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leftHandVisual.name = "Left Hand Visual";
            leftHandVisual.transform.SetParent(transform);
            leftHandVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            
            Destroy(leftHandVisual.GetComponent<Collider>());
            
            Renderer renderer = leftHandVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.green;
            }
        }
        
        if (rightHandVisual == null && rightHandTransform != null)
        {
            rightHandVisual = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rightHandVisual.name = "Right Hand Visual";
            rightHandVisual.transform.SetParent(transform);
            rightHandVisual.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            
            Destroy(rightHandVisual.GetComponent<Collider>());
            
            Renderer renderer = rightHandVisual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = Color.red;
            }
        }
        
        Debug.Log("✅ Visuals del jugador VR creados");
    }

    private void UpdateVisualPositions()
    {
        // Si tenemos transformSync, usar datos sincronizados, sino usar transforms locales
        if (transformSync != null)
        {
            // Actualizar manos con datos de red
            if (leftHandVisual != null)
            {
                Vector3 leftPos = transformSync.GetNetworkLeftHandPosition();
                Quaternion leftRot = transformSync.GetNetworkLeftHandRotation();
                
                if (leftPos != Vector3.zero)
                {
                    leftHandVisual.transform.position = leftPos;
                    leftHandVisual.transform.rotation = leftRot;
                }
            }
            
            if (rightHandVisual != null)
            {
                Vector3 rightPos = transformSync.GetNetworkRightHandPosition();
                Quaternion rightRot = transformSync.GetNetworkRightHandRotation();
                
                if (rightPos != Vector3.zero)
                {
                    rightHandVisual.transform.position = rightPos;
                    rightHandVisual.transform.rotation = rightRot;
                }
            }
        }
        else
        {
            // Fallback: usar transforms locales (solo funciona si está en la misma instancia)
            if (headVisual != null && cameraTransform != null)
            {
                headVisual.transform.position = cameraTransform.position;
                headVisual.transform.rotation = cameraTransform.rotation;
            }
            
            if (leftHandVisual != null && leftHandTransform != null)
            {
                leftHandVisual.transform.position = leftHandTransform.position;
                leftHandVisual.transform.rotation = leftHandTransform.rotation;
            }
            
            if (rightHandVisual != null && rightHandTransform != null)
            {
                rightHandVisual.transform.position = rightHandTransform.position;
                rightHandVisual.transform.rotation = rightHandTransform.rotation;
            }
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (bodyVisual != null) bodyVisual.SetActive(active);
        if (headVisual != null) headVisual.SetActive(active);
        if (leftHandVisual != null) leftHandVisual.SetActive(active);
        if (rightHandVisual != null) rightHandVisual.SetActive(active);
    }
}