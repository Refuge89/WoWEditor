﻿using System;
using System.Windows.Forms;
using SharpDX;
using WoWEditor6.Settings;
using WoWEditor6.Utils;
using Point = System.Drawing.Point;

namespace WoWEditor6.Scene
{
    class CameraControl
    {
        public delegate void PositionChangedHandler(Vector3 newPosition, bool updateTerrain);

        private readonly Control mWindow;
        private Point mLastCursorPos;
        private DateTime mLastUpdate = DateTime.Now;

        private const float SpeedFactor = 100.0f;
        private const float SpeedFactorWheel = 0.5f;
        public bool InvertX { get; set; }
        public bool InvertY { get; set; }

        public float TurnFactor { get; set; }

        public event PositionChangedHandler PositionChanged;

        public CameraControl(Control window)
        {
            TurnFactor = 0.2f;
            mWindow = window;
        }

        public void Update(Camera cam, bool stateOnly)
        {
            if (mWindow.Focused == false || stateOnly)
            {
                mLastCursorPos = Cursor.Position;
                mLastUpdate = DateTime.Now;
                return;
            }

            var keyState = new byte[256];
            UnsafeNativeMethods.GetKeyboardState(keyState);

            var positionChanged = false;
            var updateTerrain = false;
            var diff = (float)(DateTime.Now - mLastUpdate).TotalSeconds;

            var camBind = KeyBindings.Instance.Camera;

            if (KeyHelper.AreKeysDown(keyState, camBind.Forward))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveForward(diff * SpeedFactor);
            }

            if (KeyHelper.AreKeysDown(keyState, camBind.Backward))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveForward(-diff * SpeedFactor);
            }

            if (KeyHelper.AreKeysDown(keyState, camBind.Right))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveRight(diff * SpeedFactor);
            }

            if (KeyHelper.AreKeysDown(keyState, camBind.Left))
            {
                positionChanged = true;
                updateTerrain = true;
                cam.MoveRight(-diff * SpeedFactor);
            }

            if (KeyHelper.AreKeysDown(keyState, camBind.Up))
            {
                positionChanged = true;
                cam.MoveUp(diff * SpeedFactor);
            }

            if (KeyHelper.AreKeysDown(keyState, camBind.Down))
            {
                positionChanged = true;
                cam.MoveUp(-diff * SpeedFactor);
            }

            if (KeyHelper.IsKeyDown(keyState, Keys.RButton))
            {
                var curPos = Cursor.Position;
                var dx = curPos.X - mLastCursorPos.X;
                var dy = curPos.Y - mLastCursorPos.Y;

                if (dx != 0)
                    cam.Yaw(dx * TurnFactor * (InvertX ? 1 : -1));

                if (dy != 0)
                    cam.Pitch(dy * TurnFactor * (InvertY ? 1 : -1));
            }

            if (positionChanged && PositionChanged != null)
                PositionChanged(cam.Position, updateTerrain);

            mLastUpdate = DateTime.Now;
            mLastCursorPos = Cursor.Position;
        }

        public void HandleMouseWheel(int delta)
        {
            var keyState = new byte[256];
            UnsafeNativeMethods.GetKeyboardState(keyState);

            if (KeyHelper.IsKeyDown(keyState, Keys.RButton))
            {
                var cam = WorldFrame.Instance.ActiveCamera;
                cam.MoveForward(delta * SpeedFactorWheel);
                WorldFrame.Instance.MapManager.UpdatePosition(cam.Position, true);
            }
        }
    }
}