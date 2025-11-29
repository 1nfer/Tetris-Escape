// MIT License
//
// Copyright (c) 2021 Filipe Manana <fdmanana@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using UnityEngine;

public static class GameSettings
{
    public const bool debugMode = false;

    // The game grid is organized as P planes, each with NxM cubes, so the grid
    // is defined as:  GameObject[P, M, N].
    //
    // A plane is parallel to XoZ plane.
    // So given the index of a cube, which corresponds to the vertex of the
    // cube that has the lowest x, lowest y and lowest z coordinates, we index
    // the grid like this:
    //
    // Grid[vertex.y][vertex.x][vertex.z]
    //
    public const int numberOfPlanes = 20;
    public const int rowsPerPlane = 9;
    public const int columnsPerPlane = 9;
    public const int cubesPerPlane = rowsPerPlane * columnsPerPlane;

    public const int scorePerPlane = 10;

    // Update piece drop timeout after every N points.
    public const int scoreTimeoutTrigger = 50;

    // Update piece drop timeout after every N seconds of play time.
    public const float timeTimeoutTrigger = 40f;

    // Each tetris pice is composed of cubes with a scale of 1 for x, y and z.
    // This center offset is added or subtracted to a cube's transform position,
    // which is its center, to get coordinates of a vertex.
    public const float cubeCenterOffset = 0.5f;

    public const KeyCode helpKey = KeyCode.H;
    public const KeyCode pauseKey = KeyCode.P;

    // public const KeyCode rotateCameraYLeftKey = KeyCode.Q;
    // public const KeyCode rotateCameraYRightKey = KeyCode.W;

    // public const KeyCode rotateCameraXDownKey = KeyCode.A;
    // public const KeyCode rotateCameraXUpKey = KeyCode.S;

    // public const KeyCode resetCameraKey = KeyCode.R;

    public const KeyCode rotatePieceYAxis = KeyCode.Z;
    public const KeyCode rotatePieceXAxis = KeyCode.X;

    public const KeyCode moveYAxisNegativeKey = KeyCode.Space;

    public const KeyCode moveYAxisPositiveKey = KeyCode.Underscore;

    public static KeyCode moveXAxisNegativeKey = KeyCode.LeftArrow;
    public static KeyCode moveXAxisPositiveKey = KeyCode.RightArrow;
    public static KeyCode moveZAxisNegativeKey = KeyCode.DownArrow;
    public static KeyCode moveZAxisPositiveKey = KeyCode.UpArrow;
}
