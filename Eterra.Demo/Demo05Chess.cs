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
using System.Collections.Generic;
using System.Numerics;

namespace Eterra.Demo
{
    /// <summary>
    /// Provides a demonstration on how to use the techniques demonstrated in
    /// the previous demos to create the foundation for a simple 2D chess game.
    /// </summary>
    /// <remarks>
    /// It's more of a "foundation" than a real game because it doesn't 
    /// contain any implementation of game logic, as it should only 
    /// demonstrate how the resource, graphics and user input part could 
    /// be done.
    /// </remarks>
    class Demo05Chess : EterraApplicationBase
    {
        private enum PawnType
        {
            Disabled,
            Empty,
            PawnLight,
            PawnDark,
            RookLight,
            RookDark,
            KnightLight,
            KnightDark,
            BishopLight,
            BishopDark,
            QueenLight,
            QueenDark,
            KingLight,
            KingDark
        }

        private readonly struct SelectedPawn
        {
            public bool IsSelected { get; }
            public int X { get; }
            public int Y { get; }

            public SelectedPawn(int x, int y) : this()
            {
                X = x;
                Y = y;
                IsSelected = true;
            }
        }

        private const float FieldLength = 1.0f / FieldCount;
        private const int FieldCount = 8;
        private const int borderTiles = 5;

        private readonly Vector3 FieldScale = 
            new Vector3(FieldLength, FieldLength, 1);
        private readonly TimeSpan fieldInitTimeSpan =
            TimeSpan.FromMilliseconds(50);

        private readonly Color SelectionColor
            = new Color(255, 255, 255, 200);

        private readonly PawnType[,] defaultField =
            new PawnType[8, 8];

        private PawnType[,] field = new PawnType[FieldCount, FieldCount];

        private TextureBuffer splashScreen;
        private TextureBuffer tileLight, tileDark, tileVeryDark,
            tileReset, tileExit;
        private readonly Dictionary<PawnType, TextureBuffer>
            pawnTextures = new Dictionary<PawnType, TextureBuffer>();
        private MeshBuffer plane;

        private ControlMapping click;

        private SelectedPawn selectedPawn = new SelectedPawn();

        private TimeSpan fieldInitTimer = TimeSpan.Zero;
        private TimeSpan fieldInitLastUpdate = TimeSpan.FromSeconds(-2);
        private bool FieldInitialisationStarted => 
            fieldInitX > -2 || fieldInitY > 0;
        private bool FieldInitialized =>
            fieldInitX == 7 && fieldInitY == 7;
        private int fieldInitX = -2, fieldInitY = 0;

        private readonly RenderParameters renderParameters = 
            new RenderParameters();

        public Demo05Chess()
        {
            defaultField[0, 0] = defaultField[7, 0] = PawnType.RookLight;
            defaultField[1, 0] = defaultField[6, 0] = PawnType.KnightLight;
            defaultField[2, 0] = defaultField[5, 0] = PawnType.BishopLight;
            defaultField[3, 0] = PawnType.QueenLight;
            defaultField[4, 0] = PawnType.KingLight;
            for (int x = 0; x < 8; x++)
                defaultField[x, 1] = PawnType.PawnLight;

            defaultField[0, 7] = defaultField[7, 7] = PawnType.RookDark;
            defaultField[1, 7] = defaultField[6, 7] = PawnType.KnightDark;
            defaultField[2, 7] = defaultField[5, 7] = PawnType.BishopDark;
            defaultField[3, 7] = PawnType.QueenDark;
            defaultField[4, 7] = PawnType.KingDark;
            for (int x = 0; x < 8; x++)
                defaultField[x, 6] = PawnType.PawnDark;

            for (int y = 2; y < 6; y++)
                for (int x = 0; x < 8; x++)
                    defaultField[x, y] = PawnType.Empty;
        }

        protected override void Load()
        {
            void ErrorHandler(Exception exc)
            {
                Log.Error(exc);
                Close();
            }

            TextureFilter f = TextureFilter.Nearest;

            Resources.LoadTexture("/chessSplash.png", f).AddFinalizer(
                r => splashScreen = r, ErrorHandler);
            Resources.LoadTexture("/chessTileLight.png", f).AddFinalizer(
                r => tileLight = r, ErrorHandler);
            Resources.LoadTexture("/chessTileDark.png", f).AddFinalizer(
                r => tileDark = r, ErrorHandler);
            Resources.LoadTexture("/chessTileDarker.png", f).AddFinalizer(
                r => tileVeryDark = r, ErrorHandler);
            Resources.LoadTexture("/chessTileReset.png", f).AddFinalizer(
                r => tileReset = r, ErrorHandler);
            Resources.LoadTexture("/chessTileExit.png", f).AddFinalizer(
                r => tileExit = r, ErrorHandler);

            foreach (PawnType pawn in Enum.GetValues(typeof(PawnType)))
            {
                if (pawn != PawnType.Empty && pawn != PawnType.Disabled)
                    Resources.LoadTexture("/chess" + pawn + ".png", f)
                        .AddFinalizer(r => pawnTextures.Add(pawn, r));
            }

            Resources.LoadMesh(MeshData.Plane).AddFinalizer(
                r => plane = r, ErrorHandler);
            click = Controls.Map(MouseButton.Left);

            renderParameters.Camera.ProjectionMode =
                ProjectionMode.OrthgraphicRelativeProportional;

            DateTime loadingStart = DateTime.Now;
            Resources.LoadingTasksCompleted += (s, a) =>
                Log.Trace("Resources loaded in " +
                (DateTime.Now - loadingStart).TotalSeconds.ToString("F2") +
                " seconds.");
        }

        protected override void Redraw(TimeSpan delta)
        {
            Graphics.Render<IRenderContext>(renderParameters, RenderCanvas);
        }

        private void RenderCanvas(IRenderContext canvas)
        {
            canvas.Mesh = plane;

            if (!FieldInitialisationStarted)
            {
                canvas.Location = 
                    Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.2f);
                canvas.Texture = splashScreen;
                canvas.Draw();
            }

            bool drawLightField = false;
            for (int y = -borderTiles; y < FieldCount + borderTiles; y++)
            {
                for (int x = -borderTiles; x < FieldCount + borderTiles; x++)
                {
                    bool isBorderDrawing = (x < 0 || x >= FieldCount)
                        || (y < 0 || y >= FieldCount);
                    PawnType fieldPawn = isBorderDrawing ?
                        PawnType.Disabled : field[x, y];

                    if (!isBorderDrawing && fieldPawn != PawnType.Disabled)
                        canvas.Texture = 
                            (drawLightField ? tileLight : tileDark);
                    else if ((y == 0 && x == 8) || (y == -1 && x == 7))
                        canvas.Texture = tileExit;
                    else if ((y == 1 && x == 8) || (y == -1 && x == 6))
                        canvas.Texture = tileReset;
                    else canvas.Texture = tileVeryDark;

                    canvas.Location = MathHelper.CreateTransformation(
                        new Vector3(x * FieldLength + FieldLength / 2, 
                        y * FieldLength + FieldLength / 2, 1),
                        FieldScale);
                    canvas.Draw();

                    if (isBorderDrawing) continue;

                    if (fieldPawn != PawnType.Empty)
                    {
                        canvas.Location = 
                            MathHelper.CreateTransformation(new Vector3(
                                    x * FieldLength + FieldLength / 2,
                                    y * FieldLength + FieldLength / 2, 0.5f),
                                FieldScale);
                        pawnTextures.TryGetValue(fieldPawn,
                            out TextureBuffer pawnTexture);
                        canvas.Texture = pawnTexture;
                        canvas.Color = Color.Black;
                        canvas.Draw();
                    }

                    if (selectedPawn.IsSelected && selectedPawn.X == x &&
                        selectedPawn.Y == y)
                    {
                        canvas.Location = 
                            MathHelper.CreateTransformation(new Vector3(
                                    x * FieldLength + FieldLength / 2,
                                    y * FieldLength + FieldLength / 2, 0.4f),
                                FieldScale);
                        canvas.Texture = null;
                        canvas.Color = SelectionColor;
                        canvas.Draw();
                    }

                    drawLightField = !drawLightField;
                }
                drawLightField = !drawLightField;
            }
        }

        protected override void Update(TimeSpan delta)
        {
            bool mouseClicked = click.IsActivated;

            Vector2 positionMouse = Graphics.PointToOrthographic(
                Controls.Input.GetMousePosition(), true);

            Vector2 positionField = new Vector2(positionMouse.X * 8,
                positionMouse.Y * 8);

            int fieldX = (int)positionField.X - (positionField.X < 0 ? 1 : 0);
            int fieldY = (int)positionField.Y - (positionField.Y < 0 ? 1 : 0);

            if (mouseClicked && ((fieldY == 0 && fieldX == 8)
                || (fieldY == -1 && fieldX == 7)))
            {
                Close();
                return;
            }

            if (mouseClicked && (!FieldInitialisationStarted ||
                (fieldY == 1 && fieldX == 8)
                || (fieldY == -1 && fieldX == 6)))
            {
                fieldInitX = -1;
                fieldInitY = 0;
                field = new PawnType[FieldCount, FieldCount];
            }

            if (FieldInitialisationStarted && !FieldInitialized)
            {
                fieldInitTimer += delta;
                if ((fieldInitTimer - fieldInitLastUpdate) > fieldInitTimeSpan)
                {
                    fieldInitLastUpdate = fieldInitTimer;
                    fieldInitY += ((fieldInitX + 1) / FieldCount);
                    fieldInitX = (fieldInitX + 1) % FieldCount;
                    field[fieldInitX, fieldInitY] = defaultField[fieldInitX,
                        fieldInitY];
                }
            }

            if (mouseClicked && FieldInitialized)
            {
                //That subtraction is important so that -0.7 wont get rounded 
                //to 0 (which would still be on the field, which is not what
                //is wanted here)
                bool clickOutsideField =
                    positionMouse.X < 0 || positionMouse.X > 1
                    || positionMouse.Y < 0 || positionMouse.Y > 1;

                if (selectedPawn.IsSelected)
                {
                    if (clickOutsideField)
                        field[selectedPawn.X, selectedPawn.Y] = PawnType.Empty;
                    else if (fieldX == selectedPawn.X && fieldY ==
                        selectedPawn.Y)
                        selectedPawn = default;
                    else
                    {
                        field[fieldX, fieldY] = field[selectedPawn.X,
                            selectedPawn.Y];
                        field[selectedPawn.X, selectedPawn.Y] = PawnType.Empty;
                    }
                    selectedPawn = default;
                }
                else if (!clickOutsideField)
                {
                    selectedPawn = new SelectedPawn(fieldX, fieldY);
                }
            }
        }

        protected override void Unload()
        {
            foreach (TextureBuffer pawnTexture in pawnTextures.Values)
                pawnTexture.Dispose();
            pawnTextures.Clear();

            splashScreen?.Dispose();
            tileLight?.Dispose();
            tileDark?.Dispose();
            tileVeryDark?.Dispose();
            tileReset?.Dispose();
            tileExit?.Dispose();

            plane?.Dispose();
        }
    }
}
