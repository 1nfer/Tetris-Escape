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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    private GameObject floor;
    private int yRotAngle = 0;
    private int xRotAngle = 0;

    // Start is called before the first frame update
    void Start()
    {
        floor = GameObject.Find("/Floor");
    }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetKey(GameSettings.rotateCameraYLeftKey)) {
        //     transform.RotateAround(floor.transform.position, floor.transform.up, 3);
        //     yRotAngle += 3;
        //     if (yRotAngle == 183) {
        //         yRotAngle = -177;
        //     }
        //     AdjustKeysToCamera();
        // }

        // if (Input.GetKey(GameSettings.rotateCameraYRightKey)) {
        //     transform.RotateAround(floor.transform.position, floor.transform.up, -3);
        //     yRotAngle -= 3;
        //     if (yRotAngle == -183) {
        //         yRotAngle = 177;
        //     }
        //     AdjustKeysToCamera();
        // }

        // if (Input.GetKey(GameSettings.rotateCameraXDownKey) && xRotAngle < 20) {
        //     transform.RotateAround(floor.transform.position, transform.right, 1);
        //     xRotAngle++;
        // }

        // if (Input.GetKey(GameSettings.rotateCameraXUpKey) && xRotAngle > -20) {
        //     transform.RotateAround(floor.transform.position, transform.right, -1);
        //     xRotAngle--;
        // }

        // if (Input.GetKey(GameSettings.resetCameraKey)) {
        //     // Must match initial value in editor.
        //     transform.position = new Vector3(2.5f, 20f, -3f);
        //     transform.rotation = Quaternion.Euler(55, 0, 0);
        //     yRotAngle = 0;
        //     xRotAngle = 0;
        //     AdjustKeysToCamera();
        // }
    }

    private void AdjustKeysToCamera()
    {
        if (yRotAngle <= 45 && yRotAngle >= -45) {
            GameSettings.moveXAxisNegativeKey = KeyCode.LeftArrow;
            GameSettings.moveXAxisPositiveKey = KeyCode.RightArrow;
            GameSettings.moveZAxisNegativeKey = KeyCode.DownArrow;
            GameSettings.moveZAxisPositiveKey = KeyCode.UpArrow;
        } else if (yRotAngle > 45 && yRotAngle <= 135) {
            GameSettings.moveXAxisNegativeKey = KeyCode.DownArrow;
            GameSettings.moveXAxisPositiveKey = KeyCode.UpArrow;
            GameSettings.moveZAxisNegativeKey = KeyCode.RightArrow;
            GameSettings.moveZAxisPositiveKey = KeyCode.LeftArrow;
        } else if (yRotAngle > 135 && yRotAngle <= 180) {
            GameSettings.moveXAxisNegativeKey = KeyCode.RightArrow;
            GameSettings.moveXAxisPositiveKey = KeyCode.LeftArrow;
            GameSettings.moveZAxisNegativeKey = KeyCode.UpArrow;
            GameSettings.moveZAxisPositiveKey = KeyCode.DownArrow;
        } else if (yRotAngle >= -135 && yRotAngle <= -45) {
            GameSettings.moveXAxisNegativeKey = KeyCode.UpArrow;
            GameSettings.moveXAxisPositiveKey = KeyCode.DownArrow;
            GameSettings.moveZAxisNegativeKey = KeyCode.LeftArrow;
            GameSettings.moveZAxisPositiveKey = KeyCode.RightArrow;
        } else if (yRotAngle >= -180 && yRotAngle < -135) {
            GameSettings.moveXAxisNegativeKey = KeyCode.RightArrow;
            GameSettings.moveXAxisPositiveKey = KeyCode.LeftArrow;
            GameSettings.moveZAxisNegativeKey = KeyCode.UpArrow;
            GameSettings.moveZAxisPositiveKey = KeyCode.DownArrow;
        }
    }
}
