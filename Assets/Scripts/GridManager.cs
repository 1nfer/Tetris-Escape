using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Netcode;

public class GridManager : NetworkBehaviour
{
    public static GridManager gm;

    public GameObject cubeExplosionPrefab;
    public AudioClip explosionSFX;
    public GameObject frozenCubePrefab;
    public LayerMask frozenCubeLayer;

    private GameObject[,,] grid;
    private int[] planeCubes;

    void Awake()
    {
        grid = new GameObject[GameSettings.numberOfPlanes,
                            GameSettings.rowsPerPlane,
                            GameSettings.columnsPerPlane];

        for (int i = 0; i < grid.GetLength(0); i++) {
            for (int j = 0; j < grid.GetLength(1); j++) {
                for (int k = 0; k < grid.GetLength(2); k++) {
                    grid[i, j, k] = null;
                }
            }
        }

        planeCubes = new int[GameSettings.numberOfPlanes];
        for (int i = 0; i < planeCubes.Length; i++) {
            planeCubes[i] = 0;
        }

        if (gm == null) {
            gm = this.gameObject.GetComponent<GridManager>();
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"GridManager spawned. IsServer: {IsServer}");
    }

    public void SyncGridToClient(ulong clientId)
    {
        if (!IsServer) return;
        
        List<CubeInfo> allCubes = new List<CubeInfo>();
        
        for (int plane = 0; plane < grid.GetLength(0); plane++)
        {
            for (int row = 0; row < grid.GetLength(1); row++)
            {
                for (int col = 0; col < grid.GetLength(2); col++)
                {
                    GameObject cube = grid[plane, row, col];
                    
                    if (cube != null)
                    {
                        CubeInfo info = new CubeInfo();
                        info.plane = plane;
                        info.row = row;
                        info.col = col;
                        info.position = cube.transform.position;
                        info.rotation = cube.transform.rotation;
                        info.scale = cube.transform.localScale;
                        
                        Renderer renderer = cube.GetComponent<Renderer>();
                        if (renderer != null && renderer.material != null)
                        {
                            info.materialColor = renderer.material.color;
                            info.materialName = renderer.material.name;
                        }
                        else
                        {
                            info.materialColor = Color.white;
                            info.materialName = "";
                        }
                        
                        BoxCollider boxCollider = cube.GetComponent<BoxCollider>();
                        if (boxCollider != null)
                        {
                            info.colliderCenter = boxCollider.center;
                            info.colliderSize = boxCollider.size;
                            info.hasCollider = true;
                        }
                        else
                        {
                            info.colliderCenter = Vector3.zero;
                            info.colliderSize = Vector3.one;
                            info.hasCollider = false;
                        }
                        
                        allCubes.Add(info);
                    }
                }
            }
        }
        
        if (allCubes.Count > 0)
        {
            Debug.Log($"🔄 Sincronizando {allCubes.Count} cubos al cliente {clientId}");
            
            ClientRpcParams clientRpcParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] { clientId }
                }
            };
            
            SyncGridClientRpc(allCubes.ToArray(), clientRpcParams);
        }
        else
        {
            Debug.Log("✅ Grid vacío, no hay nada que sincronizar");
        }
    }

    [ClientRpc]
    private void SyncGridClientRpc(CubeInfo[] cubes, ClientRpcParams clientRpcParams = default)
    {
        Debug.Log($"📥 Recibiendo sincronización de {cubes.Length} cubos");
        
        GameObject[] existingFrozenCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
        foreach (var cube in existingFrozenCubes)
        {
            Destroy(cube);
        }
        
        foreach (var info in cubes)
        {
            CreateFrozenCube(info);
        }
        
        Debug.Log("✅ Grid sincronizado correctamente");
    }

    public bool MaybeFreezePiece(GameObject piece)
    {
        if (!IsServer) return false;
        
        bool freeze = false;

        foreach (Transform cube in piece.transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            if (plane == 0) {
                freeze = true;
                break;
            }

            if (grid[plane - 1, row, col] != null) {
                freeze = true;
                break;
            }
        }

        if (freeze) {
            FreezePiece(piece);
        }

        return freeze;
    }

    private void FreezePiece(GameObject piece)
    {
        if (!IsServer) return;
        
        NetworkObject networkObject = piece.GetComponent<NetworkObject>();
        List<CubeInfo> cubeInfos = new List<CubeInfo>();
        
        foreach (Transform cube in piece.transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            CubeInfo info = new CubeInfo();
            info.plane = plane;
            info.row = row;
            info.col = col;
            info.position = cube.position;
            info.rotation = cube.rotation;
            info.scale = cube.localScale;
            
            Renderer renderer = cube.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null) {
                info.materialColor = renderer.material.color;
                info.materialName = renderer.material.name;
            } else {
                info.materialColor = Color.white;
                info.materialName = "";
            }
            
            BoxCollider boxCollider = cube.GetComponent<BoxCollider>();
            if (boxCollider != null) {
                info.colliderCenter = boxCollider.center;
                info.colliderSize = boxCollider.size;
                info.hasCollider = true;
            } else {
                info.colliderCenter = Vector3.zero;
                info.colliderSize = Vector3.one;
                info.hasCollider = false;
            }

            cubeInfos.Add(info);
        }
        
        foreach (var info in cubeInfos)
        {
            GameObject frozenCube = CreateFrozenCube(info);
            FreezeCube(frozenCube.transform, info.plane, info.row, info.col);
        }
        
        SpawnFrozenCubesClientRpc(cubeInfos.ToArray());

        piece.transform.DetachChildren();
        
        foreach (var info in cubeInfos)
        {
            GameObject[] allObjects = FindObjectsOfType<GameObject>();
            foreach (GameObject obj in allObjects)
            {
                if (Vector3.Distance(obj.transform.position, info.position) < 0.01f && 
                    obj.transform.parent == null)
                {
                    Destroy(obj);
                    break;
                }
            }
        }
        
        if (networkObject != null && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
        }
        else
        {
            Destroy(piece);
        }
        
        ProcessFullPlanes();
    }
    
    // ⚠️ MÉTODO CRÍTICO CORREGIDO - CONFIGURACIÓN CORRECTA DE COLLIDERS
    private GameObject CreateFrozenCube(CubeInfo info)
    {
        GameObject frozenCube;
        
        if (frozenCubePrefab != null)
        {
            frozenCube = Instantiate(frozenCubePrefab, info.position, info.rotation);
        }
        else
        {
            frozenCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            frozenCube.transform.position = info.position;
            frozenCube.transform.rotation = info.rotation;
        }
        
        // Aplicar escala
        frozenCube.transform.localScale = info.scale;
        
        // ✅ CRÍTICO: ELIMINAR RIGIDBODY completamente
        Rigidbody rb = frozenCube.GetComponent<Rigidbody>();
        if (rb != null)
        {
            DestroyImmediate(rb);
        }
        
        // Aplicar material y color
        Renderer renderer = frozenCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = info.materialColor;
        }
        
        // ✅ CONFIGURACIÓN CORRECTA DEL COLLIDER
        BoxCollider collider = frozenCube.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = frozenCube.AddComponent<BoxCollider>();
        }
        
        if (collider != null)
        {
            // IMPORTANTE: NO expandir el collider - usar tamaño exacto
            collider.center = info.colliderCenter;
            collider.size = info.colliderSize;
            
            // ✅ CRÍTICO: NO es trigger - debe ser sólido
            collider.isTrigger = false;
            
            // ✅ ASEGURAR que "Provides Contacts" esté habilitado (crítico para detección)
            collider.providesContacts = true;
            
            Debug.Log($"✅ Cubo congelado creado con collider: Center={collider.center}, Size={collider.size}");
        }
        
        // ✅ CONFIGURACIÓN DE TAG Y LAYER
        frozenCube.tag = "FrozenCube";
        
        // ✅ CRÍTICO: Asignar al layer correcto para detección
        // Usa el mismo layer que obstacleLayer en HandFistMoveWithPhysics
        frozenCube.layer = LayerMask.NameToLayer("Default");
        
        // ✅ IMPORTANTE: Marcar como estático para optimización de física
        frozenCube.isStatic = true;
        
        return frozenCube;
    }
    
    [ClientRpc]
    private void SpawnFrozenCubesClientRpc(CubeInfo[] cubeInfos)
    {
        foreach (var info in cubeInfos)
        {
            CreateFrozenCube(info);
        }
    }
    
    [System.Serializable]
    private struct CubeInfo : INetworkSerializable
    {
        public int plane;
        public int row;
        public int col;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public Color materialColor;
        public string materialName;
        public Vector3 colliderCenter;
        public Vector3 colliderSize;
        public bool hasCollider;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref plane);
            serializer.SerializeValue(ref row);
            serializer.SerializeValue(ref col);
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref rotation);
            serializer.SerializeValue(ref scale);
            serializer.SerializeValue(ref materialColor);
            serializer.SerializeValue(ref colliderCenter);
            serializer.SerializeValue(ref colliderSize);
            serializer.SerializeValue(ref hasCollider);
        }
    }

    public bool GridCellOccupied(int plane, int row, int col)
    {
        return grid[plane, row, col] != null;
    }

    private void FreezeCube(Transform cube, int plane, int row, int col)
    {
        grid[plane, row, col] = cube.gameObject;
        planeCubes[plane]++;
        Assert.IsTrue(planeCubes[plane] <= GameSettings.cubesPerPlane);
    }

    private void ProcessFullPlanes()
    {
        if (!IsServer) return;
        
        int scoreMultiplier = 0;
        int planes = 0;
        int score;

        for (int i = 0; i < planeCubes.Length; i++) {
            if (planeCubes[i] == GameSettings.cubesPerPlane) {
                scoreMultiplier++;
                planes++;
            }
        }

        if (planes == 0) {
            return;
        }

        score = GameSettings.scorePerPlane * planes * scoreMultiplier;

        while (planes > 0) {
            int i = -1;

            for (i = planeCubes.Length - 1; i >= 0; i--) {
                if (planeCubes[i] == GameSettings.cubesPerPlane) {
                    DestroyPlane(i);
                    break;
                }
            }

            Assert.IsTrue(i >= 0);
            planes--;
        }

        GameManager.gm.IncrementScore(score);
    }

    private void DestroyPlane(int plane)
    {
        if (!IsServer) return;
        
        List<Vector3> explosionPositions = new List<Vector3>();
        List<Quaternion> explosionRotations = new List<Quaternion>();
        
        for (int i = 0; i < grid.GetLength(1); i++) {
            for (int j = 0; j < grid.GetLength(2); j++) {
                GameObject cube = grid[plane, i, j]; 

                Assert.IsNotNull(cube);
                
                explosionPositions.Add(cube.transform.position);
                explosionRotations.Add(cube.transform.rotation);
                
                Destroy(cube);
                grid[plane, i, j] = null;
            }
        }
        
        DestroyPlaneClientRpc(plane, explosionPositions.ToArray(), explosionRotations.ToArray());
        
        planeCubes[plane] = 0;

        List<CubeMoveInfo> cubesToMove = new List<CubeMoveInfo>();
    
        for (int i = plane + 1; i < grid.GetLength(0); i++) {
            for (int j = 0; j < grid.GetLength(1); j++) {
                for (int k = 0; k < grid.GetLength(2); k++) {
                    GameObject cube = grid[i, j, k];

                    if (cube != null) {
                        Vector3 oldPosition = cube.transform.position;
                        
                        grid[i - 1, j, k] = cube;
                        grid[i, j, k] = null;
                        cube.transform.Translate(0, -1, 0, Space.World);
                        
                        cubesToMove.Add(new CubeMoveInfo {
                            oldPosition = oldPosition,
                            newPosition = cube.transform.position
                        });
                    }
                }
            }
            planeCubes[i - 1] = planeCubes[i];
            planeCubes[i] = 0;
        }
        
        if (cubesToMove.Count > 0)
        {
            MoveCubesClientRpc(cubesToMove.ToArray());
        }
    }

    [ClientRpc]
    private void DestroyPlaneClientRpc(int plane, Vector3[] explosionPositions, Quaternion[] explosionRotations)
    {
        GameObject[] frozenCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
        
        foreach (var position in explosionPositions)
        {
            foreach (var frozenCube in frozenCubes)
            {
                if (frozenCube != null && Vector3.Distance(frozenCube.transform.position, position) < 0.1f)
                {
                    Destroy(frozenCube);
                    break;
                }
            }
        }
        
        for (int i = 0; i < explosionPositions.Length; i++)
        {
            if (cubeExplosionPrefab != null)
            {
                Instantiate(cubeExplosionPrefab, explosionPositions[i], explosionRotations[i]);
            }
        }
        
        if (explosionSFX != null && explosionPositions.Length > 0)
        {
            AudioSource.PlayClipAtPoint(explosionSFX, explosionPositions[0]);
        }
    }

    [ClientRpc]
    private void MoveCubesClientRpc(CubeMoveInfo[] moves)
    {
        GameObject[] frozenCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
        
        foreach (var move in moves)
        {
            foreach (var frozenCube in frozenCubes)
            {
                if (frozenCube != null && Vector3.Distance(frozenCube.transform.position, move.oldPosition) < 0.1f)
                {
                    frozenCube.transform.position = move.newPosition;
                    break;
                }
            }
        }
    }

    [System.Serializable]
    private struct CubeMoveInfo : INetworkSerializable
    {
        public Vector3 oldPosition;
        public Vector3 newPosition;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref oldPosition);
            serializer.SerializeValue(ref newPosition);
        }
    }

    public void ClearGrid()
    {
        if (!IsServer) return;
        
        Debug.Log("🧹 Limpiando grid...");
        
        int gridCubesDestroyed = 0;
        for (int plane = 0; plane < grid.GetLength(0); plane++)
        {
            for (int row = 0; row < grid.GetLength(1); row++)
            {
                for (int col = 0; col < grid.GetLength(2); col++)
                {
                    if (grid[plane, row, col] != null)
                    {
                        Destroy(grid[plane, row, col]);
                        grid[plane, row, col] = null;
                        gridCubesDestroyed++;
                    }
                }
            }
        }
        Debug.Log($"✅ Destruidos {gridCubesDestroyed} cubos del array grid");
        
        for (int pass = 0; pass < 3; pass++)
        {
            GameObject[] frozenCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
            Debug.Log($"🧹 Pasada {pass + 1}: Encontrados {frozenCubes.Length} cubos con tag FrozenCube");
            
            foreach (var cube in frozenCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                }
            }
            
            if (frozenCubes.Length == 0) break;
        }
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int additionalDestroyed = 0;
        
        List<GameObject> toDestroy = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null) continue;
            if (obj.transform.parent != null) continue;
            
            if (obj.GetComponent<NetworkObject>() != null ||
                obj.GetComponent<TetrisPieceController>() != null ||
                obj.GetComponent<Camera>() != null ||
                obj.GetComponent<Light>() != null ||
                obj.GetComponent<Canvas>() != null ||
                obj.GetComponent<NetworkManager>() != null ||
                obj.GetComponent<AudioListener>() != null ||
                obj.GetComponent<GameManager>() != null ||
                obj.GetComponent<GridManager>() != null)
            {
                continue;
            }
            
            string objNameLower = obj.name.ToLower();
            if (objNameLower.Contains("grid") ||
                objNameLower.Contains("floor") ||
                objNameLower.Contains("wall") ||
                objNameLower.Contains("plane") && !objNameLower.Contains("cube") ||
                objNameLower.Contains("background") ||
                objNameLower.Contains("environment") ||
                objNameLower.Contains("manager") ||
                objNameLower.Contains("camera") ||
                objNameLower.Contains("light") && !objNameLower.Contains("cube") ||
                objNameLower.Contains("canvas") ||
                objNameLower.Contains("eventsystem") ||
                objNameLower.Contains("network") ||
                objNameLower.Contains("xr") ||
                objNameLower.Contains("vr") ||
                objNameLower.Contains("player") && !objNameLower.Contains("cube") ||
                objNameLower.Contains("main"))
            {
                continue;
            }
            
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            Renderer renderer = obj.GetComponent<Renderer>();
            
            if (meshFilter != null && renderer != null)
            {
                Vector3 pos = obj.transform.position;
                
                bool isInGridRange = (pos.x >= -1 && pos.x <= GameSettings.rowsPerPlane + 1 &&
                                      pos.y >= -1 && pos.y <= GameSettings.numberOfPlanes + 1 &&
                                      pos.z >= -1 && pos.z <= GameSettings.columnsPerPlane + 1);
                
                if (isInGridRange)
                {
                    toDestroy.Add(obj);
                }
            }
        }
        
        foreach (GameObject obj in toDestroy)
        {
            if (obj != null)
            {
                Debug.Log($"🗑️ Destruyendo: {obj.name} en posición {obj.transform.position}");
                Destroy(obj);
                additionalDestroyed++;
            }
        }
        
        Debug.Log($"🧹 Destruidos {additionalDestroyed} cubos adicionales");
        
        for (int i = 0; i < planeCubes.Length; i++)
        {
            planeCubes[i] = 0;
        }
        
        ClearGridClientRpc();
        
        Debug.Log($"✅ Grid limpiado: {gridCubesDestroyed + additionalDestroyed} cubos totales eliminados");
        
        StartCoroutine(VerifyCleanupCoroutine());
    }

    private System.Collections.IEnumerator VerifyCleanupCoroutine()
    {
        yield return new WaitForSeconds(0.5f);
        
        GameObject[] remainingCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
        
        if (remainingCubes.Length > 0)
        {
            Debug.LogWarning($"⚠️ Aún quedan {remainingCubes.Length} cubos después de limpiar!");
            
            foreach (var cube in remainingCubes)
            {
                if (cube != null)
                {
                    Debug.Log($"🗑️ Destruyendo cubo restante: {cube.name} en {cube.transform.position}");
                    Destroy(cube);
                }
            }
        }
        else
        {
            Debug.Log("✅ Verificación completa: Grid limpio correctamente");
        }
    }

    [ClientRpc]
    private void ClearGridClientRpc()
    {
        Debug.Log("🧹 Cliente limpiando cubos...");
        
        int destroyed = 0;
        
        for (int pass = 0; pass < 3; pass++)
        {
            GameObject[] frozenCubes = GameObject.FindGameObjectsWithTag("FrozenCube");
            
            foreach (var cube in frozenCubes)
            {
                if (cube != null)
                {
                    Destroy(cube);
                    destroyed++;
                }
            }
            
            if (frozenCubes.Length == 0) break;
        }
        
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> toDestroy = new List<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj == null || obj.transform.parent != null) continue;
            
            if (obj.GetComponent<NetworkObject>() != null ||
                obj.GetComponent<TetrisPieceController>() != null ||
                obj.GetComponent<Camera>() != null ||
                obj.GetComponent<Light>() != null ||
                obj.GetComponent<Canvas>() != null ||
                obj.GetComponent<NetworkManager>() != null ||
                obj.GetComponent<AudioListener>() != null ||
                obj.GetComponent<GameManager>() != null ||
                obj.GetComponent<GridManager>() != null)
            {
                continue;
            }
            
            string objNameLower = obj.name.ToLower();
            if (objNameLower.Contains("grid") ||
                objNameLower.Contains("floor") ||
                objNameLower.Contains("wall") ||
                objNameLower.Contains("plane") && !objNameLower.Contains("cube") ||
                objNameLower.Contains("manager") ||
                objNameLower.Contains("network"))
            {
                continue;
            }
            
            MeshFilter meshFilter = obj.GetComponent<MeshFilter>();
            Renderer renderer = obj.GetComponent<Renderer>();
            
            if (meshFilter != null && renderer != null)
            {
                Vector3 pos = obj.transform.position;
                bool isInGridRange = (pos.x >= -1 && pos.x <= GameSettings.rowsPerPlane + 1 &&
                                      pos.y >= -1 && pos.y <= GameSettings.numberOfPlanes + 1 &&
                                      pos.z >= -1 && pos.z <= GameSettings.columnsPerPlane + 1);
                
                if (isInGridRange)
                {
                    toDestroy.Add(obj);
                }
            }
        }
        
        foreach (GameObject obj in toDestroy)
        {
            if (obj != null)
            {
                Destroy(obj);
                destroyed++;
            }
        }
        
        Debug.Log($"✅ Cliente eliminó {destroyed} cubos");
    }
}