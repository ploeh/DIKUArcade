﻿using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace DIKUArcade {
    /// <summary>
    /// Aspect ratio for a DIKUArcade.Window object, defining
    /// width as a function of height.
    /// </summary>
    public enum AspectRatio {
        R4X3,
        R16X9,
        R1X1
    }

    /// <summary>
    ///     Create an OpenTK window wrapper, where we only show
    ///     relevant data members and hide unneeded functionality.
    /// </summary>
    public class Window {
        private static uint screenShotCounter;

        private GameWindow window;

        // This is the signature for a key event handler:
        //private delegate void KeyEventHandler(object sender, KeyboardKeyEventArgs e);
        private EventHandler<KeyboardKeyEventArgs> defaultKeyHandler;

        private bool isRunning;

        // TODO: Remove these?
        private uint width, height;

        private string title;

        private void ActivateThisWindowContext() {
            window = new GameWindow((int) this.width, (int) this.height, GraphicsMode.Default,
                this.title, GameWindowFlags.Default, DisplayDevice.Default,
                3, 3, // OpenGL major and minor version
                GraphicsContextFlags.ForwardCompatible);

            GL.ClearDepth(1);
            GL.ClearColor(Color.Black);

            AddDefaultKeyEventHandler();

            isRunning = true;
            window.Context.MakeCurrent(window.WindowInfo);
            window.Visible = true;
        }

        public Window(string title, uint width, uint height)
        {
            this.width = width;
            this.height = height;
            this.title = title;
            isRunning = true;
            ActivateThisWindowContext();
        }

        public Window(string title, uint height, AspectRatio aspect) {
            this.height = height;
            switch (aspect) {
            case AspectRatio.R1X1:
                width = this.height;
                break;
            case AspectRatio.R4X3:
                width = this.height * 4 / 3;
                break;
            case AspectRatio.R16X9:
                width = this.height * 16 / 9;
                break;
            default:
                throw new InvalidEnumArgumentException();
            }
            ActivateThisWindowContext();
        }

        // TODO: Do we want/need to make the window resizable?
        public bool Resizable { get; set; }

        private void AddDefaultKeyEventHandler() {
            defaultKeyHandler = delegate(object sender, KeyboardKeyEventArgs e) {
                if (e.Key == Key.Escape) {
                    CloseWindow();
                    return;
                }
                if (e.Key == Key.F12) {
                    SaveScreenShot();
                }
            };
            window.Keyboard.KeyDown += defaultKeyHandler;
        }

        private void RemoveDefaultKeyEventHandler() {
            if (defaultKeyHandler != null) {
                window.Keyboard.KeyDown -= defaultKeyHandler;
                defaultKeyHandler = null;
            }
        }

        /// <summary>
        /// Add an event handler for when any keyboard key is pressed.
        /// </summary>
        /// <param name="method">Delegate method</param>
        public void AddKeyPressEventHandler(EventHandler<KeyboardKeyEventArgs> method) {
            RemoveDefaultKeyEventHandler();
            window.Keyboard.KeyUp += method;
        }

        /// <summary>
        /// Add an event handler for when any keyboard key is released.
        /// </summary>
        /// <param name="method">Delegate method</param>
        public void AddKeyReleaseEventHandler(EventHandler<KeyboardKeyEventArgs> method) {
            RemoveDefaultKeyEventHandler();
            window.Keyboard.KeyDown += method;
        }

        /// <summary>
        /// Check if the Window is still running.
        /// </summary>
        public bool IsRunning() {
            return isRunning;
        }

        /// <summary>
        /// Close the Window.
        /// </summary>
        public void CloseWindow() {
            isRunning = false;
            window.Close();
        }

        /// <summary>
        /// Clear the Window with a uniform background color.
        /// </summary>
        public void Clear() {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        }

        /// <summary>
        /// Set color to be used as clear color when using the Window.Clear() method.
        /// </summary>
        /// <param name="vec">Vec3F containing the RGB color values.</param>
        /// <exception cref="ArgumentOutOfRangeException">Normalized color values must be
        /// between 0 and 1.</exception>
        public void SetClearColor(Math.Vec3F vec) {
            if (vec.X < 0.0f || vec.X > 1.0f ||
                vec.Y < 0.0f || vec.Y > 1.0f ||
                vec.Z < 0.0f || vec.Z > 1.0f) {
                throw new ArgumentOutOfRangeException($"RGB Color values must be between 0 and 1: {vec}");
            }
            GL.ClearColor(vec.X, vec.Y, vec.Z, 1.0f);
        }

        /// <summary>
        /// Set color to be used as clear color when using the Window.Clear() method.
        /// </summary>
        /// <param name="vec">Vec3I containing the RGB color values.</param>
        /// <exception cref="ArgumentOutOfRangeException">Color values must be between 0 and 255.</exception>
        public void SetClearColor(Math.Vec3I vec) {
            if (vec.X < 0 || vec.X > 255 ||
                vec.Y < 0 || vec.Y > 255 ||
                vec.Z < 0 || vec.Z > 255) {
                throw new ArgumentOutOfRangeException($"RGB Color values must be between 0 and 255: {vec}");
            }
            GL.ClearColor((float)vec.X / 255.0f, (float)vec.Y / 255.0f, (float)vec.Z / 255.0f, 1.0f);
        }

        /// <summary>
        /// Swap double buffers for the Window.
        /// </summary>
        public void SwapBuffers() {
            window.SwapBuffers();
            //window.Context.SwapBuffers();
        }

        /// <summary>
        /// Check for incoming keyboard or mouse events.
        /// </summary>
        public void PollEvents() {
            window.ProcessEvents();
        }

        /// <summary>
        /// Save a screenshot of the Window's current state.
        /// </summary>
        /// <exception cref="GraphicsContextMissingException"></exception>
        public void SaveScreenShot() {
            if (GraphicsContext.CurrentContext == null) {
                throw new GraphicsContextMissingException();
            }

            var bmp = new Bitmap(window.ClientSize.Width, window.ClientSize.Height);
            var data =
                bmp.LockBits(window.ClientRectangle, ImageLockMode.WriteOnly,
                    PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, window.ClientSize.Width, window.ClientSize.Height,
                OpenTK.Graphics.OpenGL.PixelFormat.Bgr,
                PixelType.UnsignedByte, data.Scan0);
            bmp.UnlockBits(data);

            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bmp.Save($"screenShot_{Window.screenShotCounter}.bmp");
            Window.screenShotCounter++;
        }
    }
}