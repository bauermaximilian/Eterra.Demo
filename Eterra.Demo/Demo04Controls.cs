/*
 * Eterra Demo
 * A set of examples on how the Eterra framework can be used.
 * Copyright (C) 2020, Maximilian Bauer (contact@lengo.cc)
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
*/

using Eterra.Common;
using Eterra.Controls;
using Eterra.Graphics;
using Eterra.IO;
using System;
using System.Numerics;

namespace Eterra.Demo
{
    /// <summary>
    /// Provides a demonstration on how to handle user input through keyboard,
    /// mouse and devices using the <see cref="ControlMapping"/> functionality
    /// of <see cref="Eterra"/>.
    /// </summary>
    class Demo04Controls : EterraApplicationBase
    {
        //This application is structured into different screens - an enum
        //is used to store the currently active screen.
        private enum ControlScreen
        {
            Main,
            Evaluate,
            Control,
            Leave
        }

        //The drawing operations work pretty much as always - in this case,
        //the different screens of the application are just textures we'll 
        //draw to screen using a plane mesh. The variable user interface 
        //elements are taken from a single, so-called "sprite sheet" - but
        //that is explained later in more detail and only requires one 
        //additional drawing context property.
        private TextureBuffer controlScreenControl, controlScreenEvaluate,
            controlScreenLeave, controlScreenMain;
        private TextureBuffer controlIcons;

        private MeshBuffer plane;

        private readonly RenderParameters renderParameters;

        //The name of the demo already implied it, and here it is - control!
        //Access to user input (keyboard, mouse, gamepad etc.) can either be
        //done via "raw" access to the input devices or (preferably) with
        //"ControlMappings". These object instances take one or more input 
        //sources (like a key on the keyboard and a button on a gamepad) and
        //give them a semantic or "meaning" through the identifier name
        //of the instance. This additional step allows you, for example, to 
        //implement all your menu navigation logic using the fields "menuUp",
        //"menuDown", "menuAccept" and "menuBack" and independently change
        //what keys or buttons the user needs to press (like the "Up" arrow key
        //on the keyboard and the "Up" D-Pad button on a gamepad) to actually 
        //change the value or state of this control mapping.
        private ControlMapping menuUp, menuDown, menuAccept, menuBack;
        private ControlMapping playerLookUp, playerLookDown,
            playerLookLeft, playerLookRight, playerMoveForward,
            playerMoveBackward, playerMoveRight, playerMoveLeft,
            playerJump, playerShoot;

        //The following variables are for the application itself and don't
        //have to do much with the framework, so they'll be explained when
        //and where required.
        private ControlScreen currentScreen = ControlScreen.Main;
        private int mainScreenSelection = 0;
        private Vector2 currentPlayerMove = Vector2.Zero;
        private Vector2 currentPlayerLook = Vector2.Zero;
        private bool playerCurrentlyShooting = false;
        private DateTime lastPlayerJump = DateTime.Now;

        public Demo04Controls()
        {
            renderParameters = new RenderParameters();
            renderParameters.Camera.ProjectionMode = 
                ProjectionMode.OrthgraphicRelativeProportional;
        }

        protected override void Load()
        {
            //As mentioned in a previous example, the behaviour in case of an
            //error during resource loading can be overridden. In this case,
            //if one resources can't be loaded, an error message is logged and
            //the application is closed (instead of the default behaviour 
            //where errors are just logged and the application continues).
            void errorHandler(Exception exc)
            {
                Log.Error("One of the resources couldn't be loaded.", exc);
                Close();
            }

            //The GUI and control elements are in a very low resolution this
            //time - this is intentional, as we'll go with a 8-bit-look here.
            //For this, the interpolation of the textures need to be set to
            //"Nearest" - otherwise, the default "Linear" interpolation method 
            //would be used and the resulting image would be blurry.
            const TextureFilter pixelFilter = TextureFilter.Nearest;

            Resources.LoadTexture("/controlScreenMain.png", pixelFilter)
                .AddFinalizer(r => controlScreenMain = r, errorHandler);
            Resources.LoadTexture("/controlScreenEvaluate.png", pixelFilter)
                .AddFinalizer(r => controlScreenEvaluate = r, errorHandler);
            Resources.LoadTexture("/controlScreenControl.png", pixelFilter)
                .AddFinalizer(r => controlScreenControl = r, errorHandler);
            Resources.LoadTexture("/controlScreenLeave.png", pixelFilter)
                .AddFinalizer(r => controlScreenLeave = r, errorHandler);
            Resources.LoadTexture("/controlIcons.png", pixelFilter)
                .AddFinalizer(r => controlIcons = r, errorHandler);

            Resources.LoadMesh(MeshData.Plane).AddFinalizer(
                r => plane = r, errorHandler);

            Graphics.Mode = WindowMode.Maximized;

            //As explained above, we're linking various input device elements 
            //to the declared control mappings here using the "Map" method in
            //the "Controls" property of the EterraApplicationBase. The most 
            //usage cases are covered by the "Map" method - if not, you can 
            //either use the "MapCustom" method or just access the input units
            //directly with Controls.Input - but this goes beyond the scope of 
            //this introduction.
            menuUp = Controls.Map(KeyboardKey.Up, GamepadButton.DPadUp,
                GamepadAxis.LeftStickUp);
            menuDown = Controls.Map(KeyboardKey.Down, 
                GamepadButton.DPadDown, GamepadAxis.LeftStickDown);
            menuAccept = Controls.Map(KeyboardKey.Enter, GamepadButton.A);
            menuBack = Controls.Map(KeyboardKey.Escape, GamepadButton.B);

            playerLookUp = Controls.Map(MouseSpeedAxis.Up,
                GamepadAxis.RightStickUp);
            playerLookRight = Controls.Map(MouseSpeedAxis.Right,
                GamepadAxis.RightStickRight);
            playerLookDown = Controls.Map(MouseSpeedAxis.Down,
                GamepadAxis.RightStickDown);
            playerLookLeft = Controls.Map(MouseSpeedAxis.Left,
                GamepadAxis.RightStickLeft);
            playerMoveForward = Controls.Map(KeyboardKey.W,
                GamepadAxis.LeftStickUp);
            playerMoveRight = Controls.Map(KeyboardKey.D,
                GamepadAxis.LeftStickRight);
            playerMoveBackward = Controls.Map(KeyboardKey.S,
                GamepadAxis.LeftStickDown);
            playerMoveLeft = Controls.Map(KeyboardKey.A,
                GamepadAxis.LeftStickLeft);

            playerJump = Controls.Map(KeyboardKey.Space,
                GamepadButton.A);
            playerShoot = Controls.Map(MouseButton.Left, 
                GamepadAxis.RightTrigger);            
        }

        //If you're a fan of lambda expressions and have a relatively simple 
        //drawing logic, you're gonna like this shortcut.
        protected override void Redraw(TimeSpan delta)
            => Graphics.Render<IRenderContext>(renderParameters, Redraw);

        private void Redraw(IRenderContext c)
        {
            c.Mesh = plane;

            c.Color = Color.Parse("#696969"); //nice.
            c.Location = MathHelper.CreateTransformation(
                0.5f, 0.5f, 5, 5, 5, 1);

            c.Draw();

            //The "LoadingTasksPending" property returns the amount of 
            //resources which have not been loaded yet - in this case, we'll
            //stop redrawing the screen after drawing the grey background
            //if there are still resources to be loaded.
            if (Resources.LoadingTasksPending > 0) return;

            c.Color = Color.Transparent;
            c.Location = Matrix4x4.CreateTranslation(0.5f, 0.5f, 4);

            if (currentScreen == ControlScreen.Main)
            {
                c.Texture = controlScreenMain;
                c.Draw();

                //To avoid having to load a single texture for every different
                //movable or animatable little user interface element, we can
                //just put all these small icons into one texture, using that
                //texture as the currently active texture - and then, right
                //before drawing the plane with the full texture, we specify
                //the section of that texture to be used. By default, this
                //"texture clipping" is a rectangle at position 0/0 with size 
                //100%/100% (1/1) that contains the full texture - but when 
                //it's moved and scaled appropriately, the plane mesh will
                //suddenly only contain a small subsection of the whole 
                //texture. This principle is also used in rendering sprite 
                //fonts and could also be used in rendering sprite animations.
                c.Texture = controlIcons;
                c.TextureClipping = new Rectangle(0, 0.84f, 0.33f, 0.17f);

                c.Location = MathHelper.CreateTransformation(0.132f,
                    0.45f - 0.079f * mainScreenSelection,
                    2, 0.08f, 0.08f, 1);

                c.Draw();
            }
            else if (currentScreen == ControlScreen.Evaluate)
            {
                c.Texture = controlScreenEvaluate;
                c.Draw();
            }
            else if (currentScreen == ControlScreen.Leave)
            {
                c.Texture = controlScreenLeave;
                c.Draw();
            }
            else if (currentScreen == ControlScreen.Control)
            {
                c.Texture = controlScreenControl;
                c.Draw();

                c.Texture = controlIcons;
                //It is recommended to use "power-of-two"-sized textures
                //(with the same width and height which is a potency of two,
                //like 512x512) and to subdivide the sizes of the individual
                //sprites in a similar manner. If you don't do that,
                //finding the correct texture clipping is often a tedioius 
                //matter of trial and error and will fill your code with very 
                //weird magic numbers... just like the ones below. 
                c.TextureClipping =
                    new Rectangle(0.672f, 0.666f, 0.02f, 0.02f);

                c.Opacity = Math.Min(1, Math.Abs(currentPlayerMove.X) * 10);
                c.Location = MathHelper.CreateTransformation(
                    0.1835f + 0.04f * currentPlayerMove.X,
                    0.5112f,
                    2, 0.008f, 0.008f, 1);
                c.Draw();

                c.Opacity = Math.Min(1, Math.Abs(currentPlayerMove.Y) * 10);
                c.Location = MathHelper.CreateTransformation(
                    0.1835f,
                    0.5112f + 0.04f * currentPlayerMove.Y,
                    2, 0.008f, 0.008f, 1);
                c.Draw();


                c.Opacity = Math.Min(1, Math.Abs(currentPlayerLook.X) * 10);
                c.Location = MathHelper.CreateTransformation(
                    0.4217f + 0.1835f + 0.04f * currentPlayerLook.X,
                    0.5112f,
                    2, 0.008f, 0.008f, 1);
                c.Draw();

                c.Opacity = Math.Min(1, Math.Abs(currentPlayerLook.Y) * 10);
                c.Location = MathHelper.CreateTransformation(
                    0.4217f + 0.1835f,
                    0.5112f + 0.04f * currentPlayerLook.Y,
                    2, 0.008f, 0.008f, 1);
                c.Draw();

                c.Opacity = 1;

                //A minor optical thing: When the "jump" is activated, the
                //light will be on for a bit longer than the one of "shoot", 
                //which turns off right after the player stops shooting.
                //The reason behind this is... well, you can do rapid-fire, but
                //rapid-jump would just be ridiculous. I mean, it does sound
                //kinda hillarious though, the more I think about it...
                if ((DateTime.Now - lastPlayerJump) <
                        TimeSpan.FromSeconds(0.75))
                    c.TextureClipping =
                        new Rectangle(0, 0.0208f, 0.625f, 0.312f);
                else c.TextureClipping =
                        new Rectangle(0, 0.354f, 0.625f, 0.312f);
                c.Location = MathHelper.CreateTransformation(
                    0.184f, 0.332f, 0, 0.125f, 0.125f, 1);
                c.Draw();

                if (playerCurrentlyShooting)
                    c.TextureClipping =
                        new Rectangle(0, 0.0208f, 0.625f, 0.312f);
                else c.TextureClipping =
                        new Rectangle(0, 0.354f, 0.625f, 0.312f);
                c.Location = MathHelper.CreateTransformation(
                    0.606f, 0.332f, 0, 0.125f, 0.125f, 1);
                c.Draw();
            }
        }

        protected override void Update(TimeSpan delta)
        {
            //Just like in the Redraw method above, this one-liner prevents
            //any update logic from happening before all resources are loaded.
            if (Resources.LoadingTasksPending > 0) return;

            if (currentScreen == ControlScreen.Control)
            {
                //The value of a control mapping should always be greater than
                //or equal to 0. It's regular maximum is at 1.0 - higher values
                //are possible too (like for the mouse speed).
                //Its current value can be accessed via the "Value" property 
                //of a control mapping.
                currentPlayerMove = new Vector2(
                    playerMoveRight.Value - playerMoveLeft.Value,
                    playerMoveForward.Value - playerMoveBackward.Value);

                //To shorten things a bit, the control mapping can also be used
                //whereever a float value is expected - and will be implicitely
                //converted into its current value.
                currentPlayerLook = new Vector2(Math.Max(-1, Math.Min(1, 
                    playerLookRight - playerLookLeft)), Math.Max(-1, 
                    Math.Min(1, playerLookUp - playerLookDown)));

                //If the current value of an input is greater than the
                //activation treshold (defined as constant "ActivationTreshold"
                //in the ControlMapping class), the value is considered to be
                //"active". This can be used to determine quickly wheter a 
                //button or key is currently pressed down or if an analog pad
                //or trigger is pushed in "enough" so that the element can
                //be considered as active.
                playerCurrentlyShooting = playerShoot.IsActive;

                //When a control element is activated by for example pressing 
                //a button, so that the "IsActive" property of an control 
                //element changes from "false" to "true", the "IsActivated"
                //property can be used to retrieve this state change.
                //This value can, for example, be used to react a key press 
                //to navigate in a menu, as the value will only be "true" once 
                //per changing the state from "Inactive" to "Active" in an 
                //update cycle.
                if (playerJump.IsActivated) lastPlayerJump = DateTime.Now;

                if (menuBack.IsActivated)
                {
                    currentScreen = ControlScreen.Main;
                    Controls.Input.SetMouse(MouseMode.VisibleFree);
                }
            }
            else if (currentScreen == ControlScreen.Main)
            {
                if (menuUp.IsActivated)
                {
                    mainScreenSelection = (mainScreenSelection - 1) % 3;
                    if (mainScreenSelection < 0)
                        mainScreenSelection = 3 + mainScreenSelection;
                }
                else if (menuDown.IsActivated)
                    mainScreenSelection = (mainScreenSelection + 1) % 3;
                if (menuBack.IsActivated)
                {
                    if (mainScreenSelection == (int)ControlScreen.Leave - 1)
                        currentScreen = ControlScreen.Leave;
                    else mainScreenSelection = (int)ControlScreen.Leave - 1;
                }

                if (menuAccept.IsActivated)
                {
                    currentScreen = (ControlScreen)(mainScreenSelection + 1);
                    if (currentScreen == ControlScreen.Control)
                        Controls.Input.SetMouse(MouseMode.InvisibleFixed);
                }
            }
            else if (currentScreen == ControlScreen.Evaluate)
            {
                if (menuAccept.IsActivated || menuBack.IsActivated)
                    currentScreen = ControlScreen.Main;
            }
            else if (currentScreen == ControlScreen.Leave)
            {
                if (menuAccept.IsActivated || menuBack.IsActivated) Close();
            }
        }

        protected override void Unload()
        {
            controlIcons?.Dispose();
            controlScreenControl?.Dispose();
            controlScreenEvaluate?.Dispose();
            controlScreenLeave?.Dispose();
            controlScreenMain?.Dispose();
            plane?.Dispose();
        }
    }
}
