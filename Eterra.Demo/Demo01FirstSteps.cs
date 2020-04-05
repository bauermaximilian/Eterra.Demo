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
using Eterra.Graphics;
using Eterra.IO;
using System;
using System.Numerics;

namespace Eterra.Demo
{
    /// <summary>
    /// Provides a demonstration of how to do basic 2D drawing operations.
    /// </summary>
    /// <remarks>
    /// Welcome to <see cref="Eterra"/>! This class is the first of many 
    /// examples which will demonstrate how to use the <see cref="Engine"/>.
    /// It's recommended to start at this class and then continue at the
    /// next demo <see cref="Demo02Animation"/>.
    /// </remarks>
    class Demo01FirstSteps : EterraApplicationBase
    {
        //In this example, we will draw... rectangles. And as the framework
        //supports both 2D and 3D (which we will cover later), everything which
        //is drawn to screen is a so-called "polygon mesh". These meshes can
        //either be created programatically or loaded from a file - but no 
        //matter where your mesh came from, you will end up with a MeshData 
        //instance. This MeshData instance, however, can't be drawn 
        //just yet - first, it needs to go from the nice and safe managed C#
        //world into the dark and dangerous depths of your GPU. At the end,
        //you will have... a MeshBuffer. For our example, we just need this 
        //one: it's a simple mesh of a quadratic plane.
        private MeshBuffer plane;

        //To draw things, a render context is used. This render context is 
        //created new on every redraw - but the parameters used to create this
        //context usually is not, so it can already be created and (partly) 
        //initialized in the constructor of your application. Other parameters,
        //like the camera position, can be updated some other time - but more 
        //to that later.
        private readonly RenderParameters renderParameters =
            new RenderParameters();

        /// <summary>
        /// Initializes a new instance of the <see cref="Demo01FirstSteps"/> 
        /// class. To start this application, use 
        /// <see cref="EterraApplicationBase.Run(IPlatformProvider)"/>
        /// (or <see cref="EterraApplicationBase.Run(IPlatformProvider, 
        /// IFileSystem)"/>).
        /// </summary>
        public Demo01FirstSteps()
        {
            //A very important information right at the beginning:
            try { Graphics.Title = "This won't work"; }
            catch (InvalidOperationException)
            {
                Log.Information("Remember: The units of the application " +
                    "(like graphics, controls, etc.) can only be accessed " +
                    "when the application (base) was started and " +
                    "successfully initialized!");
            }
            //Platform-related initialisation tasks (like mapping controls, 
            //changing the current window properties or loading resources)
            //must be done in the Load method - in the constructor, the
            //platform is not yet initialized.

            //The constructor is only for initializing things like timelines,
            //event handlers - or the previously mentioned render parameters.
            //All parameters are initialized with default values already that 
            //should make it as straightforward as possible to just get 
            //something drawn to screen. For this example, we just have to 
            //change the projection mode from "Perspective" to "Orthographic",
            //which is more suitable for drawing 2D things. The difference
            //between the different projection modes is explained in later
            //examples or the documentation of the ProjectionMode enum - but 
            //for now I'll just simplify and summarize things as follows: if 
            //you want to draw 2D things, use "OrthographicRelative",
            //if you want to draw 3D things, use "Perspective".
            renderParameters.Camera.ProjectionMode =
                ProjectionMode.OrthographicRelative;
        }

        protected override void Load()
        {
            //As already mentioned before, meshes need to be buffered to be
            //drawn. This is done by the following two commands -
            //the first method starts the buffering of a pre-defined MeshData 
            //structure that defines a default plane mesh. That method returns
            //a so-called "SyncTask" with the expected result of a MeshBuffer.
            //That sync-task can be used to check the state of the 
            //operation and access its results - either via the events that 
            //the instance exposes, or by using so-called "task finalizers"
            //which we will come to in a later demo.
            SyncTask<MeshBuffer> task = Resources.LoadMesh(MeshData.Plane);
            task.Completed += LoadingTaskCompleted;
        }

        //This event executes as soon as the buffering of the plane mesh is
        //complete - and will either store the MeshBuffer instance into the
        //associated field or write an error message to the log.
        private void LoadingTaskCompleted(object sender,
            SyncTaskCompletedEventArgs<MeshBuffer> e)
        {
            if (e.Success) plane = e.Result;
            else Log.Error("The plane mesh couldn't be buffered.", e.Error);
        }

        /// <summary>
        /// Renders the graphics of the current <see cref="Demo01FirstSteps"/>
        /// instance.
        /// </summary>
        protected override void Redraw(TimeSpan delta)
        {
            //If you want to render things, the "Render" method is used. 
            //Its concept and behaviour is described in detail under the 
            //remarks section of that method, but summarized, it does three
            //things for you:
            //1) Initialize and clear the window, screen or custom render 
            //   target - our drawing surface - with the specified parameters
            //2) Perform the drawing operations in the specified delegate onto
            //   that drawing surface
            //3) Apply effects and post-processing to the drawing surface
            //You can either provide a render method as delegate or define the
            //drawing operations like below. Just... don't keep the render 
            //context instance, because it's created new on every render call.
            Graphics.Render(renderParameters, delegate (IRenderContext context)
            {
                //The render context works a bit like a state machine - 
                //parameters are set, then the "Draw" method is called and
                //it performs a drawing call using the current parameters
                //of that context. In the following lines, it's specified 
                //that the mesh, which should be drawn, should be the
                //plane. The color of the drawn mesh and the position and
                //scale is defined afterwards. And then, the rectangle is 
                //drawn to the drawing surface.

                //If the buffer wasn't loaded yet, the parameter is set to 
                //the default value of the field (null) - which is okay,
                //but then the "Draw" method won't do anything but return
                //'false'. Checking whether all resources are loaded or not
                //before drawing is a possible optimisation we will use in 
                //later examples.
                context.Mesh = plane;

                //The first plane will be dark gray and cover the entire
                //screen.
                context.Color = Color.Gunmetal;
                //For clarification: the pre-defined plane mesh has a 
                //size of 1x1 and its pivot in the middle - the origin of
                //the coordinate system in the "OrthographicRelative"
                //projection mode is in the bottom left though. So the
                //position of the plane needs to be set to 0.5|0.5. As
                //Z position, 3 is used (higher value means further in the
                //distance). The default size of 1x1 is kept and not modified,
                //as the size of the visible area in "OrthographicRelative" is 
                //always 1 unit on the X axis (to the right) and one unit on
                //the .And as the location of what we want to draw is
                //defined provided through 4-dimensional matrix (which contains
                //the position, scale and rotation in one data structure), 
                //we'll create that using the method below and just provide 
                //that resulting matrix to the "Location" parameter. 
                context.Location = Matrix4x4.CreateTranslation(0.5f, 0.5f, 3);

                context.Draw();

                //The next rectangle should be at the top right quarter
                //of the screen and colored blue.
                //As we're drawing another rectangle, we can use the same 
                //mesh again and just have to change the parameters so that
                //the mesh has a different colour and position/scale.
                //To create that new location (or transformation), you could
                //either create the translation and scale matrices manually and
                //multiply them in the right order or - if you don't really
                //know how that works (it's okay, you're still breathtaking <3) 
                //just use the methods in the MathHelper class.
                context.Color = Color.Blue;
                context.Location = MathHelper.CreateTransformation(
                    0.75f, 0.75f, 2, 0.5f, 0.5f, 1);

                context.Draw();

                //The last rectangle should be in the bottom left quarter
                //of the screen and colored red. I like red!
                context.Color = Color.Red;
                context.Location = MathHelper.CreateTransformation(
                    0.25f, 0.25f, 1, 0.5f, 0.5f, 1);

                context.Draw();

                //After this last drawing call, the rendering is completed and 
                //the image will be finalized and shown on the screen. Wohoo!
            });
        }

        protected override void Update(TimeSpan delta)
        {
            //This demo application is so simple that it needs no updating.
            //But don't worry, we'll get there!
        }

        protected override void Unload()
        {
            //Good practice: Dispose all buffers which were created - and to
            //avoid any exceptions if the buffer wasn't properly initialized
            //(yet), the '?' can be used.
            plane?.Dispose();
        }
    }
}