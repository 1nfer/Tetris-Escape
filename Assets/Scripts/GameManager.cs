using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;
using Unity.Netcode;

public class GameManager : NetworkBehaviour
{
    private enum State {
        Waiting,
        Playing,
        Paused,
        GameOver,
        Victory,
    };

    public static GameManager gm;

    public GameObject[] tetrisPiecePrefabs;

    // The timeout after which a piece is moved down 1 unit automatically.
    public float startTimeout = 2.0f;
    public float minTimeout = 0.3f;
    private float timeout;
    private float playingTime = 0;

    private NetworkVariable<State> state = new NetworkVariable<State>(State.Waiting, 
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Setup in the editor.
    public GameObject gameCanvas;
    public GameObject pauseCanvas;
    public GameObject gameOverCanvas;
    public GameObject victoryCanvas;
    public GameObject victoryConditionCanvas;
    public GameObject mainCamera;
    public GameObject nextPieceText;
    public AudioClip gameOverAudioClip;
    public AudioClip victoryAudioClip;
  

    // Timer variables
    private float gameTimeLimit = 300f; // 5 minutos en segundos
    private NetworkVariable<float> currentGameTime = new NetworkVariable<float>(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public int timerFontSize = 30;
    public Color timerColor = Color.white;

    private NetworkVariable<int> score = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private int nextPieceIndex = -1;
    private GameObject nextPiece = null;

    private Text scoreText;
    private Text highScoreText;

    void Awake()
    {
        if (gm == null) {
            gm = this.gameObject.GetComponent<GameManager>();
        }
        
        gameCanvas.SetActive(false);
        victoryConditionCanvas.SetActive(true);
        gameOverCanvas.SetActive(false);
        pauseCanvas.SetActive(false);
        if (victoryCanvas != null) {
            victoryCanvas.SetActive(false);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        timeout = startTimeout;
        
        // Suscribirse a cambios de estado
        state.OnValueChanged += OnStateChanged;
        score.OnValueChanged += OnScoreChanged;
        
        Debug.Log($"GameManager spawned. IsServer: {IsServer}, IsClient: {IsClient}");
    }

    void Start()
    {
        // El juego no inicia automáticamente, espera a que NetworkConnectionManager lo active
        Debug.Log("GameManager: Esperando conexión de ambos jugadores...");
    }

    void Update()
    {
        // Solo el servidor actualiza la lógica del juego
        if (!IsServer) return;
        
        if (state.Value == State.Playing &&
            (Input.GetKeyDown(GameSettings.helpKey) || Input.GetKeyDown(GameSettings.pauseKey))) {
            PauseGame();
        } else if (state.Value == State.Paused &&
            (Input.GetKeyDown(GameSettings.helpKey) || Input.GetKeyDown(GameSettings.pauseKey))) {
            UnpauseGame();
        }

        // Update timer only when playing
        if (state.Value == State.Playing) {
            currentGameTime.Value += Time.deltaTime;

            // Check if time is up
            if (currentGameTime.Value >= gameTimeLimit) {
                GameOver();
            }
        }

        playingTime += Time.deltaTime;
        if (playingTime >= GameSettings.timeTimeoutTrigger) {
            UpdateTimeout();
            playingTime = 0;
        }
    }

    void OnGUI()
    {
        if (state.Value == State.Playing) {
            float timeRemaining = Mathf.Max(0, gameTimeLimit - currentGameTime.Value);
            int minutes = Mathf.FloorToInt(timeRemaining / 60f);
            int seconds = Mathf.FloorToInt(timeRemaining % 60f);
            
            // Configure el estilo del texto
            GUIStyle style = new GUIStyle();
            style.fontSize = timerFontSize;
            style.fontStyle = FontStyle.Bold;
            style.normal.textColor = timerColor;
            
            // Cambiar color cuando queda poco tiempo
            if (timeRemaining <= 30f) {
                style.normal.textColor = Color.red;
            } else if (timeRemaining <= 60f) {
                style.normal.textColor = Color.yellow;
            }
            
            // Dibujar el timer en la esquina superior derecha
            string timerString = string.Format("Tiempo: {0:00}:{1:00}", minutes, seconds);
            GUI.Label(new Rect(Screen.width - 200, 10, 190, 40), timerString, style);
        }
    }

    private void OnStateChanged(State oldState, State newState)
    {
        Debug.Log($"Estado cambió de {oldState} a {newState}");
        
        // Actualizar UI según el nuevo estado
        gameCanvas.SetActive(newState == State.Playing);
        pauseCanvas.SetActive(newState == State.Paused);
        gameOverCanvas.SetActive(newState == State.GameOver);
        
        if (victoryCanvas != null)
        {
            victoryCanvas.SetActive(newState == State.Victory);
        }
        
        UpdateScoreObjects();
        UpdateScoreText();
    }

    private void OnScoreChanged(int oldScore, int newScore)
    {
        UpdateScoreText();
    }

    private void UpdateTimeout()
    {
        float newTimeout = Mathf.Max(timeout - 0.1f, minTimeout);

        if (newTimeout != timeout) {
            Debug.Log("Timeout updated from " + timeout + " to " + newTimeout);
        }
        timeout = newTimeout;
    }

    public float GetTimeout()
    {
        return timeout;
    }

    public void PauseGame()
    {
        if (!IsServer) return;
        
        Assert.IsTrue(state.Value == State.Playing);
        state.Value = State.Paused;
    }

    public void UnpauseGame()
    {
        if (!IsServer) return;
        
        Assert.IsTrue(state.Value == State.Paused);
        state.Value = State.Playing;
    }

    public void PlayAgain()
    {
        // Solo el servidor puede reiniciar
        if (!IsServer)
        {
            Debug.LogWarning("⚠️ Solo el servidor puede reiniciar el juego");
            return;
        }
        
        Debug.Log("🔄 Reiniciando juego...");
        
        // Limpiar todas las piezas activas
        CleanupActivePieces();
        
        // Limpiar el grid
        if (GridManager.gm != null)
        {
            GridManager.gm.ClearGrid();
        }
        
        // Reiniciar variables del juego
        currentGameTime.Value = 0f;
        SetScore(0);
        timeout = startTimeout;
        playingTime = 0;
        
        // Destruir pieza de preview
        if (nextPiece != null)
        {
            Destroy(nextPiece);
            nextPiece = null;
        }
        nextPieceIndex = -1;
        
        // Cambiar estado a Playing
        state.Value = State.Playing;
        
        // Notificar a todos los clientes
        NotifyRestartClientRpc();
        
        // Spawnear primera pieza
        SpawnNextPiece();
        victoryConditionCanvas.SetActive(true);
        
        Debug.Log("✅ Juego reiniciado exitosamente");
    }

    public void OnVictoryButtonPressed()
    {
        if (IsClient)
        {
            Debug.Log("🎯 Botón de victoria presionado");
            RequestVictoryServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestVictoryServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("✅ ServerRpc recibido - Procesando victoria");
        victoryConditionCanvas.SetActive(true);
        Victory();
    }

    public void ShowVictoryConditionCanvas()
    {
        if (IsClient)
        {
            Debug.Log("🎯 Solicitando mostrar canvas");
            ShowVictoryConditionCanvasServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ShowVictoryConditionCanvasServerRpc()
    {
        Debug.Log("✅ Mostrando canvas en todos los clientes");
        ShowVictoryConditionCanvasClientRpc();
    }

    [ClientRpc]
    private void ShowVictoryConditionCanvasClientRpc()
    {
        if (victoryConditionCanvas != null)
        {
            victoryConditionCanvas.SetActive(true);
            Debug.Log("👁️ Canvas mostrado");
        }
    }

    public void HideVictoryConditionCanvas()
    {
        if (IsClient)
        {
            Debug.Log("🎯 Solicitando ocultar canvas");
            HideVictoryConditionCanvasServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void HideVictoryConditionCanvasServerRpc()
    {
        Debug.Log("✅ Ocultando canvas en todos los clientes");
        HideVictoryConditionCanvasClientRpc();
    }

    [ClientRpc]
    private void HideVictoryConditionCanvasClientRpc()
    {
        if (victoryConditionCanvas != null)
        {
            victoryConditionCanvas.SetActive(false);
            Debug.Log("🙈 Canvas ocultado");
        }
    }

    [ClientRpc]
    private void NotifyRestartClientRpc()
    {
        Debug.Log("🔄 Juego reiniciado por el servidor");
        
        // Aquí puedes agregar efectos visuales o sonidos si quieres
    }

    private void CleanupActivePieces()
{
    // Destruir todas las piezas activas
    TetrisPieceController[] pieces = FindObjectsOfType<TetrisPieceController>();
    foreach (var piece in pieces)
    {
        NetworkObject netObj = piece.GetComponent<NetworkObject>();
        if (netObj != null && netObj.IsSpawned)
        {
            netObj.Despawn(true);
        }
        else
        {
            Destroy(piece.gameObject);
        }
    }
}
    public void MainMenu()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MainMenu");
    }

    public void ResetHighScore()
    {
        PlayerPrefs.SetInt("HighScore", 0);
        UpdateScoreText();
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void GameOver()
    {
        if (!IsServer) return;
        
        nextPieceIndex = -1;
        if (nextPiece != null) {
            Destroy(nextPiece);
            nextPiece = null;
        }
        state.Value = State.GameOver;
        
        PlaySoundClientRpc(true); // true = game over sound
    }

    public void Victory()
    {
        if (!IsServer) return;
        
        nextPieceIndex = -1;
        if (nextPiece != null) {
            Destroy(nextPiece);
            nextPiece = null;
        }
        state.Value = State.Victory;
        
        PlaySoundClientRpc(false); // false = victory sound
    }

    [ClientRpc]
    private void PlaySoundClientRpc(bool isGameOver)
    {
        if (isGameOver)
        {
            AudioSource.PlayClipAtPoint(gameOverAudioClip, transform.position);
        }
        else if (victoryAudioClip != null)
        {
            AudioSource.PlayClipAtPoint(victoryAudioClip, transform.position);
        }
    }

    public bool IsGameActive()
    {
        return state.Value == State.Playing;
    }

    public void StartGame()
    {
        if (!IsServer) return;
        
        state.Value = State.Playing;
        currentGameTime.Value = 0f;
        UpdateScoreObjects();
        SetScore(0);
        
        Debug.Log("✅ Juego iniciado por el servidor");
    }

    public void SpawnNextPiece()
    {
        // Solo el servidor puede spawnear piezas
        if (!IsServer) return;
        
        Vector3 nextPiecePos;
        int newNextIndex = Random.Range(0, tetrisPiecePrefabs.Length);

        if (nextPieceIndex == -1) {
            nextPieceIndex = Random.Range(0, tetrisPiecePrefabs.Length);
        }

        if (nextPiece != null) {
            nextPiecePos = nextPiece.transform.position;
            Destroy(nextPiece);
        } else {
            nextPiecePos = nextPieceText.transform.position;
        }

        // Crear preview de siguiente pieza (local, no sincronizado)
        nextPiece = Instantiate(tetrisPiecePrefabs[newNextIndex],
                                transform.position,
                                transform.rotation) as GameObject;
        TetrisPieceController pieceController = nextPiece.GetComponent<TetrisPieceController>();
        pieceController.SetUnplayable();

        nextPiece.transform.position = nextPiecePos;
        nextPiece.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        nextPiece.transform.SetParent(mainCamera.transform);

        // Spawnear la pieza jugable en la red
        GameObject piece = Instantiate(tetrisPiecePrefabs[nextPieceIndex],
                                       transform.position,
                                       transform.rotation) as GameObject;
        
        // Spawnear en la red
        NetworkObject networkObject = piece.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn(true);
        }

        nextPieceIndex = newNextIndex;
    }

    public void IncrementScore(int amount)
    {
        if (!IsServer) return;
        SetScore(score.Value + amount);
    }

    private void SetScore(int newScore)
    {
        if (!IsServer) return;
        
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        score.Value = newScore;
        if (newScore > highScore) {
            PlayerPrefs.SetInt("HighScore", newScore);
        }
        
        if (newScore > 0 && ((newScore % GameSettings.scoreTimeoutTrigger) == 0)) {
            UpdateTimeout();
        }
    }

    private void UpdateScoreText()
    {
        if (scoreText == null || highScoreText == null) return;
        
        int highScore = PlayerPrefs.GetInt("HighScore", 0);

        scoreText.text = "Score: " + score.Value.ToString();
        highScoreText.text = "High Score: " + highScore.ToString();
    }

    private void UpdateScoreObjects()
    {
        GameObject canvas = null;
        Transform t;

        switch (state.Value) {
        case State.Playing:
            canvas = gameCanvas;
            break;
        case State.GameOver:
            canvas = gameOverCanvas;
            break;
        case State.Paused:
            canvas = pauseCanvas;
            break;
        case State.Victory:
            if (victoryCanvas != null) {
                canvas = victoryCanvas;
            }
            break;
        }

        if (canvas != null) {
            t = canvas.transform.Find("ScoreText");
            if (t != null) {
                scoreText = t.gameObject.GetComponent<Text>();
            }
            t = canvas.transform.Find("HighScoreText");
            if (t != null) {
                highScoreText = t.gameObject.GetComponent<Text>();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        state.OnValueChanged -= OnStateChanged;
        score.OnValueChanged -= OnScoreChanged;
        base.OnNetworkDespawn();
    }
}