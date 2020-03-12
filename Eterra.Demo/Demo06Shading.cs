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
    internal class Demo06Shading : EterraApplicationBase
    {
        private MeshBuffer plane, cube;

        private TextureBuffer diffuseMap, specularMap, emissionMap, 
            splashScreen, splashScreenText;

        private readonly RenderParameters backgroundRenderParameters,
            foregroundRenderParameters;

        private RenderTextureBuffer backgroundRenderTarget;

        private ControlMapping enter;

        private float counter = 0;
        private bool enterPressed = false;       

        public Demo06Shading()
        {
            backgroundRenderParameters = new RenderParameters();

            backgroundRenderParameters.Camera.MoveTo(0, 0, -3.5f);

            backgroundRenderParameters.Lighting.Enabled = true;
            backgroundRenderParameters.Lighting.Add(
                new Light(new Color(5, 5, 5), -Vector3.UnitY));
            backgroundRenderParameters.Lighting.Add(
                new Light(new Color(255, 255, 255), 
                backgroundRenderParameters.Camera.Position, 20, Vector3.UnitZ, 
                Angle.Deg(30), Angle.Deg(2)));

            foregroundRenderParameters = new RenderParameters();
            foregroundRenderParameters.Camera.ProjectionMode =
                ProjectionMode.OrthgraphicRelativeProportional;
        }

        protected override void Load()
        {
            enter = Controls.Map(KeyboardKey.Enter);

            Resources.LoadMesh(MeshData.Plane).AddFinalizer(
                r => plane = r);
            Resources.LoadMesh(MeshData.Box).AddFinalizer(
                r => cube = r);

            Resources.LoadTexture("/boxSplash.png").AddFinalizer(
                r => splashScreen = r);
            Resources.LoadTexture("/boxSplashText.png").AddFinalizer(
                r => splashScreenText = r);
            Resources.LoadTexture("/boxDiffuse.png").AddFinalizer(
                r => diffuseMap = r);
            Resources.LoadTexture("/boxSpecular.png").AddFinalizer(
                r => specularMap = r);
            Resources.LoadTexture("/boxEmission.png").AddFinalizer(
                r => emissionMap = r);

            Graphics.Resized += (s, e) => InitializeBackgroundRenderTarget();
            InitializeBackgroundRenderTarget();
        }

        private void InitializeBackgroundRenderTarget()
        {
            if (backgroundRenderTarget != null && 
                !backgroundRenderTarget.IsDisposed) 
                backgroundRenderTarget.Dispose();
            backgroundRenderTarget = Graphics.CreateRenderBuffer(Graphics.Size, 
                TextureFilter.Linear);
        }

        protected override void Redraw(TimeSpan delta)
        {
            Graphics.Render<IRenderContextPhong>(backgroundRenderParameters, 
                RedrawBackground, backgroundRenderTarget);
            Graphics.Render<IRenderContext>(foregroundRenderParameters,
                RedrawForeground);
        }

        private void RedrawForeground(IRenderContext c)
        {
            c.Mesh = plane;

            c.Location = MathHelper.CreateTransformation(0.5f, 0.5f, 1, 
                Math.Max(1, (float)Graphics.Size.Width / Graphics.Size.Height),
                Math.Max(1, (float)Graphics.Size.Height / Graphics.Size.Width),
                1);
            c.Texture = backgroundRenderTarget;

            c.Draw();

            c.Location = MathHelper.CreateTransformation(0.5f, 0.75f, 0.5f, 
                1, 0.45f, 1);
            c.Texture = splashScreen;

            c.Draw();

            c.Location = MathHelper.CreateTransformation(0.5f, 0.1f, 0.5f,
                0.45f, 0.05f, 1);
            c.Texture = splashScreenText;
            c.TextureClipping = new Rectangle(0, enterPressed ? 0 : 0.5f, 
                1, 0.5f);

            c.Draw();
        }

        private void RedrawBackground(IRenderContextPhong c)
        {
            int flipFactor = -1;
            for (float x = -4; x <= 4; x += 2)
            {
                for (float y = -4; y <= 4; y += 2)
                {
                    Vector3 position = new Vector3(x, y, 0);
                    Vector3 scale = Vector3.One;
                    Quaternion rotation = Quaternion.CreateFromAxisAngle(
                        Vector3.UnitY, Angle.PiRad(counter * flipFactor));

                    c.Location =
                        MathHelper.CreateTransformation(position, scale,
                        rotation);

                    c.Mesh = cube;
                    c.Texture = diffuseMap;
                    c.SpecularMap = specularMap;
                    c.EmissionMap = emissionMap;

                    c.Draw();

                    flipFactor *= -1;
                }
            }
        }

        protected override void Update(TimeSpan delta)
        {
            counter = (float)(counter + delta.TotalSeconds / 2) % 2;

            if (enter.IsActivated) enterPressed = true;
        }

        protected override void Unload()
        {
            plane?.Dispose();
            cube?.Dispose();
            splashScreen?.Dispose();
            diffuseMap?.Dispose();
            specularMap?.Dispose();
            emissionMap?.Dispose();
            backgroundRenderTarget?.Dispose();
        }
    }
}
