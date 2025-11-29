using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class TetrisPieceController : NetworkBehaviour
{
    public enum PieceShape {
        L,
        Z,
        T,
        I,
        O,
        Single
    };

    private enum RotationAxis {
        X,
        Y,
    };

    private float nextDrop;

    // These offsets, for x, y and z, are what we sum into the piece's current
    // position at Start()/Awake() so that the piece is moved to the bottom
    // left and front corner of the game grid.
    public float centerOffsetX;
    public float centerOffsetY;
    public float centerOffsetZ;

    // Set in the editor.
    public PieceShape pieceShape;
    public AudioClip moveSFX;
    public AudioClip canNotMoveSFX;

    private bool canRotate = true;
    // Piece can't be moved or rotated, not part of the game.
    private bool inGame = true;
    
    // Variable de red para sincronizar la posición
    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        Vector3.zero, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server);

    public void SetUnplayable()
    {
        inGame = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Suscribirse a cambios de posición en clientes
        if (!IsServer)
        {
            networkPosition.OnValueChanged += OnPositionChanged;
        }
    }

    private void OnPositionChanged(Vector3 oldPos, Vector3 newPos)
    {
        // Sincronizar posición en clientes
        transform.position = newPos;
    }

    void Start()
    {
        // Solo el servidor ejecuta la lógica de inicio
        if (!IsServer || !inGame) {
            return;
        }

        bool collision = false;
        float xStart = centerOffsetX;
        float yStart = centerOffsetY;
        float zStart = centerOffsetZ;

        switch (pieceShape) {
            case PieceShape.T:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.I:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 3.0f;
                break;
            case PieceShape.Z:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.L:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.O:
                xStart += 1.0f;
                yStart += GameSettings.numberOfPlanes - 2;
                zStart += 2.0f;
                break;
            case PieceShape.Single:
                xStart += 2.0f;
                yStart += GameSettings.numberOfPlanes - 1;
                zStart += 3.0f;
                canRotate = false;
                break;
        }

        // If the piece overlaps any previously layed down piece, we have a
        // game over, and we move the piece up until it no longer overlaps,
        // just for visualization purposes.
        transform.Translate(xStart, yStart, zStart, Space.World);
        networkPosition.Value = transform.position;
        
        DumpChildrenGridIndices();
        while (InvalidPosition(true)) {
            transform.Translate(0, 1, 0, Space.World);
            networkPosition.Value = transform.position;
            collision = true;
        }

        if (collision) {
            GameManager.gm.GameOver();
        } else if (GridManager.gm.MaybeFreezePiece(gameObject)) {
            GameManager.gm.GameOver();
        }

        nextDrop = 0;
    }

    private string GridIndexString(Transform cube)
    {
        int plane = TetrisHelpers.GetPieceCubePlane(cube);
        int row = TetrisHelpers.GetPieceCubeRow(cube);
        int col = TetrisHelpers.GetPieceCubeColumn(cube);

        return "[" + plane + ", " + row + ", " + col + "]";
    }

    private void DumpChildrenGridIndices()
    {
        if (!GameSettings.debugMode) {
            return;
        }

        // Debug.Log("Current position: " + transform.position + " world: " + transform.TransformPoint(transform.position));

        foreach (Transform cube in transform) {
            Debug.Log("cube '" + cube.name + "' index: " + GridIndexString(cube) + " position: " + cube.position);
        }        
    }

    // Check if we can move a piece along the X axis by a certain amount.
    private bool CanMovePieceAxisX(int amount)
    {
        bool canMove = true;

        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);
            int nextRow = row + amount;

            if (nextRow < 0) {
                canMove = false;
                break;
            }
            if (nextRow >= GameSettings.rowsPerPlane) {
                canMove = false;
                break;
            }
            if (GridManager.gm.GridCellOccupied(plane, nextRow, col)) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            PlaySoundClientRpc(true);
        } else {
            PlaySoundClientRpc(false);
        }

        return canMove;
    }

    // Check if we can move a piece along the Z axis by a certain amount.
    private bool CanMovePieceAxisZ(int amount)
    {
        bool canMove = true;

        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);
            int nextCol = col + amount;

            if (nextCol < 0) {
                canMove = false;
                break;
            }
            if (nextCol >= GameSettings.columnsPerPlane) {
                canMove = false;
                break;
            }

            if (GridManager.gm.GridCellOccupied(plane, row, nextCol)) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            PlaySoundClientRpc(true);
        } else {
            PlaySoundClientRpc(false);
        }

        return canMove;
    }

    // Check if we can move a piece along the Y axis by a certain amount.
    private bool CanMovePieceAxisY(int amount)
    {
        bool canMove = true;

        foreach (Transform child in transform) {
            float sum = child.position.y + amount;

            if (amount < 0 && sum < 0) {
                canMove = false;
                break;
            }
            if (sum >= GameSettings.numberOfPlanes) {
                canMove = false;
                break;
            }
        }

        if (canMove) {
            PlaySoundClientRpc(true);
        } else {
            PlaySoundClientRpc(false);
        }

        return canMove;
    }

    [ClientRpc]
    private void PlaySoundClientRpc(bool isMove)
    {
        if (isMove)
        {
            AudioSource.PlayClipAtPoint(moveSFX, transform.position);
        }
        else
        {
            AudioSource.PlayClipAtPoint(canNotMoveSFX, transform.position);
        }
    }

    private bool InvalidPosition(bool ignoreYPosition = false)
    {
        foreach (Transform cube in transform) {
            int plane = TetrisHelpers.GetPieceCubePlane(cube);
            int row = TetrisHelpers.GetPieceCubeRow(cube);
            int col = TetrisHelpers.GetPieceCubeColumn(cube);

            // One cube of the piece is going outside the game grid...
            if (row < 0 || row >= GameSettings.rowsPerPlane ||
                col < 0 || col >= GameSettings.columnsPerPlane) {
                    return true;
            }

            if (!ignoreYPosition) {
                if (plane < 0 || plane >= GameSettings.numberOfPlanes) {
                    return true;
                }
            }

            if (ignoreYPosition && plane >= GameSettings.numberOfPlanes) {
                continue;
            }

            // One cube of the piece is in a grid position that already has a cube
            // from a previous piece...
            if (GridManager.gm.GridCellOccupied(plane, row, col)) {
                return true;
            }
        }

        return false;
    }

    private bool RotatePiece(RotationAxis axis)
    {
        if (!canRotate) {
            return false;
        }

        float xAngle = 0;
        float yAngle = 0;

        switch (axis) {
            case RotationAxis.X:
                xAngle = 45;
                break;
            case RotationAxis.Y:
                yAngle = 45;
                break;
        }

        transform.Rotate(xAngle, yAngle, 0, Space.World);

        if (InvalidPosition()) {
            transform.Rotate(-xAngle, -yAngle, 0, Space.World);
            PlaySoundClientRpc(false);
            return false;
        }

        transform.Rotate(xAngle, yAngle, 0, Space.World);

        if (InvalidPosition()) {
            transform.Rotate(-xAngle * 2, -yAngle * 2, 0, Space.World);
            PlaySoundClientRpc(false);
            return false;
        }

        PlaySoundClientRpc(true);
        networkPosition.Value = transform.position;
        return true;
    }

    private void LayPieceDown()
    {
        do {
            transform.Translate(0, -1, 0, Space.World);
            networkPosition.Value = transform.position;
        } while (!GridManager.gm.MaybeFreezePiece(gameObject));

        GameManager.gm.SpawnNextPiece();
    }

    void Update()
    {
        // Solo el servidor procesa input y mueve piezas
        if (!IsServer || !inGame || !GameManager.gm.IsGameActive()) {
            return;
        }

        nextDrop += Time.deltaTime;

        if (nextDrop >= GameManager.gm.GetTimeout()) {
            transform.Translate(0, -1, 0, Space.World);
            networkPosition.Value = transform.position;
            nextDrop = 0;
            if (GridManager.gm.MaybeFreezePiece(gameObject)) {
                GameManager.gm.SpawnNextPiece();
                return;
            }
        }

        // Piece moved or rotated.
        bool pieceMoved = false;

        if (Input.GetKeyDown(GameSettings.moveYAxisNegativeKey)) {
            // Move along Y axis in the negative direction (down).
            if (CanMovePieceAxisY(-1)) {
                LayPieceDown();
            }
        } else if (GameSettings.debugMode && Input.GetKeyDown(GameSettings.moveYAxisPositiveKey)) {
            // Move along Y axis in the positive direction (up) - debug mode only.
            if (CanMovePieceAxisY(1)) {
                transform.Translate(0, 1, 0, Space.World);
                networkPosition.Value = transform.position;
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveXAxisNegativeKey)) {
            // Move along X axis in the negative direction.
            if (CanMovePieceAxisX(-1)) {
                transform.Translate(-1, 0, 0, Space.World);
                networkPosition.Value = transform.position;
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveXAxisPositiveKey)) {
            // Move along X axis in the positive direction.
            if (CanMovePieceAxisX(1)) {
                transform.Translate(1, 0, 0, Space.World);
                networkPosition.Value = transform.position;
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveZAxisNegativeKey)) {
            // Move along Z axis in the negative direction.
            if (CanMovePieceAxisZ(-1)) {
                transform.Translate(0, 0, -1, Space.World);
                networkPosition.Value = transform.position;
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.moveZAxisPositiveKey)) {
            // Move along Z axis in the positive direction.
            if (CanMovePieceAxisZ(1)) {
                transform.Translate(0, 0, 1, Space.World);
                networkPosition.Value = transform.position;
                pieceMoved = true;
            }
        } else if (Input.GetKeyDown(GameSettings.rotatePieceXAxis)) {
            // Rotate by +90 degrees on the X axis (if possible).
            pieceMoved = RotatePiece(RotationAxis.X);
        } else if (Input.GetKeyDown(GameSettings.rotatePieceYAxis)) {
            // Rotate by +90 degrees on the Y axis (if possible).
            pieceMoved = RotatePiece(RotationAxis.Y);
        }

        if (pieceMoved) {
            DumpChildrenGridIndices();
            if (GridManager.gm.MaybeFreezePiece(gameObject)) {
                GameManager.gm.SpawnNextPiece();
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            networkPosition.OnValueChanged -= OnPositionChanged;
        }
        base.OnNetworkDespawn();
    }
}