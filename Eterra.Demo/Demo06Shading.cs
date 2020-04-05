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
    /// Provides a demonstration on how to render things in 3D with the more 
    /// feature-rich <see cref="IRenderContextPhong"/> and how to use a 
    /// <see cref="RenderTextureBuffer"/> to render a GUI over the 3D content.
    /// </summary>
    class Demo06Shading : EterraApplicationBase
    {
        //A new challenger approaches: C U B E!
        private MeshBuffer plane, cube;

        //As this example introduces phong shading, we will render our new cube
        //with not just a plain color or texture (which is called "Diffuse Map"
        //in phong shading), but also a specular texture (or "specular map")
        //and an emission map.
        //The specular map specifies where the model will reflect light and 
        //where not. The brighter the white in the specular map, the more it 
        //will reflect in that area.
        //Unlike the diffuse map ("main texture"), which would appear pitch
        //black if there wasn't a light illuminating it, the emission map will
        //be added on top of the diffuse map and, whereever it appears on the
        //mesh and indepenendly of the lighting conditions, it will always 
        //shine bright - inspirational, isn't it? 
        private TextureBuffer diffuseMap, specularMap, emissionMap, 
            splashScreen, splashScreenText;

        //As this demo both renders a 3D scene (background) and a 2D "GUI"
        //on top of it (foreground), two render parameter instances are 
        //required.
        private readonly RenderParameters backgroundRenderParameters,
            foregroundRenderParameters;

        //Render texture buffers were already introduced in a previous demo -
        //in this example, however, we'll use them as render target for the 
        //3D scene in every redraw. After redrawing the 3D scene to this 
        //texture, we'll draw that to the screen using our good old plane -
        //with the rest of the GUI, in the correct order.
        private RenderTextureBuffer backgroundRenderTarget;

        private ControlMapping enter;

        private float counter = 0;
        private bool enterPressed = false;       

        public Demo06Shading()
        {
            backgroundRenderParameters = new RenderParameters();

            backgroundRenderParameters.Camera.MoveTo(0, 0, -3.5f);

            //The IRenderContextPhong supports lighting calculations. 
            //The lights, which should illuminate everything rendered with 
            //those render parameters/render context, need to be added to the
            //"Lighting" collection of the render parameters and, additionally
            //to that, the lighting needs to be enabled.
            //Enabling the lighting without adding any lights will most likely
            //just give you a pitch black screen (unless you use emission maps
            //on a model on the screen somewhere).
            backgroundRenderParameters.Lighting.Enabled = true;
            backgroundRenderParameters.Lighting.Add(
                new Light(new Color(5, 5, 5), -Vector3.UnitY));
            backgroundRenderParameters.Lighting.Add(
                new Light(new Color(255, 255, 255), 
                backgroundRenderParameters.Camera.Position, 20, Vector3.UnitZ, 
                Angle.Deg(30), Angle.Deg(2)));

            //Our GUI is rendered in an orthographic projection mode.
            //While it is possible to render the GUI and the 3D content in one
            //render pass and with a perspective projection mode, the 
            //positioning and scaling of the GUI elements isn't that easy.
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

            //The size of our render target for the 3D content should be as
            //big as the screen or window. As this can change, the render 
            //target needs to be always reinitialized when the window size
            //changes.
            Graphics.Resized += (s, e) => InitializeBackgroundRenderTarget();
            InitializeBackgroundRenderTarget();
        }

        private void InitializeBackgroundRenderTarget()
        {
            //If the render target texture has already been initialized before,
            //it needs to be disposed before creating and assigning a new
            //instance with the new window or screen size.
            if (backgroundRenderTarget != null && 
                !backgroundRenderTarget.IsDisposed) 
                backgroundRenderTarget.Dispose();
            backgroundRenderTarget = Graphics.CreateRenderBuffer(Graphics.Size, 
                TextureFilter.Linear);
        }

        protected override void Redraw(TimeSpan delta)
        {
            //As mentioned before, we'll first render our 3D scene to the
            //"backgroundRenderTarget" render texture buffer and then render
            //our foreground with that texture.
            Graphics.Render<IRenderContextPhong>(backgroundRenderParameters, 
                RedrawBackground, backgroundRenderTarget);
            Graphics.Render<IRenderContext>(foregroundRenderParameters,
                RedrawForeground);
        }

        private void RedrawForeground(IRenderContext c)
        {
            c.Mesh = plane;

            //To scale our drawing plane so that it always occupies the entire
            //screen or window space in a "OrthgraphicRelativeProportional"
            //projection, the default 1x1x1 scale won't do - the X and Y scale
            //need to be calculated using the current graphics size ratio.
            c.Location = MathHelper.CreateTransformation(0.5f, 0.5f, 1, 
                Math.Max(1, (float)Graphics.Size.Width / Graphics.Size.Height),
                Math.Max(1, (float)Graphics.Size.Height / Graphics.Size.Width),
                1);
            c.Texture = backgroundRenderTarget;

            c.Draw();

            //The GUI consists of a few textures which are simply drawn on the
            //screen using some magic numbers that look good in the result.
            c.Location = MathHelper.CreateTransformation(0.5f, 0.75f, 0.5f, 
                1, 0.45f, 1);
            c.Texture = splashScreen;

            c.Draw();

            c.Location = MathHelper.CreateTransformation(0.5f, 0.1f, 0.5f,
                0.45f, 0.05f, 1);
            c.Texture = splashScreenText;

            //The "text texture" contains two text rows - the first one is 
            //initially displayed, the second one after hitting enter.
            c.TextureClipping = new Rectangle(0, enterPressed ? 0 : 0.5f, 
                1, 0.5f);

            c.Draw();
        }

        private void RedrawBackground(IRenderContextPhong c)
        {
            //Behold: a highly sophisticated implementation to draw a 
            //2-dimensional array of oppositely rotating boxes!
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

                    //Using diffuse, specular and emission maps is quite 
                    //intuitive - with the IRenderContextPhong interface, 
                    //there are additional properties that the texture buffers
                    //with these maps can just be assigned to (or they can be
                    //left at "null").
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
