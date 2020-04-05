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
using Eterra.Sound;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Eterra.Demo
{
    /// <summary>
    /// Provides a demonstration on how to use the techinques demonstrated in
    /// the previous demos to create a short walking simulator in 3D.
    /// </summary>
    /// <remarks>
    /// This code needs to be refactored and commented before it's actually a
    /// good example on how to use the framework - right now, it just... works.
    /// </remarks>
    class Demo07Game : EterraApplicationBase
    {
        private class GameEntity : IDisposable
        {
            private const float soundFadeSpeed = 0.5f;

            private bool isVisible;
            private Vector3 position, scale;
            private Quaternion rotation;
            private bool locationChanged = true;
            private Matrix4x4 location = Matrix4x4.Identity;

            private Deformer characterDeformer = null;

            private readonly string collisionAction = null;
            private readonly Color color;
            private readonly ColliderPrimitive collider;

            private readonly DeformerAnimationPlayer deformerAnimationPlayer;
            private readonly AnimationPlayer locationAnimationPlayer;
            private readonly IAnimationLayer<Vector3> 
                positionAnimationPlayerLayer, scaleAnimationPlayerLayer;
            private readonly IAnimationLayer<Quaternion>
                rotationAnimationPlayerLayer;

            private MeshBuffer mesh;
            private TextureBuffer texture, textureEmission;
            private SoundSource soundArea = null;
            private SoundSource soundEvent = null;

            private bool soundEventPlayedOnce = false;

            private readonly bool hasSoundArea = false;
            private bool soundAreaHearable = false;

            private readonly Demo07Game game;

            public GameEntity(Demo07Game game, Entity entity)
            {
                this.game = game ?? throw new ArgumentNullException(
                    nameof(game));
                if (entity == null)
                    throw new ArgumentNullException(nameof(entity));

                position = entity.Position;
                scale = entity.Scale;
                rotation = entity.Rotation;

                if (entity.TryGet(EntityParameter.MeshData,
                    out MeshData meshData))
                {
                    game.Resources.LoadMesh(meshData).AddFinalizer(
                        r => mesh = r);
                    if (entity.TryGet(
                        EntityParameter.ModelAnimationTimeline,
                        out Timeline modelAnimationTimeline))
                    {
                        if (meshData.Skeleton.Children.Count > 0)
                        {
                            deformerAnimationPlayer =
                                new DeformerAnimationPlayer(
                                modelAnimationTimeline, meshData.Skeleton);
                        }
                        else
                        {
                            locationAnimationPlayer =
                                new AnimationPlayer(modelAnimationTimeline);
                            locationAnimationPlayer.TryGetLayer(
                                entity.Name +
                                DeformerAnimationPlayer.PositionLayerSuffix,
                                out positionAnimationPlayerLayer);
                            locationAnimationPlayer.TryGetLayer(
                                entity.Name +
                                DeformerAnimationPlayer.ScaleLayerSuffix,
                                out scaleAnimationPlayerLayer);
                            locationAnimationPlayer.TryGetLayer(
                                entity.Name +
                                DeformerAnimationPlayer.RotationLayerSuffix,
                                out rotationAnimationPlayerLayer);
                        }
                    }
                }

                if (entity.TryGet(EntityParameter.TextureDataMain,
                    out TextureData textureData))
                    game.Resources.LoadTexture(textureData, 
                        TextureFilter.Linear).AddFinalizer(r => texture = r);

                if (entity.TryGet(EntityParameter.TextureDataEffect03,
                    out TextureData textureDataEmission))
                    game.Resources.LoadTexture(textureDataEmission,
                        TextureFilter.Linear).AddFinalizer(
                        r => textureEmission = r);

                if (!entity.TryGet(EntityParameter.Color, out color))
                    color = Color.White;

                if (!entity.TryGet(EntityParameter.ColliderPrimitive,
                    out collider)) collider = ColliderPrimitive.Empty;

                if (!entity.TryGet(EntityParameter.IsVisible,
                    out isVisible)) isVisible = true;

                if (!entity.TryGet("collisionAction",
                    out collisionAction)) 
                    collisionAction = null;

                if (entity.TryGet("soundArea", out string soundAreaPath))
                {
                    hasSoundArea = true;
                    game.Resources.LoadSound(soundAreaPath).AddFinalizer(
                        delegate(SoundSource r)
                        {
                            soundArea = r;
                            soundArea.Volume = 0;
                        }, false);
                    game.Resources.LoadingTasksCompleted += delegate (object s,
                        EventArgs e)
                    {
                        soundArea?.Play(true);
                    };
                }

                if (entity.TryGet("soundEvent", out string soundEventPath))
                {
                    game.Resources.LoadSound(soundEventPath).AddFinalizer(
                        r => soundEvent = r);
                }
            }

            public void Update(TimeSpan delta)
            {
                if (deformerAnimationPlayer != null)
                {
                    deformerAnimationPlayer.Update(delta);
                    if (deformerAnimationPlayer.IsPlaying)
                    {
                        characterDeformer =
                            deformerAnimationPlayer.GetCurrentDeformer();
                    }
                    else if (deformerAnimationPlayer.AnimationPlayer.Position
                      > TimeSpan.Zero && !deformerAnimationPlayer.IsPlaying)
                    {
                        if (collisionAction != null &&
                            collisionAction.ToLowerInvariant().Contains(
                                "closegameafterwards"))
                        {
                            game.FadeOut();
                        }
                    }
                }

                if (locationAnimationPlayer != null)
                {
                    locationAnimationPlayer.Update(delta);
                    if (locationAnimationPlayer.IsPlaying)
                    {
                        position = positionAnimationPlayerLayer.CurrentValue;
                        scale = scaleAnimationPlayerLayer.CurrentValue;
                        rotation = rotationAnimationPlayerLayer.CurrentValue;
                        locationChanged = true;
                    }
                }

                if (locationChanged)
                {
                    location = MathHelper.CreateTransformation(position, scale,
                        rotation);
                    locationChanged = false;
                }

                if (soundArea != null)
                {
                    if (soundAreaHearable)
                    {
                        soundArea.Volume = (float)Math.Min(1,
                            soundArea.Volume + soundFadeSpeed *
                            delta.TotalSeconds);
                    }
                    else
                    {
                        soundArea.Volume = (float)Math.Max(0,
                            soundArea.Volume - soundFadeSpeed *
                            delta.TotalSeconds);
                    }
                }
            }

            public void Draw(IRenderContextPhong canvas)
            {
                if (isVisible)
                {
                    canvas.Mesh = mesh;
                    canvas.Color = color;
                    canvas.Texture = texture;
                    canvas.EmissionMap = textureEmission;
                    canvas.Location = location;
                    canvas.Deformation = characterDeformer;
                    canvas.Draw();
                }
            }

            public bool Collides(Vector3 point, out bool allowMove)
            {
                bool collides = soundAreaHearable = 
                    collider.Intersects(position, point);

                if (hasSoundArea) allowMove = true;
                else if (collisionAction != null)
                {
                    allowMove = true;

                    if (collisionAction.ToLowerInvariant().Contains(
                        "playanimation") && collides)
                    {
                        if (deformerAnimationPlayer != null)
                            deformerAnimationPlayer.AnimationPlayer.Play();
                        if (locationAnimationPlayer != null)
                            locationAnimationPlayer.Play();
                        isVisible = true;
                    }
                    
                    if (collisionAction.ToLowerInvariant().Contains(
                        "playsoundonce") && collides)
                    {
                        if (soundEvent != null && !soundEventPlayedOnce)
                        {
                            soundEvent.Play(false);
                            soundEventPlayedOnce = true;
                        }
                    }                   
                }
                else allowMove = !collides;

                return collides;
            }

            public void Dispose()
            {
                mesh?.Dispose();
                texture?.Dispose();
                textureEmission?.Dispose();
                soundArea?.Dispose();
                soundEvent?.Dispose();
            }
        }

        private enum GameStage
        {
            Intro,
            Game,
            End
        }

        private const float PlayerHeadHeight = 1.40f;
        private const float PlayerHandHeight = 1.00f;

        private const float FadeInSpeed = 0.25f;

        private readonly List<GameEntity> actors = new List<GameEntity>();

        private readonly RenderParameters gameRenderParameters =
            new RenderParameters();
        private readonly RenderParameters guiRenderParameters =
            new RenderParameters();

        private ControlMapping forward, backward, left, right;
        private ControlMapping lookLeft, lookRight, lookUp, lookDown;
        private ControlMapping exit;

        private SoundSource music, musicOutro;
        private SoundSource footsteps;
        private MeshBuffer plane;
        private TextureBuffer splashStart, splashEnd, loading;
        private RenderTextureBuffer gameRenderTarget;

        private Light flashlight = Light.Disabled;

        private Vector3 playerAccerlation = Vector3.Zero;
        private Vector3 playerPosition = Vector3.Zero;

        private Angle playerRotationY = 0;
        private Angle playerRotationX = 0;

        private GameStage currentGameStage = GameStage.Intro;        

        private float primaryFadeIn = 0, secondaryFadeIn = 0;
        private TimeSpan elapsedTotal = TimeSpan.Zero;
        private float secondaryFadeInLimit = 0.3f;

        public Demo07Game()
        {
            gameRenderParameters.Camera.PerspectiveFieldOfView = Angle.Deg(80);
            gameRenderParameters.Camera.ClippingRange = new Vector2(0.05f, 99);
            gameRenderParameters.Lighting.Enabled = true;

            guiRenderParameters.Camera.ProjectionMode = 
                ProjectionMode.OrthgraphicRelativeProportional;            
        }

        protected override void Load()
        {
            Graphics.Mode = WindowMode.Fullscreen;

            forward = Controls.Map(KeyboardKey.W, GamepadAxis.LeftStickUp);
            right = Controls.Map(KeyboardKey.D, GamepadAxis.LeftStickRight);
            backward = Controls.Map(KeyboardKey.S, GamepadAxis.LeftStickDown);
            left = Controls.Map(KeyboardKey.A, GamepadAxis.LeftStickLeft);

            lookUp = Controls.Map(MouseSpeedAxis.Up, GamepadAxis.RightStickUp);
            lookRight = Controls.Map(MouseSpeedAxis.Right, 
                GamepadAxis.RightStickRight);
            lookDown = Controls.Map(MouseSpeedAxis.Down,
                GamepadAxis.RightStickDown);
            lookLeft = Controls.Map(MouseSpeedAxis.Left,
                GamepadAxis.RightStickLeft);

            exit = Controls.Map(KeyboardKey.Escape, GamepadButton.Back);

            Resources.LoadMesh(MeshData.Plane).AddFinalizer(
                r => plane = r);

            Resources.LoadSound("/officeFootsteps.wav").AddFinalizer(
                r => footsteps = r);
            Resources.LoadSound("/officeMusic.mp3").AddFinalizer(
                r => music = r);
            Resources.LoadSound("/officeMusicOutro.mp3").AddFinalizer(
                r => musicOutro = r);

            Resources.LoadTexture("/officeSplash.png").AddFinalizer(
                r => splashStart = r);
            Resources.LoadTexture("/officeSplashEnd.png").AddFinalizer(
                r => splashEnd = r);
            Resources.LoadTexture("/officeLoading.png").AddFinalizer(
                r => loading = r);

            Resources.LoadScene("/office.fbx").AddFinalizer(
                delegate (Scene scene)
                {
                    primaryFadeIn += float.Epsilon;
                    music.Play(false);

                    ImportScene(scene);
                    Resources.LoadingTasksCompleted += 
                    (s, e) => scene.Dispose();
                });

            Controls.Input.SetMouse(MouseMode.InvisibleFixed);

            Graphics.Resized += (s, e) => InitializeGameRenderTarget();
            InitializeGameRenderTarget();

            Resources.LoadingTasksCompleted += (s, a) => 
            currentGameStage = GameStage.Game;
        }

        private void InitializeGameRenderTarget()
        {
            if (gameRenderTarget != null && !gameRenderTarget.IsDisposed)
                gameRenderTarget.Dispose();
            gameRenderTarget = Graphics.CreateRenderBuffer(Graphics.Size,
                TextureFilter.Linear);
        }

        private void ImportScene(Scene scene)
        {
            foreach (Entity entity in scene)
            {
                if (entity.TryGet(EntityParameter.Light, out Light light))
                {
                    gameRenderParameters.Lighting.Add(light);
                    gameRenderParameters.Lighting.Enabled = true;
                }

                if (entity.Contains(EntityParameter.MeshData) || 
                    entity.Contains(EntityParameter.ColliderPrimitive))
                    actors.Add(new GameEntity(this, entity));

                if (entity.Name != null && 
                    entity.Name.Trim().ToLower() == "player")
                {
                    playerPosition = entity.Position;

                    Quaternion q = entity.Rotation;
                    double sinp = 2 * (q.W * q.Y - q.Z * q.X);
                    if (Math.Abs(sinp) >= 1) playerRotationY =
                            (float)(Math.PI / 2 * (sinp >= 0 ? 1 : -1));
                    else playerRotationY = (float)Math.Asin(sinp);
                }
            }
        }

        protected override void Redraw(TimeSpan delta)
        {
            Graphics.Render<IRenderContextPhong>(gameRenderParameters, 
                RenderGame, gameRenderTarget);
            Graphics.Render<IRenderContext>(guiRenderParameters, RenderGui);
        }

        private void RenderGui(IRenderContext c)
        {
            c.Mesh = plane;

            c.Texture = gameRenderTarget;
            c.Location = MathHelper.CreateTransformation(0.5f, 0.5f, 1,
                Math.Max(1, (float)Graphics.Size.Width / Graphics.Size.Height),
                Math.Max(1, (float)Graphics.Size.Height / Graphics.Size.Width),
                1);

            if (currentGameStage == GameStage.Intro ||
                currentGameStage == GameStage.Game) 
                c.Opacity = secondaryFadeIn;
            else if (currentGameStage == GameStage.End) c.Opacity = 0;
            else c.Opacity = 1;

            c.Draw();

            c.Opacity = primaryFadeIn;
            if (currentGameStage == GameStage.Intro)
            {
                c.Texture = splashStart;
                c.Location = MathHelper.CreateTransformation(
                    0.5f, 0.5f, 0.5f, 0.7f, 0.6f, 1);
                c.Draw();
            }
            else if (currentGameStage == GameStage.End)
            {
                c.Texture = splashEnd;
                c.Location = MathHelper.CreateTransformation(
                    0.5f, 0.5f, 0.5f, 0.7f, 0.6f, 1);
                c.Draw();
            }

            if (Resources.LoadingTasksPending > 0)
            {
                c.Texture = loading;
                c.TextureClipping = new Rectangle(0,
                    1 - DateTime.Now.TimeOfDay.Seconds % 4 * 0.25f
                    , 1, 0.25f);
                c.Location = MathHelper.CreateTransformation(
                    Math.Max(1, (float)Graphics.Size.Width / 
                    Graphics.Size.Height)
                    - 0.525f, 0.03f, 0.4f,
                    0.15f, 0.05f, 1);
                c.Draw();
            }
        }

        private void RenderGame(IRenderContextPhong c)
        {
            foreach (GameEntity entity in actors) entity.Draw(c);
        }

        private void FadeOut()
        {
            if (currentGameStage != GameStage.End)
            {
                currentGameStage = GameStage.End;
                musicOutro.Play(false);
            }
        }

        protected override void Update(TimeSpan delta)
        {         
            if (primaryFadeIn > 0 && primaryFadeIn < 1) 
                primaryFadeIn = (float)Math.Min(1, primaryFadeIn +
                    delta.TotalSeconds * FadeInSpeed);
            if (secondaryFadeIn > 0 && secondaryFadeIn < 1)
                secondaryFadeIn = (float)Math.Min(secondaryFadeInLimit, 
                    secondaryFadeIn + delta.TotalSeconds * FadeInSpeed);

            if ((elapsedTotal += delta) > TimeSpan.FromSeconds(7.5) &&
                secondaryFadeIn == 0) secondaryFadeIn = float.Epsilon;

            playerRotationY += Angle.Deg((lookRight - lookLeft) * 4);
            playerRotationX += Angle.Deg((lookDown - lookUp) * 4);

            Vector3 accerlation = 
                new Vector3(right - left, 0, forward - backward);
            if (accerlation.Length() > 1) 
                accerlation = Vector3.Normalize(accerlation);
            Vector3 accerlationRotated = new Vector3(
                (float)(accerlation.X * Math.Cos(playerRotationY) + 
                accerlation.Z * Math.Sin(playerRotationY)), accerlation.Y,
                (float)(-accerlation.X * Math.Sin(playerRotationY) +
                accerlation.Z * Math.Cos(playerRotationY)));
            playerAccerlation += accerlationRotated * 
                (float)(delta.TotalSeconds * 0.1);
            playerAccerlation *= 0.9f;

            if (secondaryFadeIn == 0) playerAccerlation = Vector3.Zero;

            Vector3 newPlayerPosition = playerPosition + playerAccerlation;

            bool collisionPreventsMoving = false;

            foreach (GameEntity entity in actors)
            {
                entity.Collides(newPlayerPosition, 
                    out bool allowMove);

                collisionPreventsMoving |= !allowMove;
                entity.Update(delta);
            }

            if (currentGameStage == GameStage.Game &&
                secondaryFadeInLimit == 0.3f) secondaryFadeInLimit = 1;

            if (playerAccerlation.Length() > 0 &&
                currentGameStage == GameStage.Intro)
                currentGameStage = GameStage.Game;

            if (currentGameStage != GameStage.End)
            {
                if (!collisionPreventsMoving)
                    playerPosition = newPlayerPosition;
                else playerAccerlation = -playerAccerlation;//boing

                if (playerAccerlation.Length() > 0.01f)
                {
                    if (footsteps != null && !footsteps.IsPlaying)
                        footsteps.Play(true);
                }
                else
                {
                    if (footsteps != null && footsteps.IsPlaying)
                        footsteps.Stop();
                }
            }

            float walkingHeightModifier = (float)Math.Sin(
                DateTime.Now.TimeOfDay.TotalSeconds * 10) * 
                playerAccerlation.Length();

            gameRenderParameters.Camera.MoveTo(playerPosition.X,
                playerPosition.Y + PlayerHeadHeight + walkingHeightModifier, 
                playerPosition.Z);
            gameRenderParameters.Camera.RotateTo(playerRotationX, 
                playerRotationY, 0);            
            
            if (flashlight != Light.Disabled)
                gameRenderParameters.Lighting.Remove(flashlight);

            flashlight = new Light(new Color(255,243,236),
                new Vector3(playerPosition.X,
                playerPosition.Y + PlayerHandHeight + walkingHeightModifier, 
                playerPosition.Z), 7,
                MathHelper.RotateDirection(Vector3.UnitZ,
                Quaternion.CreateFromYawPitchRoll(playerRotationY,
                playerRotationX, 0)), Angle.Deg(25), Angle.Deg(3));
            gameRenderParameters.Lighting.Add(flashlight);

            if (exit.IsActivated) Close();
        }

        protected override void Unload()
        {
            ClearScene();
            footsteps?.Dispose();
            plane?.Dispose();
        }

        private void ClearScene()
        {
            foreach (var actor in actors) actor.Dispose();
            actors.Clear();
            gameRenderParameters.Lighting.Clear();
        }
    }
}
