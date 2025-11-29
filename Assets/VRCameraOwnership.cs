using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Coloca este script en el Main Camera DENTRO del prefab VRPlayer
/// Ruta: VRPlayer > XR Origin > Camera Offset > Main Camera
/// </summary>
public class VRCameraOwnership : NetworkBehaviour
{
    private Camera vrCamera;
    private AudioListener vrAudioListener;

    void Awake()
    {
        vrCamera = GetComponent<Camera>();
        vrAudioListener = GetComponent<AudioListener>();
        
        // Desactivar por defecto hasta saber si somos owner
        if (vrCamera != null) vrCamera.enabled = false;
        if (vrAudioListener != null) vrAudioListener.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Solo habilitar si SOMOS el dueño del jugador VR
        if (IsOwner)
        {
            Debug.Log("✅ [VR] Esta es MI cámara VR - Habilitando");
            if (vrCamera != null) vrCamera.enabled = true;
            if (vrAudioListener != null) vrAudioListener.enabled = true;
            
            // Asegurarse de que sea la cámara principal para VR
            if (vrCamera != null)
            {
                vrCamera.tag = "MainCamera";
                vrCamera.depth = 0; // Prioridad normal
            }
        }
        else
        {
            Debug.Log("❌ [VR] NO soy el owner - Cámara VR permanece desactivada");
            // Mantener desactivada
            if (vrCamera != null) vrCamera.enabled = false;
            if (vrAudioListener != null) vrAudioListener.enabled = false;
        }
    }
}