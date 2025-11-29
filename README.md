## **Descripción del Proyecto**

**Tetris Escape** transforma el clásico Tetris en un **juego cooperativo asimétrico** para **2 jugadores**:

| **Jugador PC** | **Jugador VR** |
|---------------|----------------|
| **Constructor** | **Explorador** |
| Coloca piezas Tetris | Muevete y escala con puño |
| Controles: Flechas + Z/X | Gestos XR Hands |

Sistema de **4 paredes coloreadas**:
- 🟥 **ROJO** = Izquierda
- 🟩 **VERDE** = Derecha  
- 🔵 **AZUL** = Frente
- 🟨 **AMARILLO** = Atrás

## **Requisitos**

| **Componente** | **Versión Mínima** |
|----------------|-------------------|
| **Unity** | 2022.3.62f3 |
| **Meta Quest 3** | Oculus Link/Air Link |
| **PC** | GTX 1060+ / i5 4GB RAM |
| **Red** | **Misma WiFi LAN** |

Instalación y Ejecución del Proyecto

1. Clonar Repositorio
bashgit clone https://github.com/tuusuario/TetrisEscape.git
cd TetrisEscape

2. Abrir en Unity
bash# Unity Hub → Add → Selecciona carpeta TetrisEscape

# Unity 2022.3.62f3 → Abrir proyecto

3. Instalar Paquetes (Unity Package Manager)

* Netcode for GameObjects (1.7.1)
* Oculus XR Plugin (4.4.4)
* XR Interaction Toolkit (2.5.2)
* Input System (1.8.0)

Importar automáticamente: 
- Window → Package Manager → Unity Registry

4. Configurar Build Settings

| Build  | Settings |
| ------------- |:-------------:|
| PC Build      | File → Build Settings → PC, Mac & Linux → Switch Platform     |
| VR Build      | File → Build Settings → Android → Switch Platform → Oculus     |

5. Configurar IP LAN

```
// NetworkConnectionManager.cs (Línea 15)
public string serverIPAddress = "192.168.1.XXX";  // ← TU IP LOCAL
```

**Obtener IP:** 
- Windows: `cmd` → `ipconfig`
- Mac: `ifconfig`

## 6. Builds Separados

### 🔵 BUILD PC (Servidor)
```
File → Build Settings → PC Build
Scenes: MainScene
Build → TetrisEscape_PC.exe
```

### 🔶 BUILD VR (Cliente)
```
File → Build Settings → Android
Player Settings:
  - XR: Oculus
  - Minimum API: 29
Build → TetrisEscape_VR.apk
→ ADB sideload a Quest 3
```

## **Cómo Jugar**
```
PC: Ejecutar TetrisEscape_PC.exe
VR: Abrir app en Quest 3
VR: Conectar misma WiFi
PC: Esperar "VR Conectado" (verde)
¡JUEGO INICIADO AUTOMÁTICO!
```
