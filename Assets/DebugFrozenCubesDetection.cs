using UnityEngine;

/// <summary>
/// Script de debugging para visualizar cubos congelados y detección
/// Agregar a cualquier GameObject en la escena
/// </summary>
public class DebugFrozenCubesDetection : MonoBehaviour
{
    [Header("Debugging Options")]
    public bool showAllFrozenCubes = true;
    public bool showRaycastsFromPlayer = true;
    public bool logCubeInfo = false;
    
    [Header("Player Reference")]
    public Transform vrPlayer; // Asignar manualmente o buscar automáticamente
    
    private GameObject[] frozenCubes;
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F) || logCubeInfo)
        {
            ListAllFrozenCubes();
            logCubeInfo = false;
        }
        
        if (Input.GetKeyDown(KeyCode.T))
        {
            TestRaycastFromPlayer();
        }
    }
    
    void ListAllFrozenCubes()
    {
        frozenCubes = GameObject.FindObjectsOfType<GameObject>();
        int count = 0;
        
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("🔍 LISTANDO TODOS LOS CUBOS CONGELADOS");
        Debug.Log("═══════════════════════════════════════");
        
        foreach (GameObject obj in frozenCubes)
        {
            if (obj.name.StartsWith("FrozenCube_"))
            {
                count++;
                
                // Información detallada del cubo
                Collider col = obj.GetComponent<Collider>();
                Rigidbody rb = obj.GetComponent<Rigidbody>();
                Renderer rend = obj.GetComponent<Renderer>();
                
                Debug.Log($"\n📦 Cubo #{count}: {obj.name}");
                Debug.Log($"   Posición: {obj.transform.position}");
                Debug.Log($"   Escala: {obj.transform.localScale}");
                Debug.Log($"   Layer: {LayerMask.LayerToName(obj.layer)} (#{obj.layer})");
                Debug.Log($"   Tag: {obj.tag}");
                Debug.Log($"   Activo: {obj.activeInHierarchy}");
                
                if (col != null)
                {
                    Debug.Log($"   Collider: {col.GetType().Name}");
                    Debug.Log($"   Collider Enabled: {col.enabled}");
                    Debug.Log($"   Is Trigger: {col.isTrigger}");
                    Debug.Log($"   Bounds: {col.bounds.size}");
                }
                else
                {
                    Debug.LogError($"   ❌ NO TIENE COLLIDER!");
                }
                
                if (rb != null)
                {
                    Debug.Log($"   ⚠️ Tiene Rigidbody (isKinematic: {rb.isKinematic})");
                }
                
                if (rend != null)
                {
                    Debug.Log($"   Color: {rend.material.color}");
                }
            }
        }
        
        Debug.Log($"\n📊 TOTAL DE CUBOS CONGELADOS: {count}");
        Debug.Log("═══════════════════════════════════════\n");
    }
    
    void TestRaycastFromPlayer()
    {
        if (vrPlayer == null)
        {
            vrPlayer = GameObject.Find("VRPlayer(Clone)")?.transform;
            if (vrPlayer == null)
            {
                Debug.LogError("❌ No se encontró VRPlayer");
                return;
            }
        }
        
        Debug.Log("\n🎯 TESTEANDO RAYCASTS DESDE JUGADOR VR");
        Debug.Log("═══════════════════════════════════════");
        
        // Raycast hacia adelante
        Vector3 origin = vrPlayer.position + Vector3.up * 1f;
        Vector3 direction = vrPlayer.forward;
        
        Debug.Log($"📍 Origen: {origin}");
        Debug.Log($"➡️ Dirección: {direction}");
        
        // Test 1: Raycast simple
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, 5f))
        {
            Debug.Log($"\n✅ RAYCAST HIT:");
            Debug.Log($"   Objeto: {hit.collider.name}");
            Debug.Log($"   Distancia: {hit.distance}");
            Debug.Log($"   Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
        }
        else
        {
            Debug.Log("\n❌ RAYCAST NO DETECTÓ NADA");
        }
        
        // Test 2: RaycastAll
        RaycastHit[] hits = Physics.RaycastAll(origin, direction, 5f);
        Debug.Log($"\n📊 RaycastAll encontró: {hits.Length} objetos");
        foreach (var h in hits)
        {
            Debug.Log($"   - {h.collider.name} a {h.distance}m");
        }
        
        // Test 3: SphereCast
        RaycastHit[] sphereHits = Physics.SphereCastAll(origin, 0.4f, direction, 5f);
        Debug.Log($"\n📊 SphereCast encontró: {sphereHits.Length} objetos");
        foreach (var h in sphereHits)
        {
            Debug.Log($"   - {h.collider.name} a {h.distance}m");
        }
        
        Debug.Log("═══════════════════════════════════════\n");
    }
    
    void OnDrawGizmos()
    {
        if (!showAllFrozenCubes) return;
        
        // Encontrar y visualizar todos los cubos congelados
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("FrozenCube_"))
            {
                Collider col = obj.GetComponent<Collider>();
                
                if (col != null)
                {
                    // Verde si tiene collider habilitado
                    Gizmos.color = col.enabled ? Color.green : Color.red;
                    Gizmos.DrawWireCube(obj.transform.position, col.bounds.size);
                    
                    // Punto en el centro
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(obj.transform.position, 0.05f);
                }
                else
                {
                    // Rojo si NO tiene collider
                    Gizmos.color = Color.red;
                    Gizmos.DrawWireCube(obj.transform.position, obj.transform.localScale);
                }
            }
        }
        
        // Visualizar raycasts desde el jugador
        if (showRaycastsFromPlayer && vrPlayer != null)
        {
            Vector3 origin = vrPlayer.position + Vector3.up * 1f;
            Vector3 direction = vrPlayer.forward;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(origin, origin + direction * 5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, 0.4f);
            Gizmos.DrawWireSphere(origin + direction * 2f, 0.4f);
        }
    }
}