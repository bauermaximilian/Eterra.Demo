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
    /// Provides a demonstration of how to create and play different kinds
    /// of simple animations on the example of the prev.
    /// </summary>
    class Demo02Animation : EterraApplicationBase
    {
        //Just like in the previous demo, we're gonna use a simple plane mesh
        //for drawing and will initialize a RenderParameters instance here.
        private MeshBuffer plane;
        private readonly RenderParameters renderParameters =
            new RenderParameters();

        /// <summary>
        /// Defines the colors which are used for the planes, that are rendered
        /// to the screen.
        /// </summary>
        private readonly Color primaryColor = Color.Red,
            secondaryColor = Color.Blue, tertiaryColor = new Color(20, 20, 20);

        /// <summary>
        /// Defines the scale which is used for every rendered plane.
        /// </summary>
        private readonly Vector3 primaryScale = new Vector3(0.25f, 0.25f, 1),
            secondaryScale = new Vector3(0.5f, 0.5f, 1),
            tertiaryScale = new Vector3(1, 1, 1);

        /// <summary>
        /// Defines the position of the third plane, which rotates in 
        /// the background.
        /// </summary>
        private readonly Vector3 tertiaryPosition
            = new Vector3(0.5f, 0.5f, 2);

        /// <summary>
        /// The animation players, which contain a <see cref="Timeline"/> with
        /// keyframes, that transform each rectangle.
        /// </summary>
        private readonly AnimationPlayer primaryAnimation, secondaryAnimation,
            tertiaryAnimation;

        /// <summary>
        /// The animation player layers taken from the 
        /// <see cref="primaryAnimation"/> and <see cref="secondaryAnimation"/>
        /// instances. These expose access to the position transformation of
        /// the primary and secondary rectangle.
        /// </summary>
        private readonly IAnimationLayer<Vector3> primaryAnimationPosition,
            secondaryAnimationPosition;

        /// <summary>
        /// The animation player layer taken from the 
        /// <see cref="tertiaryAnimation"/>. This instance exposes access to
        /// the rotation transformation of the tertiary rectangle.
        /// </summary>
        private readonly IAnimationLayer<Quaternion> tertiaryAnimationRotation;

        public Demo02Animation()
        {
            //To animate objects, an animation player is required. That 
            //animation player needs a timeline with all keyframes that define
            //the animation. The timeline consists of multiple layers, where
            //one layer can define the keyframes for the position animation
            //with the type "Vector3", another layer the scale, another layer
            //the rotation with the type "Quaternion" and so on. Each layer
            //has an identifier, which can not only be used to identify the
            //timeline layer within the timeline, but also to access the 
            //animated value of that layer later.

            //The first rendered plane has a height/width of 0.5 and will move 
            //around the center of the canvas clockwise while being completely 
            //visible all the time. The movement is interpolated using a cubic 
            //interpolation.
            primaryAnimation = new AnimationPlayer(new Timeline(
                new TimelineLayer<Vector3>(TimelineLayer.IdentifierPosition,
                InterpolationMethod.Cubic,
                new Keyframe<Vector3>[]
                {
                    //The first and last frame aren't part of the played 
                    //animation, it's just to create a smooth arc between the 
                    //end and the start of the animation.
                    new Keyframe<Vector3>(-1, new Vector3(0.75f, 0.25f, 0)),
                    new Keyframe<Vector3>(0, new Vector3(0.25f, 0.25f, 0)),
                    new Keyframe<Vector3>(1, new Vector3(0.25f, 0.75f, 0)),
                    new Keyframe<Vector3>(2, new Vector3(0.75f, 0.75f, 0)),
                    new Keyframe<Vector3>(3, new Vector3(0.75f, 0.25f, 0)),
                    new Keyframe<Vector3>(4, new Vector3(0.25f, 0.25f, 0)),
                    new Keyframe<Vector3>(5, new Vector3(0.25f, 0.75f, 0))
                })))
            {
                PlaybackStart = TimeSpan.Zero,
                PlaybackEnd = TimeSpan.FromSeconds(4),
                PlaybackLoop = true
            };

            //To access the current animated value of a timeline layer later, 
            //the identifier previously used to create the timeline layer is 
            //now used to retrieve the animation player layer - using that
            //value (which is stored in a field), the current animated value
            //can later be retrieved efficiently.
            primaryAnimationPosition = primaryAnimation.GetLayer<Vector3>(
                TimelineLayer.IdentifierPosition);

            //The second plane, which will be rendered "behind" the first red 
            //plane in blue (klance is cannon king), will move counterclockwise
            //at the "0, 0, 1, 1" borders of the canvas (which are the 
            //window/screen borders only if the windows' aspect ratio is 1:1).
            secondaryAnimation = new AnimationPlayer(new Timeline(
                new TimelineLayer<Vector3>(TimelineLayer.IdentifierPosition,
                InterpolationMethod.Linear,
                new Keyframe<Vector3>[]
                {
                    new Keyframe<Vector3>(0, new Vector3(0.25f, 0.25f, 1)),
                    new Keyframe<Vector3>(1, new Vector3(0.25f, 0.75f, 1)),
                    new Keyframe<Vector3>(2, new Vector3(0.75f, 0.75f, 1)),
                    new Keyframe<Vector3>(3, new Vector3(0.75f, 0.25f, 1)),
                    new Keyframe<Vector3>(4, new Vector3(0.25f, 0.25f, 1)),
                })))
            {
                PlaybackLoop = true
            };
            secondaryAnimationPosition = secondaryAnimation.GetLayer<Vector3>(
                TimelineLayer.IdentifierPosition);

            //The final third plane, which is covered by the two other planes 
            //and rendered behind those two, will rotate clockwise around the
            //center of the canvas (because it's pivot is in the center of the
            //mesh and the position will be set to (0.5|0.5). 
            //Its color is a dark gray.
            tertiaryAnimation = new AnimationPlayer(new Timeline(
                new TimelineLayer<Quaternion>(TimelineLayer.IdentifierRotation,
                InterpolationMethod.Linear,
                new Keyframe<Quaternion>[]
                {
                    new Keyframe<Quaternion>(0,
                    Quaternion.CreateFromYawPitchRoll(0, 0, 0)),
                    new Keyframe<Quaternion>(1,
                    Quaternion.CreateFromYawPitchRoll(0, 0, Angle.Deg(-90))),
                    new Keyframe<Quaternion>(2,
                    Quaternion.CreateFromYawPitchRoll(0, 0, Angle.Deg(-180))),
                    new Keyframe<Quaternion>(3,
                    Quaternion.CreateFromYawPitchRoll(0, 0, Angle.Deg(-270))),
                    new Keyframe<Quaternion>(4,
                    Quaternion.CreateFromYawPitchRoll(0, 0, Angle.Deg(-360)))
                })))
            {
                PlaybackLoop = true
            };
            tertiaryAnimationRotation = tertiaryAnimation.GetLayer<Quaternion>(
                TimelineLayer.IdentifierRotation);
        }

        protected override void Load()
        {
            //Loads the plane mesh from the planeMesh field into the 
            //planeMeshBuffer field, which is used to render the three 
            //different planes in this demo.
            //In this example, the "task finalizer" mechanism is used instead
            //of the event. The advantage: it's shorter!
            //Just like Javascript callbacks (or the task events), these 
            //delegates get invoked after the sync task was completed.
            //The first callback is used when the task was finished 
            //successfully, the second one (optionally) when the task failed. 
            //If the latter callback is omitted, the error will be logged.
            Resources.LoadMesh(MeshData.Plane).AddFinalizer(
                r => plane = r);

            //Start the playback for all animations here already, as the 
            //playback doesn't have to "wait" until any big resources have
            //been loaded.
            primaryAnimation.Play();
            secondaryAnimation.Play();
            tertiaryAnimation.Play();

            //This projection mode gives us "perfect squares". The difference
            //between "OrthographicRelative" is most visible in this demo -
            //try to change it and see what happens!
            renderParameters.Camera.ProjectionMode = 
                ProjectionMode.OrthgraphicRelativeProportional;
        }

        protected override void Redraw(TimeSpan delta)
        {
            //In this demo, the actual canvas drawing functionality has been 
            //outsourced to another method. Just keep in mind that the provided
            //ICanvas instance gets created "fresh" by Graphics.RenderCanvas
            //and is just valid during the execution of the specified
            //Redraw method. After that, it automatically gets destroyed.
            //So... don't even think about keeping that ICanvas instance
            //and attempt to do something with it later. 
            //In the best case, it won't work. In the worst case, it might
            //kill your pet hamster Heinrich.
            Graphics.Render<IRenderContext>(renderParameters, Redraw);
        }

        private void Redraw(IRenderContext canvas)
        {
            //The drawing logic is quite similar to the first example.

            canvas.Mesh = plane;

            //The transformation is calculated using the current animated value
            //from the animation player layers and constant values.
            canvas.Location = MathHelper.CreateTransformation(
                primaryAnimationPosition.CurrentValue,
                primaryScale);
            canvas.Color = primaryColor;
            canvas.Draw();

            canvas.Location = MathHelper.CreateTransformation(
                secondaryAnimationPosition.CurrentValue,
                secondaryScale);
            canvas.Color = secondaryColor;
            canvas.Draw();

            canvas.Location = MathHelper.CreateTransformation(
                tertiaryPosition, tertiaryScale,
                tertiaryAnimationRotation.CurrentValue);
            canvas.Color = tertiaryColor;
            canvas.Draw();
        }

        protected override void Update(TimeSpan delta)
        {
            //The animation players need to be updated regularily with the
            //amount of time the animation should be progressed - this is
            //required so the animation will actually "play". That time can 
            //just be the value of the "delta" parameter of this method.
            //Updating the animation updating into the "Redraw" method would
            //work too and might even be better in some cases - 
            //but in simple applications like that, it doesn't really matter.
            primaryAnimation.Update(delta);
            secondaryAnimation.Update(delta);
            tertiaryAnimation.Update(delta);
        }

        protected override void Unload()
        {
            plane?.Dispose();
        }
    }
}
