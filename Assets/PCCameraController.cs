using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Coloca este script en el Main Camera de PC (el que está en la escena TetrisLevel)
/// NO en el prefab VRPlayer
/// </summary>
public class PCCameraController : MonoBehaviour
{
    private Camera pcCamera;
    private AudioListener pcAudioListener;
    
    void Awake()
    {
        pcCamera = GetComponent<Camera>();
        pcAudioListener = GetComponent<AudioListener>();
    }

    void Start()
    {
        // Verificar si somos jugador PC o VR
        if (NetworkConnectionManager.Instance != null)
        {
            bool isVR = NetworkConnectionManager.Instance.IsVRPlayer();
            
            if (isVR)
            {
                // Si somos VR, desactivar la cámara PC
                Debug.Log("❌ [PC] Soy jugador VR - Desactivando cámara PC");
                if (pcCamera != null) pcCamera.enabled = false;
                if (pcAudioListener != null) pcAudioListener.enabled = false;
            }
            else
            {
                // Si somos PC, mantener cámara PC activa
                Debug.Log("✅ [PC] Soy jugador PC - Manteniendo cámara PC activa");
                if (pcCamera != null)
                {
                    pcCamera.enabled = true;
                    pcCamera.tag = "MainCamera";
                    pcCamera.depth = 0;
                }
                if (pcAudioListener != null)
                {
                    pcAudioListener.enabled = true;
                }
            }
        }
        else
        {
            // Por defecto, mantener activa (por si acaso)
            Debug.LogWarning("⚠️ [PC] NetworkConnectionManager no encontrado - Cámara PC permanece activa");
            if (pcCamera != null) pcCamera.enabled = true;
            if (pcAudioListener != null) pcAudioListener.enabled = true;
        }
    }

    void Update()
    {
        // Monitorear si hay múltiples AudioListeners (causa warning)
        if (pcAudioListener != null && pcAudioListener.enabled)
        {
            AudioListener[] allListeners = FindObjectsOfType<AudioListener>();
            if (allListeners.Length > 1)
            {
                // Solo mantener activo el nuestro si somos PC
                bool isVR = NetworkConnectionManager.Instance != null && 
                           NetworkConnectionManager.Instance.IsVRPlayer();
                
                if (isVR)
                {
                    pcAudioListener.enabled = false;
                }
            }
        }
    }
}