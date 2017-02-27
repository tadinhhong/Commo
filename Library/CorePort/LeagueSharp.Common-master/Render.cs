namespace LeagueSharp.Common
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Diagnostics.CodeAnalysis;

    using SharpDX;
    using SharpDX.Direct3D9;
    using EloBuddy;

    using Color = System.Drawing.Color;
    using Font = SharpDX.Direct3D9.Font;
    using Rectangle = SharpDX.Rectangle;
    using SharpDX.Text;
    using Properties;

    /// <summary>
    ///     The render class allows you to draw stuff using SharpDX easier.
    /// </summary>
    public static class Render
    {
        #region Static Fields
        /// <summary>
        ///     The visible render objects.
        /// </summary>
        private static List<RenderObject> _renderVisibleObjects = new List<RenderObject>();

        private static readonly List<RenderObject> RenderObjects = new List<RenderObject>();

        private static readonly object RenderObjectsLock = new object();

        private static List<RenderObject> renderVisibleObjects = new List<RenderObject>();

        private static bool terminateThread;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes static members of the <see cref="Render" /> class.
        /// </summary>
        static Render()
        {
            Drawing.OnEndScene += OnEndScne;
            Drawing.OnDraw += OnDraw;

            var thread = new Thread(PrepareObjects);
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the device.
        /// </summary>
        /// <value>The device.</value>
        public static Device Device => Drawing.Direct3DDevice;

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Adds the specified layer.
        /// </summary>
        /// <param name="renderObject">The render object.</param>
        /// <param name="layer">The layer.</param>
        /// <returns>RenderObject.</returns>
        public static RenderObject Add(this RenderObject renderObject, float layer = float.MaxValue)
        {
            renderObject.Layer = !layer.Equals(float.MaxValue) ? layer : renderObject.Layer;
            lock (RenderObjectsLock)
            {
                RenderObjects.Add(renderObject);
            }

            return renderObject;
        }

        /// <summary>
        ///     Determines if the point is on the screen.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns><c>true</c> if the point is on the screen, <c>false</c> otherwise.</returns>
        public static bool OnScreen(Vector2 point)
            => point.X > 0 && point.Y > 0 && point.X < Drawing.Width && point.Y < Drawing.Height;

        /// <summary>
        ///     Removes the specified render object.
        /// </summary>
        /// <param name="renderObject">The render object.</param>
        public static void Remove(this RenderObject renderObject)
        {
            lock (RenderObjectsLock)
            {
                RenderObjects.Remove(renderObject);
            }
        }

        public static void Terminate() => terminateThread = true;

        #endregion

        #region Methods

        /// <summary>
        ///     Fired when the game is drawn.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>

        private static void OnDraw(EventArgs args)
        {
            if (Device == null || Device.IsDisposed)
            {
                return;
            }

            foreach (var renderObject in renderVisibleObjects)
            {
                renderObject.OnDraw();
            }
        }

        /// <summary>
        ///     Fired when the scene ends, and everything has been rendered.
        /// </summary>
        /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
        private static void OnEndScne(EventArgs args)
        {
            if (Device == null || Device.IsDisposed)
            {
                return;
            }

            Device.SetRenderState(RenderState.AlphaBlendEnable, true);

            foreach (var renderObject in renderVisibleObjects)
            {
                renderObject.OnEndScene();
            }
        }

        /// <summary>
        ///     Prepares the objects.
        /// </summary>
        private static void PrepareObjects()
        {
            while (!terminateThread)
            {
                try
                {
                    Thread.Sleep(1);
                    lock (RenderObjectsLock)
                    {
                        renderVisibleObjects =
                            RenderObjects.Where(o => o != null && o.Visible && o.HasValidLayer())
                                .OrderBy(o => o.Layer)
                                .ToList();
                    }
                }
                catch (ThreadAbortException)
                {
                    // ignored
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Cannot prepare RenderObjects for drawing. Ex:" + e);
                }
            }
        }

        #endregion

        /// <summary>
        ///     Draws circles.
        /// </summary>
        public class Circle : RenderObject
        {
            #region Static Fields

            /// <summary>
            ///     The sprite effect
            /// </summary>
            private static Effect Effect { get; set; }

            /// <summary>
            ///     <c>true</c> if this instanced initialized.
            /// </summary>
            private static bool Initialized { get; set; }

            /// <summary>
            ///     The offset
            /// </summary>
            private static Vector3 _offset = new Vector3(0, 0, 0);

            /// <summary>
            ///     The vertex declaration
            /// </summary>
            private static VertexBuffer VertexBuffer { get; set; }

            private static VertexDeclaration VertexDeclaration { get; set; }

            #endregion

            #region Constructors and Destructors
            /*
            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="unit">The unit.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="color">The color.</param>
            /// <param name="width">The width.</param>
            /// <param name="zDeep">if set to <c>true</c> [z deep].</param>
            public Circle(GameObject unit, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Unit = unit;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.SubscribeToResetEvents();
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="unit">The unit.</param>
            /// <param name="offset">The offset.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="color">The color.</param>
            /// <param name="width">The width.</param>
            /// <param name="zDeep">if set to <c>true</c> [z deep].</param>
            public Circle(GameObject unit, Vector3 offset, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Unit = unit;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.Offset = offset;
                this.SubscribeToResetEvents();
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="offset">The offset.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="color">The color.</param>
            /// <param name="width">The width.</param>
            /// <param name="zDeep">if set to <c>true</c> [z deep].</param>
            public Circle(
                Vector3 position,
                Vector3 offset,
                float radius,
                Color color,
                int width = 1,
                bool zDeep = false)
            {
                this.Color = color;
                this.Position = position;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.Offset = offset;
                this.SubscribeToResetEvents();
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="color">The color.</param>
            /// <param name="width">The width.</param>
            /// <param name="zDeep">if set to <c>true</c> [z deep].</param>
            public Circle(Vector3 position, float radius, Color color, int width = 1, bool zDeep = false)
            {
                this.Color = color;
                this.Position = position;
                this.Radius = radius;
                this.Width = width;
                this.ZDeep = zDeep;
                this.SubscribeToResetEvents();
            }//*/

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="unit">
            ///     The unit.
            /// </param>
            /// <param name="radius">
            ///     The radius.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="zDeep">
            ///     A value indicating whether to enable depth.
            /// </param>
            public Circle(GameObject unit, float radius, Color color, int width = 1, bool zDeep = false)
                : this(radius, color, width, zDeep)
            {
                this.Unit = unit;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="unit">
            ///     The unit.
            /// </param>
            /// <param name="offset">
            ///     The offset.
            /// </param>
            /// <param name="radius">
            ///     The radius.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="zDeep">
            ///     A value indicating whether to enable depth.
            /// </param>
            public Circle(GameObject unit, Vector3 offset, float radius, Color color, int width = 1, bool zDeep = false)
                : this(radius, color, width, zDeep)
            {
                this.Unit = unit;
                this.Offset = offset;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="pos">
            ///     The position.
            /// </param>
            /// <param name="offset">
            ///     The offset.
            /// </param>
            /// <param name="radius">
            ///     The radius.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="zDeep">
            ///     A value indicating whether to enable depth.
            /// </param>
            public Circle(Vector3 pos, Vector3 offset, float radius, Color color, int width = 1, bool zDeep = false)
                : this(radius, color, width, zDeep)
            {
                this.Position = pos;
                this.Offset = offset;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Circle" /> class.
            /// </summary>
            /// <param name="pos">
            ///     The position.
            /// </param>
            /// <param name="radius">
            ///     The radius.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="zDeep">
            ///     A value indicating whether to enable depth.
            /// </param>
            public Circle(Vector3 pos, float radius, Color color, int width = 1, bool zDeep = false)
                : this(radius, color, width, zDeep)
            {
                this.Position = pos;
            }

            private Circle(float radius, Color color, int width, bool zDeep)
            {
                this.Radius = radius;
                this.Color = color;
                this.Width = width;
                this.ZDeep = zDeep;
                this.SubscribeToResetEvents();
            }

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            /// <value>The color.</value>
            public Color Color { get; set; }

            /// <summary>
            ///     Gets or sets the offset.
            /// </summary>
            /// <value>The offset.</value>
            public Vector3 Offset { get; set; } = default(Vector3);

            /// <summary>
            ///     Gets or sets the position.
            /// </summary>
            /// <value>The position.</value>
            public Vector3 Position { get; set; }

            /// <summary>
            ///     Gets or sets the radius.
            /// </summary>
            /// <value>The radius.</value>
            public float Radius { get; set; }

            /// <summary>
            ///     Gets or sets the unit.
            /// </summary>
            /// <value>The unit.</value>
            public GameObject Unit { get; set; }

            /// <summary>
            ///     Gets or sets the width.
            /// </summary>
            /// <value>The width.</value>
            public int Width { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether to enable depth buffering.
            /// </summary>
            /// <value><c>true</c> if depth buffering enabled; otherwise, <c>false</c>.</value>
            public bool ZDeep { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     Creates the vertexes.
            /// </summary>
            public static void CreateVertexes()
            {
                const Usage Usage = Usage.WriteOnly;
                const VertexFormat Format = VertexFormat.None;
                const Pool Pool = Pool.Managed;

                var sizeInBytes = Utilities.SizeOf<Vector4>() * 2 * 6;

                VertexBuffer = new VertexBuffer(Device, sizeInBytes, Usage, Format, Pool);
                SatisfyBuffer(VertexBuffer.Lock(0, 0, LockFlags.None));
                VertexBuffer.Unlock();

                var vertexElements = CreateVertexElements();
                VertexDeclaration = new VertexDeclaration(Device, vertexElements);

                try
                {
                    var effect = Encoding.UTF8.GetString(Resources.CircleEffect);
                    Effect = Effect.FromString(Device, effect, ShaderFlags.None);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (!Initialized)
                {
                    Initialized = true;
                    Drawing.OnPreReset += OnPreReset;
                    Drawing.OnPostReset += OnPostReset;
                }
            }
            private static VertexElement[] CreateVertexElements()
                =>
                new[]
                    {
                        new VertexElement(
                            0,
                            0,
                            DeclarationType.Float4,
                            DeclarationMethod.Default,
                            DeclarationUsage.Position,
                            0),
                        new VertexElement(
                            0,
                            16,
                            DeclarationType.Float4,
                            DeclarationMethod.Default,
                            DeclarationUsage.Color,
                            0),
                        VertexElement.VertexDeclarationEnd
                    };

            /// <summary>
            ///     Draws the circle.
            /// </summary>
            /// <param name="position">The position.</param>
            /// <param name="radius">The radius.</param>
            /// <param name="color">The color.</param>
            /// <param name="width">The width.</param>
            /// <param name="zDeep">if set to <c>true</c> the circle will be drawn with depth buffering.</param>           
            public static void DrawCircle(Vector3 pos, float radius, Color color, int width = 5, bool zDeep = false)
            {
                if (Device == null || Device.IsDisposed)
                {
                    return;
                }

                if (VertexBuffer == null)
                {
                    CreateVertexes();
                }

                if ((VertexBuffer?.IsDisposed ?? false) || VertexDeclaration.IsDisposed || Effect.IsDisposed)
                {
                    return;
                }

                try
                {
                    var vertexDeclaration = Device.VertexDeclaration;

                    Effect.Begin();
                    Effect.BeginPass(0);

                    Effect.SetValue(
                        "ProjectionMatrix",
                        Matrix.Translation(pos.SwitchYZ()) * Drawing.View * Drawing.Projection);
                    Effect.SetValue(
                        "CircleColor",
                        new Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f));
                    Effect.SetValue("Radius", radius);
                    Effect.SetValue("Border", 2f + width);
                    Effect.SetValue("zEnabled", zDeep);

                    Device.SetStreamSource(0, VertexBuffer, 0, Utilities.SizeOf<Vector4>() * 2);
                    Device.VertexDeclaration = VertexDeclaration;

                    Device.DrawPrimitives(PrimitiveType.TriangleList, 0, 2);

                    Effect.EndPass();
                    Effect.End();

                    Device.VertexDeclaration = vertexDeclaration;
                }
                catch (Exception e)
                {
                    Dispose(null, EventArgs.Empty);
                    Console.WriteLine(@"DrawCircle: " + e);
                }
            }

            /// <summary>
            ///     Called when the circle is drawn.
            /// </summary>
            public override void OnDraw()
            {
                try
                {
                    var position = default(Vector3);
                    if (this.Unit?.IsValid ?? false)
                    {
                        position = this.Unit.Position + this.Offset;
                    }
                    else if (!(this.Position + this.Offset).To2D().IsZero)
                    {
                        position = this.Position + this.Offset;
                    }

                    if (!position.IsZero)
                    {
                        DrawCircle(position, this.Radius, this.Color, this.Width, this.ZDeep);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Circle.OnEndScene: " + e);
                }
            }

            #endregion

            #region Methods

            /// <summary>
            ///     Disposes the circle.
            /// </summary>
            /// <param name="sender">The sender.</param>
            /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
            internal static void Dispose(object sender, EventArgs e)
            {
                Initialized = false;
                OnPreReset(EventArgs.Empty);

                if (Effect != null && !Effect.IsDisposed)
                {
                    Effect.Dispose();
                }

                if (VertexBuffer != null && !VertexBuffer.IsDisposed)
                {
                    VertexBuffer.Dispose();
                }

                if (VertexDeclaration != null && !VertexDeclaration.IsDisposed)
                {
                    VertexDeclaration.Dispose();
                }
            }

            /// <summary>
            ///     Handles the <see cref="E:PostReset" /> event.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
            private static void OnPostReset(EventArgs args)
            {
                if (Effect != null && !Effect.IsDisposed)
                {
                    Effect.OnResetDevice();
                }
            }

            /// <summary>
            ///     Handles the <see cref="E:PreReset" /> event.
            /// </summary>
            /// <param name="args">The <see cref="EventArgs" /> instance containing the event data.</param>
            private static void OnPreReset(EventArgs args)
            {
                if (Effect != null && !Effect.IsDisposed)
                {
                    Effect.OnLostDevice();
                }
            }

            private static void SatisfyBuffer(DataStream dataStream)
            {
                const float X = 6000f;
                var range = new Vector4[12];

                for (var i = 1; i < range.Length; i += 2)
                {
                    range[i] = Vector4.Zero;
                }

                // T1
                range[0] = new Vector4(-X, 0f, -X, 1.0f);
                range[2] = new Vector4(-X, 0f, X, 1.0f);
                range[4] = new Vector4(X, 0f, -X, 1.0f);

                // T2
                range[6] = new Vector4(-X, 0f, X, 1.0f);
                range[8] = new Vector4(X, 0f, X, 1.0f);
                range[10] = new Vector4(X, 0f, -X, 1.0f);

                dataStream.WriteRange(range);
            }

            #endregion
        }

        /// <summary>
        ///     Draws lines.
        /// </summary>
        public class Line : RenderObject
        {
            #region Fields

            private int width;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Line" /> class.
            /// </summary>
            /// <param name="start">
            ///     The start.
            /// </param>
            /// <param name="end">
            ///     The end.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            public Line(Vector2 start, Vector2 end, int width, ColorBGRA color)
            {
                this.DeviceLine = new SharpDX.Direct3D9.Line(Device);

                this.Width = width;
                this.Color = color;
                this.Start = start;
                this.End = end;

                Game.OnUpdate += this.OnUpdate;
                this.SubscribeToResetEvents();
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The position update delegate.
            /// </summary>
            /// <returns>
            ///     The <see cref="Vector2" />.
            /// </returns>
            public delegate Vector2 PositionDelegate();

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; }

            /// <summary>
            ///     Gets or sets the ending position.
            /// </summary>
            public Vector2 End { get; set; }

            /// <summary>
            ///     Gets or sets the end position update.
            /// </summary>
            public PositionDelegate EndPositionUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the starting position.
            /// </summary>
            public Vector2 Start { get; set; }

            /// <summary>
            ///     Gets or sets the start position update.
            /// </summary>
            public PositionDelegate StartPositionUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the line width.
            /// </summary>
            public int Width
            {
                get
                {
                    return this.width;
                }

                set
                {
                    this.DeviceLine.Width = value;
                    this.width = value;
                }
            }

            #endregion

            #region Properties

            private SharpDX.Direct3D9.Line DeviceLine { get; }

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc />
            public override void OnEndScene()
            {
                if (this.DeviceLine == null || this.DeviceLine.IsDisposed)
                {
                    return;
                }

                try
                {
                    this.DeviceLine.Begin();
                    this.DeviceLine.Draw(new[] { this.Start, this.End }, this.Color);
                    this.DeviceLine.End();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Line.OnEndScene: " + e);
                }
            }

            /// <inheritdoc />
            public override void OnPostReset()
            {
                base.OnPostReset();
                this.DeviceLine?.OnResetDevice();
            }

            /// <inheritdoc />
            public override void OnPreReset()
            {
                base.OnPreReset();
                this.DeviceLine?.OnLostDevice();
            }

            #endregion

            #region Methods

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (!this.DeviceLine.IsDisposed)
                {
                    this.DeviceLine.Dispose();
                }

                Game.OnUpdate -= this.OnUpdate;
            }

            private void OnUpdate(EventArgs args)
            {
                if (this.StartPositionUpdate != null)
                {
                    this.Start = this.StartPositionUpdate();
                }

                if (this.EndPositionUpdate != null)
                {
                    this.End = this.EndPositionUpdate();
                }
            }

            #endregion
        }

        /// <summary>
        ///     Draws a Rectangle.
        /// </summary>
        public class Rectangle : RenderObject
        {
            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Rectangle" /> class.
            /// </summary>
            /// <param name="x">
            ///     The X-axis of the position.
            /// </param>
            /// <param name="y">
            ///     The Y-axis of the position.
            /// </param>
            /// <param name="width">
            ///     The width.
            /// </param>
            /// <param name="height">
            ///     The height.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            public Rectangle(int x, int y, int width, int height, ColorBGRA color)
            {
                this.DeviceLine = new SharpDX.Direct3D9.Line(Device) { Width = height };

                this.X = x;
                this.Y = y;
                this.Width = width;
                this.Height = height;
                this.Color = color;

                Game.OnUpdate += this.OnUpdate;
                this.SubscribeToResetEvents();
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The position update delegate.
            /// </summary>
            /// <returns>
            ///     The <see cref="Vector2" />.
            /// </returns>
            public delegate Vector2 PositionDelegate();

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; }

            /// <summary>
            ///     Gets or sets the height.
            /// </summary>
            public int Height { get; set; }

            /// <summary>
            ///     Gets or sets the position update.
            /// </summary>
            public PositionDelegate PositionUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the width.
            /// </summary>
            public int Width { get; set; }

            /// <summary>
            ///     Gets or sets the X-axis of the position.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            ///     Gets or sets the Y-axis of the position.
            /// </summary>
            public int Y { get; set; }

            #endregion

            #region Properties

            private SharpDX.Direct3D9.Line DeviceLine { get; }

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc />
            public override void OnEndScene()
            {
                if (this.DeviceLine == null || this.DeviceLine.IsDisposed)
                {
                    return;
                }

                try
                {
                    this.DeviceLine.Begin();
                    this.DeviceLine.Draw(
                        new[]
                            {
                                new Vector2(this.X, this.Y + (this.Height / 2)),
                                new Vector2(this.X + this.Width, this.Y + (this.Height / 2))
                            },
                        this.Color);
                    this.DeviceLine.End();
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Rectangle.OnEndScene: " + e);
                }
            }

            /// <inheritdoc />
            public override void OnPostReset() => this.DeviceLine.OnResetDevice();

            /// <inheritdoc />
            public override void OnPreReset() => this.DeviceLine.OnLostDevice();

            #endregion

            #region Methods

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (!this.DeviceLine.IsDisposed)
                {
                    this.DeviceLine.Dispose();
                }

                Game.OnUpdate -= this.OnUpdate;
            }

            private void OnUpdate(EventArgs args)
            {
                if (this.PositionUpdate != null)
                {
                    var pos = this.PositionUpdate();
                    this.X = (int)pos.X;
                    this.Y = (int)pos.Y;
                }
            }

            #endregion
        }

        /// <summary>
        ///     A base class that renders objects.
        /// </summary>
        public class RenderObject : IDisposable
        {
            #region Fields

            private bool visible = true;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Finalizes an instance of the <see cref="RenderObject" /> class.
            /// </summary>
            ~RenderObject()
            {
                this.Dispose(false);
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The visible condition delegate.
            /// </summary>
            /// <param name="sender">
            ///     The sender.
            /// </param>
            /// <returns>
            ///     The <see cref="bool" />.
            /// </returns>
            public delegate bool VisibleConditionDelegate(RenderObject sender);

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets a value indicating whether the render object was dispoed.
            /// </summary>
            public bool IsDisposed { get; private set; }

            /// <summary>
            ///     Gets or sets the layer.
            /// </summary>
            public float Layer { get; set; } = 0.0f;

            /// <summary>
            ///     Gets or sets a value indicating whether the render object is visible.
            /// </summary>
            public bool Visible
            {
                get
                {
                    return this.VisibleCondition?.Invoke(this) ?? this.visible;
                }

                set
                {
                    this.visible = value;
                }
            }

            /// <summary>
            ///     Gets or sets the visible condition.
            /// </summary>
            public VisibleConditionDelegate VisibleCondition { get; set; }

            #endregion

            #region Properties

            /// <summary>
            ///     Gets the log.
            /// </summary>
            //protected ILog Log { get; } = AssemblyLogs.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                this.OnPreReset();
                this.Dispose(true);
            }

            /// <summary>
            ///     Determines if the render object has a valid layer.
            /// </summary>
            /// <returns>
            ///     The <see cref="bool" />.
            /// </returns>
            public bool HasValidLayer()
            {
                return this.Layer >= -5 && this.Layer <= 5;
            }

            /// <summary>
            ///     The draw event callback.
            /// </summary>
            public virtual void OnDraw()
            {
            }

            /// <summary>
            ///     The endscene event callback.
            /// </summary>
            public virtual void OnEndScene()
            {
            }

            /// <summary>
            ///     The post-reset event callback.
            /// </summary>
            public virtual void OnPostReset()
            {
            }

            /// <summary>
            ///     The pre-reset event callback.
            /// </summary>
            public virtual void OnPreReset()
            {
            }

            #endregion

            #region Methods

            /// <summary>
            ///     Subscribers to D3D9 reset event.
            /// </summary>
            internal void SubscribeToResetEvents()
            {
                Drawing.OnPreReset += this.DrawingOnOnPreReset;
                Drawing.OnPostReset += this.DrawingOnOnPostReset;
                AppDomain.CurrentDomain.DomainUnload += this.CurrentDomainOnDomainUnload;
            }

            /// <summary>
            ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
            /// </summary>
            /// <param name="disposing">
            ///     A value indicating whether the call is disposing managed resources.
            /// </param>
            protected virtual void Dispose(bool disposing)
            {
                if (this.IsDisposed)
                {
                    return;
                }

                Drawing.OnPreReset -= this.DrawingOnOnPreReset;
                Drawing.OnPostReset -= this.DrawingOnOnPostReset;
                AppDomain.CurrentDomain.DomainUnload -= this.CurrentDomainOnDomainUnload;

                this.IsDisposed = true;
            }

            private void CurrentDomainOnDomainUnload(object sender, EventArgs eventArgs)
            {
                this.OnPostReset();
            }

            private void DrawingOnOnPostReset(EventArgs args)
            {
                this.OnPostReset();
            }

            private void DrawingOnOnPreReset(EventArgs args)
            {
                this.OnPreReset();
            }

            #endregion
        }

        /// <summary>
        ///     Draws a sprite image.
        /// </summary>
        public class Sprite : RenderObject
        {
            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Sprite" /> class.
            /// </summary>
            /// <param name="bitmap">
            ///     The bitmap.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public Sprite(Bitmap bitmap, Vector2 position)
                : this()
            {
                this.UpdateTextureBitmap(bitmap, position);
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Sprite" /> class.
            /// </summary>
            /// <param name="texture">
            ///     The texture.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public Sprite(BaseTexture texture, Vector2 position)
                : this((Bitmap)Image.FromStream(BaseTexture.ToStream(texture, ImageFileFormat.Bmp)), position)
            {
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Sprite" /> class.
            /// </summary>
            /// <param name="stream">
            ///     The stream.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public Sprite(Stream stream, Vector2 position)
                : this(new Bitmap(stream), position)
            {
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Sprite" /> class.
            /// </summary>
            /// <param name="bytesArray">
            ///     The bytes array.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public Sprite(byte[] bytesArray, Vector2 position)
                : this((Bitmap)Image.FromStream(new MemoryStream(bytesArray)), position)
            {
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Sprite" /> class.
            /// </summary>
            /// <param name="fileLocation">
            ///     The file location.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public Sprite(string fileLocation, Vector2 position)
                : this()
            {
                if (!File.Exists(fileLocation))
                {
                    return;
                }

                this.UpdateTextureBitmap(new Bitmap(fileLocation), position);
            }

            private Sprite()
            {
                Game.OnUpdate += this.OnUpdate;
                this.SubscribeToResetEvents();
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The reset delegate.
            /// </summary>
            /// <param name="sprite">
            ///     The sprite.
            /// </param>
            public delegate void OnResetting(Sprite sprite);

            /// <summary>
            ///     The position delegate.
            /// </summary>
            /// <returns>
            ///     The <see cref="Vector2" />.
            /// </returns>
            public delegate Vector2 PositionDelegate();

            #endregion

            #region Public Events

            /// <summary>
            ///     The reset event.
            /// </summary>
            public event OnResetting OnReset;

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets the bitmap.
            /// </summary>
            public Bitmap Bitmap { get; set; }

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; } = SharpDX.Color.White;

            /// <summary>
            ///     Gets the height.
            /// </summary>
            public int Height => (int)(this.Bitmap.Height * this.Scale.Y);

            /// <summary>
            ///     Gets or sets a value indicating whether the sprite is visible.
            /// </summary>
            public bool IsVisible { get; set; } = true;

            /// <summary>
            ///     Gets or sets the position.
            /// </summary>
            public Vector2 Position
            {
                get
                {
                    return new Vector2(this.X, this.Y);
                }

                set
                {
                    this.X = (int)value.X;
                    this.Y = (int)value.Y;
                }
            }

            /// <summary>
            ///     Gets or sets the position update.
            /// </summary>
            public PositionDelegate PositionUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the rotation.
            /// </summary>
            public float Rotation { get; set; }

            /// <summary>
            ///     Gets or sets the scale.
            /// </summary>
            public Vector2 Scale { get; set; } = Vector2.One;

            /// <summary>
            ///     Gets the size.
            /// </summary>
            public Vector2 Size => new Vector2(this.Bitmap.Width, this.Bitmap.Height);

            /// <summary>
            ///     Gets or sets the sprite crop.
            /// </summary>
            public SharpDX.Rectangle? SpriteCrop { get; set; }

            /// <summary>
            ///     Gets or sets the texture.
            /// </summary>
            public Texture Texture { get; set; }

            /// <summary>
            ///     Gets the width.
            /// </summary>
            public int Width => (int)(this.Bitmap.Width * this.Scale.X);

            /// <summary>
            ///     Gets or sets the X-Axis of the position.
            /// </summary>
            public int X { get; set; }

            /// <summary>
            ///     Gets or sets the Y-Axis of the position.
            /// </summary>
            public int Y { get; set; }

            #endregion

            #region Properties

            private SharpDX.Direct3D9.Sprite DeviceSprite { get; } = new SharpDX.Direct3D9.Sprite(Device);

            private Texture OriginalTexture { get; set; }

            #endregion

            #region Public Methods and Operators

            /// <summary>
            ///     Complements the sprite.
            /// </summary>
            public void Complement() => this.SetSaturation(-1.0f);

            /// <summary>
            ///     Crops the sprite.
            /// </summary>
            /// <param name="x">
            ///     The X-axis of the position.
            /// </param>
            /// <param name="y">
            ///     The Y-axis of the position.
            /// </param>
            /// <param name="w">
            ///     The width.
            /// </param>
            /// <param name="h">
            ///     The height.
            /// </param>
            /// <param name="scale">
            ///     The scale.
            /// </param>
            public void Crop(int x, int y, int w, int h, bool scale = false)
                => this.Crop(new SharpDX.Rectangle(x, y, w, h), scale);

            /// <summary>
            ///     Crops the sprite.
            /// </summary>
            /// <param name="rect">
            ///     The rectangle.
            /// </param>
            /// <param name="scale">
            ///     The scale.
            /// </param>
            public void Crop(SharpDX.Rectangle rect, bool scale = false)
            {
                this.SpriteCrop = rect;

                if (scale)
                {
                    this.SpriteCrop = new SharpDX.Rectangle(
                                          (int)(this.Scale.X * rect.X),
                                          (int)(this.Scale.Y * rect.Y),
                                          (int)(this.Scale.X * rect.Width),
                                          (int)(this.Scale.Y * rect.Height));
                }
            }

            /// <summary>
            ///     Fades the sprite.
            /// </summary>
            public void Fade() => this.SetSaturation(.5f);

            /// <summary>
            ///     Grey scales the sprite.
            /// </summary>
            public void GrayScale()
            {
                this.SetSaturation(0.0f);
            }

            /// <summary>
            ///     Hides the sprite.
            /// </summary>
            public void Hide() => this.IsVisible = false;

            /// <inheritdoc />
            public override void OnEndScene()
            {
                if (this.DeviceSprite.IsDisposed || this.Texture.IsDisposed || this.Position.IsZero || !this.IsVisible)
                {
                    return;
                }

                try
                {
                    this.DeviceSprite.Begin();

                    var matrix = this.DeviceSprite.Transform;
                    var nMatrix = Matrix.Scaling(this.Scale.X, this.Scale.Y, 0) * Matrix.RotationZ(this.Rotation)
                                  * Matrix.Translation(this.Position.X, this.Position.Y, 0);
                    var rotation = Math.Abs(this.Rotation) > float.Epsilon
                                       ? new Vector3(this.Width / 2f, this.Height / 2f, 0)
                                       : (Vector3?)null;

                    this.DeviceSprite.Transform = nMatrix;
                    this.DeviceSprite.Draw(this.Texture, this.Color, this.SpriteCrop, rotation);
                    this.DeviceSprite.Transform = matrix;

                    this.DeviceSprite.End();
                }
                catch (Exception e)
                {
                    this.Reset();
                    Console.WriteLine(@"Common.Render.Sprite.OnEndScene: " + e);
                }
            }

            /// <inheritdoc />
            public override void OnPostReset()
            {
                try
                {
                    this.DeviceSprite?.OnResetDevice();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            /// <inheritdoc />
            public override void OnPreReset()
            {
                try
                {
                    this.DeviceSprite?.OnLostDevice();
                }
                catch (Exception)
                {
                    // ignored
                }
            }

            /// <summary>
            ///     Resets the sprite.
            /// </summary>
            public void Reset()
            {
                this.UpdateTextureBitmap(
                    (Bitmap)Image.FromStream(BaseTexture.ToStream(this.OriginalTexture, ImageFileFormat.Bmp)));

                this.OnReset?.Invoke(this);
            }

            /// <summary>
            ///     Sets the sprite saturation.
            /// </summary>
            /// <param name="saturation">
            ///     The saturation level.
            /// </param>
            public void SetSaturation(float saturation)
                => this.UpdateTextureBitmap(SaturateBitmap(this.Bitmap, saturation));

            /// <summary>
            ///     Shows the sprite.
            /// </summary>
            public void Show() => this.IsVisible = true;

            /// <summary>
            ///     Updates the texture.
            /// </summary>
            /// <param name="bitmap">
            ///     The bitmap.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            public void UpdateTextureBitmap(Bitmap bitmap, Vector2 position = default(Vector2))
            {
                if (!position.IsZero)
                {
                    this.Position = position;
                }

                this.Bitmap?.Dispose();
                this.Bitmap = bitmap;

                this.Texture = Texture.FromMemory(
                    Device,
                    (byte[])new ImageConverter().ConvertTo(bitmap, typeof(byte[])),
                    this.Width,
                    this.Height,
                    0,
                    Usage.None,
                    Format.A1,
                    Pool.Managed,
                    Filter.Default,
                    Filter.Default,
                    0);
                if (this.OriginalTexture == null)
                {
                    this.OriginalTexture = this.Texture;
                }
            }

            #endregion

            #region Methods

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (!this.DeviceSprite.IsDisposed)
                {
                    this.DeviceSprite.Dispose();
                }

                if (!this.Texture.IsDisposed)
                {
                    this.Texture.Dispose();
                }

                if (!this.OriginalTexture.IsDisposed)
                {
                    this.OriginalTexture.Dispose();
                }

                this.Bitmap = null;
            }

            private static Bitmap SaturateBitmap(Image original, float saturation)
            {
                const float RWeight = 0.3086f;
                const float GWeight = 0.6094f;
                const float BWeight = 0.0820f;

                var a = ((1.0f - saturation) * RWeight) + saturation;
                var b = (1.0f - saturation) * RWeight;
                var c = (1.0f - saturation) * RWeight;
                var d = (1.0f - saturation) * GWeight;
                var e = ((1.0f - saturation) * GWeight) + saturation;
                var f = (1.0f - saturation) * GWeight;
                var g = (1.0f - saturation) * BWeight;
                var h = (1.0f - saturation) * BWeight;
                var i = ((1.0f - saturation) * BWeight) + saturation;

                var newBitmap = new Bitmap(original.Width, original.Height);
                var gr = Graphics.FromImage(newBitmap);

                // ColorMatrix elements
                float[][] ptsArray =
                    {
                        new[] { a, b, c, 0, 0 }, new[] { d, e, f, 0, 0 }, new[] { g, h, i, 0, 0 },
                        new float[] { 0, 0, 0, 1, 0 }, new float[] { 0, 0, 0, 0, 1 }
                    };

                // Create ColorMatrix
                var clrMatrix = new ColorMatrix(ptsArray);

                // Create ImageAttributes
                var imgAttribs = new ImageAttributes();

                // Set color matrix
                imgAttribs.SetColorMatrix(clrMatrix, ColorMatrixFlag.Default, ColorAdjustType.Default);

                // Draw Image with no effects
                gr.DrawImage(original, 0, 0, original.Width, original.Height);

                // Draw Image with image attributes
                gr.DrawImage(
                    original,
                    new System.Drawing.Rectangle(0, 0, original.Width, original.Height),
                    0,
                    0,
                    original.Width,
                    original.Height,
                    GraphicsUnit.Pixel,
                    imgAttribs);
                gr.Dispose();

                return newBitmap;
            }

            private void OnUpdate(EventArgs args)
            {
                if (this.PositionUpdate != null)
                {
                    var pos = this.PositionUpdate();
                    this.X = (int)pos.X;
                    this.Y = (int)pos.Y;
                }
            }

            #endregion
        }

        /// <summary>
        ///     Object used to draw text on the screen.
        /// </summary>
        public class Text : RenderObject
        {
            #region Fields

            private string content;

            private int x;

            private int xCalcualted;

            private int y;

            private int yCalculated;

            #endregion

            #region Constructors and Destructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="Text" /> class.
            /// </summary>
            /// <param name="text">
            ///     The text.
            /// </param>
            /// <param name="x">
            ///     The X-axis.
            /// </param>
            /// <param name="y">
            ///     The Y-axis.
            /// </param>
            /// <param name="size">
            ///     The size.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="fontName">
            ///     The font name.
            /// </param>
            public Text(string text, int x, int y, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = x;
                this.y = y;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Text" /> class.
            /// </summary>
            /// <param name="text">
            ///     The text.
            /// </param>
            /// <param name="position">
            ///     The position.
            /// </param>
            /// <param name="size">
            ///     The size.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="fontName">
            ///     The font name.
            /// </param>
            public Text(string text, Vector2 position, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = (int)position.X;
                this.y = (int)position.Y;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Text" /> class.
            /// </summary>
            /// <param name="text">
            ///     The text.
            /// </param>
            /// <param name="unit">
            ///     The unit.
            /// </param>
            /// <param name="offset">
            ///     The offset.
            /// </param>
            /// <param name="size">
            ///     The size.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="fontName">
            ///     The font name.
            /// </param>
            public Text(
                string text,
                Obj_AI_Base unit,
                Vector2 offset,
                int size,
                ColorBGRA color,
                string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.Unit = unit;
                this.Offset = offset;

                var pos = unit.HPBarPosition + offset;
                this.x = (int)pos.X;
                this.y = (int)pos.Y;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Text" /> class.
            /// </summary>
            /// <param name="x">
            ///     The X-axis.
            /// </param>
            /// <param name="y">
            ///     The Y-axis.
            /// </param>
            /// <param name="text">
            ///     The text.
            /// </param>
            /// <param name="size">
            ///     The size.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="fontName">
            ///     The font name.
            /// </param>
            public Text(int x, int y, string text, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = x;
                this.y = y;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="Text" /> class.
            /// </summary>
            /// <param name="position">
            ///     The position.
            /// </param>
            /// <param name="text">
            ///     The text.
            /// </param>
            /// <param name="size">
            ///     The size.
            /// </param>
            /// <param name="color">
            ///     The color.
            /// </param>
            /// <param name="fontName">
            ///     The font name.
            /// </param>
            public Text(Vector2 position, string text, int size, ColorBGRA color, string fontName = "Calibri")
                : this(text, fontName, size, color)
            {
                this.x = (int)position.X;
                this.y = (int)position.Y;
            }

            private Text(string text, string fontName, int size, ColorBGRA color)
            {
                const FontPrecision OpDefault = FontPrecision.Default;
                const FontQuality QDefault = FontQuality.Default;

                var fontDesc = new FontDescription
                {
                    FaceName = fontName,
                    Height = size,
                    OutputPrecision = OpDefault,
                    Quality = QDefault
                };
                this.Font = new Font(Device, fontDesc);
                this.Color = color;
                this.Content = text;

                Game.OnUpdate += this.OnUpdate;
                this.SubscribeToResetEvents();
            }

            #endregion

            #region Delegates

            /// <summary>
            ///     The position delegate.
            /// </summary>
            /// <returns>
            ///     The <see cref="Vector2" />.
            /// </returns>
            public delegate Vector2 PositionDelegate();

            /// <summary>
            ///     The text delegate.
            /// </summary>
            /// <returns>
            ///     The <see cref="string" />.
            /// </returns>
            public delegate string TextDelegate();

            #endregion

            #region Public Properties

            /// <summary>
            ///     Gets or sets a value indicating whether the text is centered.
            /// </summary>
            public bool Centered { get; set; }

            /// <summary>
            ///     Gets or sets the color.
            /// </summary>
            public ColorBGRA Color { get; set; }

            /// <summary>
            ///     Gets or sets the content.
            /// </summary>
            public string Content
            {
                get
                {
                    return this.content;
                }

                set
                {
                    if (value != this.content && (!this.Font?.IsDisposed ?? false) && !string.IsNullOrEmpty(value))
                    {
                        var size = this.Font?.MeasureText(null, value, 0) ?? default(SharpDX.Rectangle);
                        this.Width = size.Width;
                        this.Height = size.Height;
                        this.Font?.PreloadText(value);
                    }

                    this.content = value;
                }
            }

            /// <summary>
            ///     Gets the height.
            /// </summary>
            public int Height { get; private set; }

            /// <summary>
            ///     Gets or sets the offset.
            /// </summary>
            public Vector2 Offset { get; set; }

            /// <summary>
            ///     Gets or sets a value indicating whether the text is outlined.
            /// </summary>
            public bool OutLined { get; set; }

            /// <summary>
            ///     Gets or sets the position update.
            /// </summary>
            public PositionDelegate PositionUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the text.
            /// </summary>
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Old API Compability.")]
            [Obsolete("Use Content property.")]
#pragma warning disable SA1300 // Element must begin with upper-case letter
            public string text
#pragma warning restore SA1300 // Element must begin with upper-case letter
            {
                get
                {
                    return this.Content;
                }

                set
                {
                    this.Content = value;
                }
            }

            /// <summary>
            ///     Gets or sets the text font description.
            /// </summary>
            public FontDescription TextFontDescription
            {
                get
                {
                    return this.Font.Description;
                }

                set
                {
                    this.Font.Dispose();
                    this.Font = new Font(Device, value);
                }
            }

            /// <summary>
            ///     Gets or sets the text update.
            /// </summary>
            public TextDelegate TextUpdate { get; set; }

            /// <summary>
            ///     Gets or sets the unit.
            /// </summary>
            public Obj_AI_Base Unit { get; set; }

            /// <summary>
            ///     Gets the width.
            /// </summary>
            public int Width { get; private set; }

            /// <summary>
            ///     Gets or sets the X-Axis of the postiion.
            /// </summary>
            public int X
            {
                get
                {
                    return this.PositionUpdate != null ? this.xCalcualted : this.x + this.XOffset;
                }

                set
                {
                    this.x = value;
                }
            }

            /// <summary>
            ///     Gets or sets the Y-Axis of the position.
            /// </summary>
            public int Y
            {
                get
                {
                    return this.PositionUpdate != null ? this.yCalculated : this.y + this.YOffset;
                }

                set
                {
                    this.y = value;
                }
            }

            #endregion

            #region Properties

            private Font Font { get; set; }

            private int XOffset => this.Centered ? -this.Width / 2 : 0;

            private int YOffset => this.Centered ? -this.Height / 2 : 0;

            #endregion

            #region Public Methods and Operators

            /// <inheritdoc />
            public override void OnEndScene()
            {
                try
                {
                    if ((this.Font == null || this.Font.IsDisposed) || string.IsNullOrEmpty(this.content))
                    {
                        return;
                    }

                    if (this.Unit != null && this.Unit.IsValid)
                    {
                        var pos = this.Unit.HPBarPosition + this.Offset;
                        this.X = (int)pos.X;
                        this.Y = (int)pos.Y;
                    }

                    var xP = this.X;
                    var yP = this.Y;

                    if (this.OutLined)
                    {
                        var outlineColor = new ColorBGRA(0, 0, 0, 255);
                        this.Font?.DrawText(null, this.Content, xP - 1, yP - 1, outlineColor);
                        this.Font?.DrawText(null, this.Content, xP + 1, yP + 1, outlineColor);
                        this.Font?.DrawText(null, this.Content, xP - 1, yP, outlineColor);
                        this.Font?.DrawText(null, this.Content, xP + 1, yP, outlineColor);
                    }

                    this.Font?.DrawText(null, this.Content, xP, yP, this.Color);
                }
                catch (Exception e)
                {
                    Console.WriteLine(@"Common.Render.Text.OnEndScene: " + e);
                }
            }

            /// <inheritdoc />
            public override void OnPostReset()
            {
                this.Font.OnResetDevice();
            }

            /// <inheritdoc />
            public override void OnPreReset()
            {
                this.Font.OnLostDevice();
            }

            #endregion

            #region Methods

            /// <inheritdoc />
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                /*
                if (disposing)
                {
                    this.Font?.Dispose();
                }//*/
                if (this.Font != null && !this.Font.IsDisposed)
                {
                    this.Font?.Dispose();
                }

                Game.OnUpdate -= this.OnUpdate;
            }

            private void OnUpdate(EventArgs args)
            {
                if (this.Visible)
                {
                    if (this.TextUpdate != null)
                    {
                        this.Content = this.TextUpdate();
                    }

                    if (this.PositionUpdate != null && !string.IsNullOrEmpty(this.Content))
                    {
                        var pos = this.PositionUpdate();
                        this.xCalcualted = (int)pos.X + this.XOffset;
                        this.yCalculated = (int)pos.Y + this.YOffset;
                    }
                }
            }

            #endregion
        }
    }

    /// <summary>
    ///     Provides extensions for fonts.
    /// </summary>
    public static class FontExtension
    {
        #region Static Fields

        /// <summary>
        ///     Collection of saved widths for each font.
        /// </summary>
        private static readonly Dictionary<Font, Dictionary<string, Rectangle>> Widths =
            new Dictionary<Font, Dictionary<string, Rectangle>>();

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     Measures the text.
        /// </summary>
        /// <param name="font">
        ///     The font.
        /// </param>
        /// <param name="sprite">
        ///     The sprite.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        /// <returns>
        ///     The <see cref="Rectangle" />.
        /// </returns>
        public static Rectangle MeasureText(this Font font, Sprite sprite, string text)
        {
            Dictionary<string, Rectangle> rectangles;
            if (!Widths.TryGetValue(font, out rectangles))
            {
                rectangles = new Dictionary<string, Rectangle>();
                Widths[font] = rectangles;
            }

            Rectangle rectangle;
            if (rectangles.TryGetValue(text, out rectangle))
            {
                return rectangle;
            }

            rectangle = font.MeasureText(sprite, text, 0);
            rectangles[text] = rectangle;
            return rectangle;
        }

        /// <summary>
        ///     Measures the text.
        /// </summary>
        /// <param name="font">
        ///     The font.
        /// </param>
        /// <param name="text">
        ///     The text.
        /// </param>
        /// <returns>
        ///     The <see cref="Rectangle" />.
        /// </returns>
        public static Rectangle MeasureText(this Font font, string text) => font.MeasureText(null, text);

        #endregion
    }
}