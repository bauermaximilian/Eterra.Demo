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
using Eterra.Sound;
using System;
using System.Numerics;

namespace Eterra.Demo
{
    /// <summary>
    /// Provides a demonstration of how to load and use resources. This 
    /// example shows how to load images, play music and how to animate and 
    /// render text using a <see cref="RenderTextureBuffer"/>.
    /// </summary>
    class Demo03Resources : EterraApplicationBase
    {
        private readonly AnimationPlayer titleAnimationPlayer;
        private readonly AnimationPlayer scrollTextAnimationPlayer;

        private readonly IAnimationLayer<Vector3> titleAnimationPosition,
            scrollTextAnimationPosition;
        private readonly IAnimationLayer<float> titleAnimationOpacity,
            scrollTextAnimationOpacity;

        //For this example, two different RenderParameters instances are used -
        //one for rendering the main application and one for rendering the
        //text.
        private readonly RenderParameters renderParameters, 
            textRenderParameters; 

        //Just like in the previous two demos, we'll use a plane mesh for 
        //performing our drawing operations here.
        private MeshBuffer plane;
        //As this demo also uses images (so-called textures in the 3D context),
        //we'll define the fields for them here as well. Just like meshes,
        //textures need to be created programatically or loaded from an image
        //file to create a TextureData instance - and after this TextureData 
        //instance was loaded (or buffered) onto the GPU, we'll have a
        //TextureBuffer instance. These texture buffers can then be used for
        //drawing - but they can't be drawn by itself, but will always be 
        //used in combination with a mesh. In the simple cases, the texture
        //will just be "drawn onto" a mesh - like in this case when we're just
        //drawing a plane with a texture onto it. The result is similar to the
        //previous examples - a rectangle is drawn. But this time not with a 
        //plain color, but with an image inside. Here, it's both the background
        //and a logo. The latter is semi-transparent - so the background will 
        //be visible through the transparent parts of the logo.
        private TextureBuffer backgroundTexture, splashTexture;

        //Text rendering works quite similar - but there are pretty good online
        //resources to explain how sprite fonts work, so here's just the 
        //summary: A char set of letters - depending on the kind of letters you
        //need - is taken and put onto one single texture. To render text, the
        //framework then takes the text you want to render, goes through every
        //character in your text, takes the section from the previously created
        //"character texture" which contains that letter, and then draws it to
        //a small plane onto the screen. This is done for every letter in your
        //text. Understandably, this operation is pretty "expensive" - so, how
        //can the costs be kept low?
        private SpriteFont font;
        
        //By using special texture buffers called "RenderTextureBuffers". These
        //textures can be used like normal textures - with one additional 
        //feature: you can render things to these textures. That might be a 
        //bit confusing at first - but just imagine you'd render something,
        //like text or other things, then taking that image and saving it
        //to a image file (like a screenshot), and then loading it as a texture
        //again and use that as a texture to draw a plane. Like that - just a 
        //lot more efficient! And in this example, we'll just render our text
        //to the render texture buffer below once - and then use that texture
        //buffer for all subsequent drawing operations. Magic!
        private RenderTextureBuffer scrollText;

        //Often overlooked, but important for a great multimedial experience is
        //not only something for your eyes, but also for your... ears. Music!
        //It works a bit different to the buffers in the graphics context, 
        //hence the different name: unless it's a pretty short audio clip, it
        //will only be pre-loaded (buffered) partly and can be played back or
        //controlled directly using the instance - no additonal contexts or 
        //alike required. To change the properties of the "listener" - as in 
        //the "player", like its position, the "Sound" property of an
        //EterraApplicationBase can be used.
        private SoundSource music;

        private bool animationStarted = false;
        private bool textRendered = false;

        public Demo03Resources()
        {
            //Just like in the previous example, we'll initialize a few 
            //animation players with some magic numbers (which I just defined
            //arbitrarily so that the animation looks good).
            titleAnimationPlayer = new AnimationPlayer(new Timeline(
                new TimelineLayer<Vector3>(TimelineLayer.IdentifierPosition,
                InterpolationMethod.Linear,
                new Keyframe<Vector3>[]
                {
                    Keyframe.Create(0, 0, 0, Camera.DefaultPosition.Z),
                    Keyframe.Create(60, 0, 0, 21)
                }),
                new TimelineLayer<float>(TimelineLayer.IdentifierOpacity,
                InterpolationMethod.Linear,
                new Keyframe<float>[]
                {
                    Keyframe.Create(0, 1),
                    Keyframe.Create(7, 1),
                    Keyframe.Create(9, 0)
                })));
            titleAnimationPosition = titleAnimationPlayer.GetLayer<Vector3>(
                TimelineLayer.IdentifierPosition);
            titleAnimationOpacity = titleAnimationPlayer.GetLayer<float>(
                TimelineLayer.IdentifierOpacity);

            scrollTextAnimationPlayer = new AnimationPlayer(new Timeline(
                new TimelineLayer<Vector3>(TimelineLayer.IdentifierPosition,
                InterpolationMethod.Linear,
                new Keyframe<Vector3>[]
                {
                    Keyframe.Create(4.3f, 0, -1, -10.5f),
                    Keyframe.Create(66, 0, -1, 10.5f)
                }),
                new TimelineLayer<float>(TimelineLayer.IdentifierOpacity,
                InterpolationMethod.Linear,
                new Keyframe<float>[]
                {
                    Keyframe.Create(0, 1),
                    Keyframe.Create(48, 1),
                    Keyframe.Create(54, 0)
                })));
            scrollTextAnimationPosition = scrollTextAnimationPlayer
                .GetLayer<Vector3>(TimelineLayer.IdentifierPosition);
            scrollTextAnimationOpacity = scrollTextAnimationPlayer
                .GetLayer<float>(TimelineLayer.IdentifierOpacity);

            //As mentioned above, we'll need two different render parameters
            //for this example - one for rendering the main application and one
            //for rendering the text. The first uses the default perspective
            //projection, the latter uses an orthographic projection.
            renderParameters = new RenderParameters();
            textRenderParameters = new RenderParameters();
            textRenderParameters.Camera.ProjectionMode =
                ProjectionMode.OrthographicRelative;
        }

        protected override void Load()
        {
            //As mentioned above, when loading a system font, that font needs
            //to be turned into a texture - the parameters for that conversion
            //are defined below.
            FontRasterizationParameters fontRasterizationParameters =
                new FontRasterizationParameters()
                {
                    //Depending on how many characters you want to support
                    //and how big the text needs to be rendered while still
                    //looking good, setting the following parameters could be
                    //problematic: if the size is too big or the charset 
                    //contains too many characters, loading the font might 
                    //fail. If that happens - try to use a smaller font size
                    //or use multiple font instances for different charsets.
                    SizePx = 64,
                    CharSet = CharSet.DefaultASCII,
                    //These are just design decisions with no real impact on
                    //performance.
                    Bold = true,  
                    //If you want to get a more pixelated look, set that 
                    //parameter to false. But we want our text to be smooth!
                    UseAntiAliasing = true
                };

            //You already know how to load a plane from mesh data...
            Resources.LoadMesh(MeshData.Plane)
                .AddFinalizer(r => plane = r);
            //...and loading resources textures from disk works quite similar, 
            //just with a "ResourcePath" instance (or a string, which can be
            //implicitely converted to an instance of the "ResourcePath" type).
            //One important thing to consider is that resource paths used to
            //load resources must always be absolute - and absolute paths start
            //with the root path element '/' (a single slash). Without that, 
            //the path is relative and can't be used for loading resources.
            //If the path is valid, absolute and referring to a file (which 
            //means it doesn't end with a '/'), it's then handed over to the 
            //FileSystem used when loading the application (with the "Load" 
            //method) - depending on the used file system, the requested 
            //resource will either reside in the default "data" folder in the 
            //application directory (by default, if no other file system is 
            //provided) or somewhere entirely different (like an game resource 
            //archive file, the internet, etc.) - but for more information on 
            //how the resource pipeline works, see the documentation of the 
            //FileSystem and ResourcePath classes. 
            //Long story short: don't forget the initial slash in the path.
            Resources.LoadTexture("/frameWorksTitle.png")
                .AddFinalizer(r => splashTexture = r);
            Resources.LoadTexture("/frameWorksBackground.png")
                .AddFinalizer(r => backgroundTexture = r);

            //Loading sounds works the same, just that the returned data object
            //instance works differently, like described above - this can have
            //an effect on the access to the referenced file, which may stay
            //open until the SoundSource is disposed.
            Resources.LoadSound("/frameWorksTheme.mp3")
                .AddFinalizer(r => music = r);

            //Depending on how a font is loaded - the choices are loading a 
            //sprite font from a sprite font format (like BMF) or generating 
            //the sprite font data on the fly by loading a generic (system) 
            //font file - additional parameters for the rasterization of the
            //font file need to be provided (and carefully chosen to avoid
            //other problems).
            Resources.LoadGenericFont("Arial", fontRasterizationParameters)
                .AddFinalizer(r => font = r);

            //If something during loading goes wrong - for example if the 
            //format of the resource is not supported, the file doesn't exist
            //or the importer fails because of other mysterious reasons, the
            //error is - by default - written to the "Log" as error. This can
            //be turned off or overridden with some other, custom behaviour -
            //by using the last, optional parameter of every "Load..." method.

            //The render buffer for our font needs to be initialized - it would
            //be better to make the decision of its size dependant on the 
            //size of the users' screen, but optimisations like that are beyond
            //the scope of this example.
            scrollText = Graphics.CreateRenderBuffer(new Size(2048, 2048),
                TextureFilter.Linear);

            //This demo contains a lot of text and should look impressive to
            //the viewer - so we'll maximize the window to get all of his/her
            //attention! Not fullscreen though, because without the exit button
            //of the window, there's no escape - that'll come in the next 
            //example (oops, spoiler alert).
            Graphics.Mode = WindowMode.Maximized;
        }

        protected override void Redraw(TimeSpan delta)
        {
            //As already mentioned, the first thing which will be drawn is the
            //text. To save CPU time, the text is only rendered once - and to 
            //a previously initialized render texture buffer. This is done as
            //soon as the font was loaded completely (and is no longer null).
            if (!textRendered && font != null)
            {
                SpriteTextFormat format = new SpriteTextFormat()
                {
                    //The type size is not in pixels, but in world units - to
                    //fit everything on the screen, we're gonna calculate the
                    //type size using the amount of available space (1.0) and
                    //dividing it by the amount of lines in the text.
                    TypeSize = 1.0f / ScrollText.Split('\n').Length,
                    //The text should be centered (horizontally). Vertical
                    //alignment is also supported by just changing the 
                    //"VerticalAlignment" parameter, for example to "Middle".
                    //Take that, CSS. IT COULD BE THAT SIMPLE.
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                //With the parameters created before, a layout of letters ready
                //for drawing is created at a fixed position.
                SpriteText text = font.CreateText(Vector3.UnitX * 0.5f, 
                    ScrollText, format);

                //That can then be drawn directly to a IGraphicsContext.
                //It involves multiple drawing operations for each character 
                //and performs them automatically - that's the reason why 
                //drawing text works a bit differently.
                Graphics.Render<IRenderContext>(textRenderParameters,
                    c => text.Draw(c), scrollText);

                textRendered = true;
            }

            //The drawing will only start after the animation was started in
            //the update method - which will happen after the text was rendered
            //and all resources were loaded.
            if (animationStarted)
            {
                Graphics.Render<IRenderContext>(renderParameters, 
                    RenderCanvas);
            }
        }

        private void RenderCanvas(IRenderContext c)
        {
            //Drawing that demo works quite similar to the previous demos.
            c.Mesh = plane;

            c.Texture = backgroundTexture;
            c.Location = MathHelper.CreateTransformation(
                0, 0, 16, 30 * Graphics.Size.Ratio, 30, 1);
            c.Draw();

            c.Texture = splashTexture;
            //Introducing: Opacity! A value between 0.0 (invisible) and 1.0
            //(completely visible, default), which you can use to fade things
            //in and out - or render them semi-transparent, like... ghosts.
            c.Opacity = titleAnimationOpacity.CurrentValue;
            c.Location = MathHelper.CreateTransformation(
                titleAnimationPosition.CurrentValue, new Vector3(2, 1, 1));
            c.Draw();

            c.Texture = scrollText;
            c.Opacity = scrollTextAnimationOpacity.CurrentValue;
            c.Location = MathHelper.CreateTransformation(
                scrollTextAnimationPosition.CurrentValue, 
                new Vector3(8, 11, 1), 
                Quaternion.CreateFromAxisAngle(Vector3.UnitX, Angle.Deg(90)));
            c.Draw();
        }

        protected override void Update(TimeSpan delta)
        {
            if (!animationStarted)
            {
                //We're waiting until all resources were loaded to start 
                //everything in sync: light, camera, action!
                if (plane != null && font != null && backgroundTexture != null
                    && splashTexture != null && music != null && textRendered)
                {
                    titleAnimationPlayer.Play();
                    scrollTextAnimationPlayer.Play();
                    music.Play(false);

                    animationStarted = true;
                }
            }
            else
            {
                titleAnimationPlayer.Update(delta);
                scrollTextAnimationPlayer.Update(delta);

                //The window should close automatically when the animation is
                //over, just in case the viewer fainted from too much 
                //epic-ness and can no longer do this by himself.
                //Yes, very likely.
                if (scrollTextAnimationPlayer.Position ==
                    scrollTextAnimationPlayer.PlaybackEnd)
                    Close();
            }
        }

        protected override void Unload()
        {
            plane?.Dispose();
            font?.Dispose();
            backgroundTexture?.Dispose();
            splashTexture?.Dispose();
            scrollText?.Dispose();
            music?.Dispose();
        }

        private const string ScrollText =
@"FRAME WORKS
EPISODE MIMIMI
FUCKING FINALLY

There is unrest in the 
programmers apartment. 
Several thousand lines of 
code have been documented 
and considered as functional 
enough for a public repo - 
all that's left to do is 
GIT PUSH.

This time killer of a project
has made it difficult for 
its creator to maintain his 
sanity. Not that someone 
forced him to write a fucking 
multimedia library all by 
himself... but hey, #yolo.

As he hits the ENTER key,
he feels a warm sensation 
flowing through his body.
He finally did it.
It's over. He's free.

But little did he know 
that the real fun hasn't
even started...


Just kidding. He knows.

Ugh.


Oh yeah, and sorry for not 
using the real intro music, 
but I think that track is 
pretty epic too... and free.
Praise the gods for CCO!
";
    }
}
