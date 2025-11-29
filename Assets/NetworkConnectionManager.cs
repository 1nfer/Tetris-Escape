using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections; // AGREGADO

public class NetworkConnectionManager : MonoBehaviour
{
    public static NetworkConnectionManager Instance { get; private set; }

    [Header("Network Settings")]
    public bool isVRPlayer = false; // FALSE para PC, TRUE para VR
    public string serverIPAddress = "192.168.1.7"; // IP de tu PC para el cliente VR
    public ushort serverPort = 7777;

    [Header("Game References")]
    public GameObject gameManagerPrefab;
    public GameObject gridManagerPrefab;

    public GameObject vrPlayerPrefab;

      public GameObject helpCanvas;
    
    private bool gameStarted = false;
    private int connectedPlayers = 0;   
    private UnityTransport transport;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
        // Obtener transport en Awake
        transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    void Start()
    {
        // Configurar cámaras según el tipo de jugador
        ConfigureCameras();
        
        // Configurar callbacks de conexión
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;

        // Iniciar según el tipo de jugador
        if (isVRPlayer)
        {
            StartClient();
            helpCanvas.SetActive(false);
        Debug.Log("❌ HelpCanvas desactivado para cliente VR");
            
        }
        else
        {
            StartServer();
        }
    }
    
    private void ConfigureCameras()
    {
        // Buscar cámara PC en la escena
        Camera[] allCameras = FindObjectsOfType<Camera>();
        
        foreach (Camera cam in allCameras)
        {
            // Si la cámara NO está dentro de un NetworkObject, es la cámara PC
            NetworkObject netObj = cam.GetComponentInParent<NetworkObject>();
            
            if (netObj == null)
            {
                // Esta es la cámara PC de la escena
                if (isVRPlayer)
                {
                    Debug.Log("❌ [Cámara] Soy VR - Desactivando cámara PC");
                    cam.enabled = false;
                    AudioListener listener = cam.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = false;
                }
                else
                {
                    Debug.Log("✅ [Cámara] Soy PC - Manteniendo cámara PC activa");
                    cam.enabled = true;
                    AudioListener listener = cam.GetComponent<AudioListener>();
                    if (listener != null) listener.enabled = true;
                }
            }
        }
    }

    void StartServer()
    {
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("🖥️ INICIANDO COMO SERVIDOR (PC Player)");
        Debug.Log("═══════════════════════════════════════════════");
        
        // CRÍTICO: Configurar transport ANTES de iniciar el servidor
        Debug.Log("🔧 Configurando UnityTransport...");
        Debug.Log($"   Address: 0.0.0.0 (escuchar en todas las interfaces)");
        Debug.Log($"   Port: {serverPort}");
        
        transport.SetConnectionData("0.0.0.0", serverPort);
        
        // Verificar configuración
        Debug.Log($"✅ Transport configurado:");
        Debug.Log($"   Address actual: {transport.ConnectionData.Address}");
        Debug.Log($"   Port actual: {transport.ConnectionData.Port}");
        
        // Iniciar como Host (servidor + cliente local)
        Debug.Log("🚀 Iniciando StartHost()...");
        bool started = NetworkManager.Singleton.StartHost();
        
        if (started)
        {
            connectedPlayers = 1; // El host cuenta como jugador
            Debug.Log("✅ StartHost() exitoso!");
            Debug.Log($"👤 Jugadores conectados: {connectedPlayers}/2");
            Debug.Log("");
            Debug.Log("┌───────────────────────────────────────────┐");
            Debug.Log("│   ESPERANDO CLIENTE VR...                 │");
            Debug.Log("├───────────────────────────────────────────┤");
            Debug.Log($"│ IP del Servidor: {GetLocalIPAddress(),-21}│");
            Debug.Log($"│ Puerto:          {serverPort,-21}│");
            Debug.Log("└───────────────────────────────────────────┘");
        }
        else
        {
            Debug.LogError("❌ StartHost() falló!");
        }
        
        Debug.Log("═══════════════════════════════════════════════");
    }

    void StartClient()
    {
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("🥽 INICIANDO COMO CLIENTE (VR Player)");
        Debug.Log("═══════════════════════════════════════════════");
        
        Debug.Log($"🎯 Servidor objetivo: {serverIPAddress}:{serverPort}");
        
        // Configurar la dirección IP del servidor
        transport.SetConnectionData(serverIPAddress, serverPort);
        
        Debug.Log($"✅ Transport configurado:");
        Debug.Log($"   Address: {transport.ConnectionData.Address}");
        Debug.Log($"   Port: {transport.ConnectionData.Port}");
        
        Debug.Log("🚀 Iniciando StartClient()...");
        bool started = NetworkManager.Singleton.StartClient();
        
        if (started)
        {
            Debug.Log($"⏳ Intentando conectar a {serverIPAddress}:{serverPort}...");
        }
        else
        {
            Debug.LogError("❌ StartClient() falló!");
        }
        
        Debug.Log("═══════════════════════════════════════════════");
    }
    
    private void OnServerStarted()
    {
        Debug.Log("");
        Debug.Log("🎉 ¡SERVIDOR COMPLETAMENTE INICIADO!");
        Debug.Log($"👂 Escuchando en puerto {serverPort}");
        Debug.Log("");
    }

    private void OnClientConnected(ulong clientId)
    {
    Debug.Log("");
    Debug.Log($"✅ Cliente conectado: {clientId}");
    
    connectedPlayers++;
    Debug.Log($"👥 Jugadores conectados: {connectedPlayers}/2");
    
    // Si somos el servidor y un cliente VR se conectó
    if (NetworkManager.Singleton.IsServer && clientId != NetworkManager.Singleton.LocalClientId)
    {
        Debug.Log($"🥽 Spawneando VRPlayer para cliente {clientId}");
        SpawnVRPlayerForClient(clientId);
        
        // NUEVO: Sincronizar estado del juego si ya está iniciado
        if (gameStarted)
        {
            Debug.Log($"🔄 Juego en progreso - sincronizando estado para cliente {clientId}");
            
            // Pequeño delay para asegurar que el cliente esté listo
            StartCoroutine(SyncStateWithDelay(clientId, 0.5f));
            
            // NUEVO: Si ahora hay 2 jugadores y el juego estaba pausado, reanudarlo
            if (connectedPlayers >= 2 && GameManager.gm != null)
            {
                Debug.Log("▶️ Ambos jugadores reconectados - reanudando juego...");
                GameManager.gm.UnpauseGame();

                HideReconnectionCanvasClientRpc();
            }
        }
    }
    
    
    // Si ahora hay 2 jugadores y el juego no ha comenzado, iniciarlo
    if (NetworkManager.Singleton.IsServer && connectedPlayers >= 2 && !gameStarted)
    {
        Debug.Log("");
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("🎮 ¡AMBOS JUGADORES CONECTADOS!");
        Debug.Log("🚀 INICIANDO JUEGO...");
        Debug.Log("═══════════════════════════════════════════════");
        Debug.Log("");
        StartGameServerRpc();
    }
}
[ClientRpc]
private void HideReconnectionCanvasClientRpc()
{
    if (reconnectionCanvas != null)
    {
        reconnectionCanvas.SetActive(false);
    }
}
    
    // NUEVO MÉTODO: Coroutine para sincronizar con delay
    private IEnumerator SyncStateWithDelay(ulong clientId, float delay)
    {
        yield return new WaitForSeconds(delay);
        SyncGameStateToClient(clientId);
    }
    
    // NUEVO MÉTODO: Sincronizar estado completo del juego a un cliente específico
    private void SyncGameStateToClient(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        Debug.Log($"🔄 Iniciando sincronización completa para cliente {clientId}");
        
        if (GameManager.gm == null)
        {
            Debug.LogWarning("⚠️ GameManager no disponible para sincronizar");
            return;
        }
        
        if (GridManager.gm == null)
        {
            Debug.LogWarning("⚠️ GridManager no disponible para sincronizar");
            return;
        }
        
        // Sincronizar el grid (cubos ya colocados)
        GridManager.gm.SyncGridToClient(clientId);
        
        // Enviar estado del juego solo a este cliente
        ClientRpcParams clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
        
        // Notificar al cliente que el juego ya está en progreso
        SyncGameStateClientRpc(clientRpcParams);
        
        Debug.Log($"✅ Sincronización completa enviada a cliente {clientId}");
    }
    
    // NUEVO ClientRpc: Notificar al cliente sobre la sincronización
    [ClientRpc]
    private void SyncGameStateClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("🔄 Recibiendo sincronización de estado del juego");
        
        if (GameManager.gm != null)
        {
            // El GameManager ya tiene NetworkVariables que se sincronizan automáticamente
            // Solo necesitamos activar la UI correcta
            Debug.Log("✅ Estado del juego sincronizado");
        }
    }
    
    private void SpawnVRPlayerForClient(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        
        if (vrPlayerPrefab == null)
        {
            Debug.LogError("❌ VRPlayer Prefab no está asignado en NetworkConnectionManager!");
            return;
        }
        
        // Posición de spawn (puedes ajustar esto)
        Vector3 spawnPosition = new Vector3(5, 1, 5);
        Quaternion spawnRotation = Quaternion.identity;
        
        // Instanciar el VRPlayer
        GameObject vrPlayerInstance = Instantiate(vrPlayerPrefab, spawnPosition, spawnRotation);
        
        // Obtener NetworkObject
        NetworkObject networkObject = vrPlayerInstance.GetComponent<NetworkObject>();
        
        if (networkObject != null)
        {
            // Spawnear como objeto de red y asignar al cliente específico
            networkObject.SpawnAsPlayerObject(clientId, true);
            Debug.Log($"✅ VRPlayer spawneado para cliente {clientId}");
        }
        else
        {
            Debug.LogError("❌ VRPlayer prefab no tiene NetworkObject component!");
            Destroy(vrPlayerInstance);
        }
    }

[Header("UI References")]
public GameObject reconnectionCanvas;

    private void OnClientDisconnected(ulong clientId)
{
    Debug.Log($"❌ Cliente desconectado: {clientId}");
    connectedPlayers--;
    Debug.Log($"👥 Jugadores restantes: {connectedPlayers}");
    
    if (NetworkManager.Singleton.IsServer && connectedPlayers < 2)
    {
        Debug.Log("⚠️ Jugador desconectado - pausando juego...");
        
        if (GameManager.gm != null && gameStarted)
        {
            GameManager.gm.PauseGame();
            NotifyPlayerDisconnectedClientRpc();
        }
    }
}
    private void NotifyPlayerDisconnectedClientRpc()
{
    Debug.Log("📢 Jugador desconectado - Juego pausado hasta que regrese");
     // Mostrar canvas de reconexión
    if (reconnectionCanvas != null)
    {
        reconnectionCanvas.SetActive(true);
    }
    // Opcional: Puedes agregar un mensaje en pantalla aquí
    // Por ejemplo, activar un canvas de "Esperando reconexión..."
}

    [ServerRpc(RequireOwnership = false)]
    private void StartGameServerRpc()
    {
        if (gameStarted) return;
        
        gameStarted = true;
        Debug.Log("🚀 Servidor iniciando el juego para todos los clientes...");
        
        // Notificar a todos los clientes que el juego ha comenzado
        StartGameClientRpc();
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        Debug.Log("🎯 Juego iniciado!");
        
        // Activar los managers del juego
        if (GameManager.gm != null)
        {
            Debug.Log("✅ GameManager encontrado, llamando StartGame()");
            GameManager.gm.StartGame();
        }
        else
        {
            Debug.LogWarning("⚠️ GameManager no encontrado!");
        }
        
        // Solo el servidor spawneará las piezas
        if (NetworkManager.Singleton.IsServer && GameManager.gm != null)
        {
            Debug.Log("🎲 Servidor spawneando primera pieza...");
            GameManager.gm.SpawnNextPiece();
        }
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    // Método para verificar si el juego está listo
    public bool IsGameReady()
    {
        return gameStarted && connectedPlayers >= 2;
    }

    // Método para obtener si es jugador VR
    public bool IsVRPlayer()
    {
        return isVRPlayer;
    }
    
    // Obtener IP local del servidor
    private string GetLocalIPAddress()
    {
        try
        {
            var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error obteniendo IP: {e.Message}");
        }
        return "No disponible";
    }
}