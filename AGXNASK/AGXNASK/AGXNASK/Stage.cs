using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using System.Diagnostics;


namespace AGXNASK {

    /// <summary>
    /// Stage.cs  is the environment / framework for AGXNASK.
    /// 
    /// Stage declares and initializes the common devices, Inspector pane,
    /// and user input options.  Stage attempts to "hide" the infrastructure of AGXNASK
    /// for the developer.  It's subclass Stage is where the program specific aspects are
    /// declared and created.
    /// </summary>
    public class Stage : Game {
        public enum GameMode {
            NP_AGENT_EXPLORING,
            NP_AGENT_TREASURE_HUNTING,
            NP_AGENT_TREASURE_STOP,
            AGENT_FLOCKING_0,
            AGENT_FLOCKING_33,
            AGENT_FLOCKING_67,
            MAP_TOP_DOWN
        };

        /*
         * Game mode
         */
        public GameMode gameMode;

        /** 
         * Range is the length of the cubic volume of stage's terrain.
         * Each dimension (x, y, z) of the terrain is from 0 .. range (512)
         * The terrain will be centered about the origin when created.
         * Note some recursive terrain height generation algorithms (ie SquareDiamond)
         * work best with range = (2^n) - 1 values (513 = (2^9) -1).
         * ***************************************************************************
         */
        /*
         * Map size
         */
        protected const int RANGE = 512;

        /*
         * x and z spaces between vertex in the terrain
         */
        protected const int SPACING = 150; 

        /*
         * Terrain size
         */
        protected const int TERRAIN_SIZE = RANGE * SPACING;

        /*
         * Treasure snap distance
         */
        protected const int SNAP_DISTANCE = 1000;

        /**
         * XNA Graphics
         * ************
         */
        /*
         * Graphics device
         */
        protected GraphicsDeviceManager graphics;

        /*
         * Graphics context
         */
        protected GraphicsDevice display;    // graphics context

        /*
         * Default effect (shader)
         */
        protected BasicEffect effect;   

        /*
         * For Trace's displayStrings
         */
        protected SpriteBatch spriteBatch;   

        /*
         * Blending effect
         */
        protected BlendState blending;
        protected BlendState notBlending;


        /**
         * Stage Models 
         * *************
         */
        /*
         * A bounding sphere model
         */
        protected Model boundingSphere3D;    // a  bounding sphere model

        /*
         * A way point marker -- for paths
         */
        protected Model wayPoint3D;          

        /*
         * For drawing sphere
         */
        protected bool drawBoundingSpheres = false;
    
        /*
         * For fog effect
         */
        protected bool fog = false;

        /*
         * 60 updates / second
         */
        protected bool fixedStepRendering = true;   

        /**
         * Viewport and matrix for split screen display w/ Inspector.cs
         * *************************************************************
         */
        /*
         * Default view port
         */
        protected Viewport defaultViewport;

        /*
         * Top window
         */
        protected Viewport inspectorViewport;

        /*
         * Bottom window
         */
        protected Viewport sceneViewport;

        /*
         * Display matrix for top/bottom windows
         */
        protected Matrix sceneProjection;
        protected Matrix inspectorProjection;


        /**
         * Variables required for use with Inspector
         * *****************************************
         */

        /*
         * Number of lines / info display pane
         */
        protected const int InfoPaneSize = 5;   
    
        /* 
         * Number of total display strings
         */ 
        protected const int InfoDisplayStrings = 20;  
        protected Inspector inspector;
        protected SpriteFont inspectorFont;

        /**
         * Projection values
         * *****************
         */
        protected Matrix projection;
        protected float fov = (float) Math.PI / 4;
        protected float hither = 5.0f;
        protected float yon = TERRAIN_SIZE / 5.0f;
        protected float farYon = TERRAIN_SIZE * 1.3f;
        protected float fogStart = 4000;
        protected float fogEnd = 10000;
        protected bool yonFlag = true;

        /**
         * User event state
         * ****************
         */
        protected GamePadState oldGamePadState;
        protected KeyboardState oldKeyboardState;

        /**
         * Lights
         * ******
         */
        protected Vector3 lightDirection;
        protected Vector3 ambientColor;
        protected Vector3 diffuseColor;

        /**
         * Cameras
         * *******
         */
        
        /*
         * Collections of all cameras
         */
        protected List<Camera> camera = new List<Camera>();  

        /*
         * The current camera
         */
        protected Camera currentCamera;

        /*
         * Camera from top of player
         */
        protected Camera topDownCamera;

        /*
         * Reference to collection of camera
         */
        protected int cameraIndex = 0;

        /**
         * Required entities:
         * all AGXNASK programs have a Player and Terrain
         * **********************************************
         */

        /*
         * Player - controlled by user
         */
        protected Player player = null;

        /*
         * Non player - controlled by computer
         */
        protected NPAgent npAgent = null;

        /*
         * The terrain map
         */
        protected Terrain terrain = null;

        /*
         * Collection of collidable objects
         */
        protected List<Object3D> collidable = null;

        /*
         * Collection of all treasures in map
         */
        protected List<Treasure> treasures = null;

        /*
         * The number of treasures
         */
        public const int NUM_TREASURES = 5;

        /*
         * Pack of dog
         */
        private Pack dogs;

        /*
         * Flocking mode index
         */
        private int flockIndex = 0;

        /*
         * All modes
         */
        private Pack.FlockEnum[] flockModes = new Pack.FlockEnum[3];

        /**
         * Screen display information variables
         * ************************************
         */
        protected double fpsSecond;
        protected int draws;
        protected int updates;
        private Vector2 fontPos;
        private Vector2 winnerFontPos;


        /// <summary>
        /// Constructor for Stage
        /// </summary>
        public Stage() 
            : base() {

            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            graphics.SynchronizeWithVerticalRetrace = false;  // allow faster FPS

            // initialize directional light values
            lightDirection = Vector3.Normalize(new Vector3(-1.0f, -1.0f, -1.0f));
            ambientColor = new Vector3(0.4f, 0.4f, 0.4f);
            diffuseColor = new Vector3(0.2f, 0.2f, 0.2f);
            IsMouseVisible = true;  // make mouse cursor visible

            // initialize information display variables
            fpsSecond = 0.0;
            draws = 0;
            updates = 0;

            flockModes[0] = Pack.FlockEnum.FLOCK_0_PERCENT;
            flockModes[1] = Pack.FlockEnum.FLOCK_33_PERCENT;
            flockModes[2] = Pack.FlockEnum.FLOCK_67_PERCENT;

            gameMode = GameMode.MAP_TOP_DOWN;
        }

        public GameMode StageGameMode {
            get {
                return gameMode;
            }
            set {
                gameMode = value;
            }
        }

        public Vector3 AmbientLight {
            get {
                return ambientColor;
            }
        }

        public Model BoundingSphere3D {
            get {
                return boundingSphere3D;
            }
        }

        public List<Object3D> Collidable {
            get {
                return collidable;
            }
        }

        public List<Treasure> Treasures {
            get {
                return treasures;
            }
        }

        public Vector3 DiffuseLight {
            get {
                return diffuseColor;
            }
        }

        public GraphicsDevice Display {
            get {
                return display;
            }
        }

        public bool DrawBoundingSpheres {
            get {
                return drawBoundingSpheres;
            }
            set {
                drawBoundingSpheres = value;
                inspector.SetInfo(8, String.Format("Draw bounding spheres = {0}", drawBoundingSpheres));
            }
        }

        public float FarYon {
            get {
                return farYon;
            }
        }

        public bool FixedStepRendering {
            get {
                return fixedStepRendering;
            }
            set {
                fixedStepRendering = value;
                IsFixedTimeStep = fixedStepRendering;
            }
        }

        public bool Fog {
            get {
                return fog;
            }
            set {
                fog = value;
            }
        }

        public float FogStart {
            get {
                return fogStart;
            }
        }

        public float FogEnd {
            get {
                return fogEnd;
            }
        }

        public Vector3 LightDirection {
            get {
                return lightDirection;
            }
        }

        public Matrix Projection {
            get {
                return projection;
            }
        }

        public int Range {
            get {
                return RANGE;
            }
        }

        public BasicEffect SceneEffect {
            get {
                return effect;
            }
        }

        public int Spacing {
            get {
                return SPACING;
            }
        }

        public Terrain Terrain {
            get {
                return terrain;
            }
        }

        public int TerrainSize {
            get {
                return TERRAIN_SIZE;
            }
        }

        public Matrix View {
            get {
                return currentCamera.ViewMatrix;
            }
        }

        public Model WayPoint3D {
            get {
                return wayPoint3D;
            }
        }

        public bool YonFlag {
            get {
                return yonFlag;
            }
            set {
                yonFlag = value;
                if (yonFlag)
                    SetProjection(yon);
                else
                    SetProjection(farYon);
            }
        }


        // Methods

        public bool IsCollidable(Object3D obj3d) {
            if (collidable.Contains(obj3d))
                return true;
            return false;
        }

        /// <summary>
        /// Make sure that aMovableModel3D does not move off the terrain.
        /// Called from MovableModel3D.Update()
        /// The Y dimension is not constrained -- code commented out.
        /// </summary>
        /// <param name="aName"> </param>
        /// <param name="newLocation"></param>
        /// <returns>true if newLocation is within range</returns>
        public bool WithinRange(String aName, Vector3 newLocation) {
            if (newLocation.X < SPACING || newLocation.X > (TERRAIN_SIZE - 2 * SPACING) ||
                newLocation.Z < SPACING || newLocation.Z > (TERRAIN_SIZE - 2 * SPACING)) {
                inspector.SetInfo(14, String.Format("{0} can't move out of range", aName));
                inspector.SetInfo(14, String.Format("{0} can't move out of range", aName));
                return false;
            }
            else {
                inspector.SetInfo(14, " ");
                return true;
            }
        }

        public void AddCamera(Camera aCamera) {
            camera.Add(aCamera);
            cameraIndex++;
        }

        public void SetInfo(int index, string info) {
            inspector.SetInfo(index, info);
        }

        protected void SetProjection(float yonValue) {
            projection = Matrix.CreatePerspectiveFieldOfView(fov,
            graphics.GraphicsDevice.Viewport.AspectRatio, hither, yonValue);
        }

        /// <summary>
        /// Changing camera view for Agents will always set YonFlag false
        /// and provide a clipped view.
        /// </summary>
        public void NextCamera() {
            cameraIndex = (cameraIndex + 1) % camera.Count;
            currentCamera = camera[cameraIndex];

            // set the appropriate projection matrix
            YonFlag = false;
            SetProjection(farYon);

            if (cameraIndex == 1 || cameraIndex == 2 || cameraIndex == 3) {
                if (flockIndex == 0)
                    gameMode = GameMode.AGENT_FLOCKING_0;
                else if (flockIndex == 1)
                    gameMode = GameMode.AGENT_FLOCKING_33;
                else if (flockIndex == 2)
                    gameMode = GameMode.AGENT_FLOCKING_67;
            }
            else if (cameraIndex == 4 || cameraIndex == 5 || cameraIndex == 6) {
                gameMode = npAgent.NPAgentGameMode;
            }
            else {
                gameMode = GameMode.MAP_TOP_DOWN;
            }
        }

        /// <summary>
        /// Get the height of the surface containing stage coordinates (x, z)
        /// </summary>
        public float SurfaceHeight(float x, float z) {
            return terrain.SurfaceHeight((int) x / SPACING, (int) z / SPACING);
        }

        /// <summary>
        /// Set surface height base on X-Z 
        /// </summary>
        public void SetSurfaceHeight(Object3D anObject3D) {
            int spacing = Spacing;
            // Using Lerp
            int Ax = (int)anObject3D.Translation.X / spacing;
            int Az = (int)anObject3D.Translation.Z / spacing;

            int Bx = Ax + 1;
            int Bz = Az;

            int Cx = Ax;
            int Cz = Az + 1;

            int Dx = Ax + 1;
            int Dz = Az + 1;

            float Yx = anObject3D.Translation.X;
            float Yz = anObject3D.Translation.Z;

            Vector3 vector3A = new Vector3(Ax * spacing, terrain.SurfaceHeight(Ax, Az), Az * spacing);
            Vector3 vector3B = new Vector3(Bx * spacing, terrain.SurfaceHeight(Bx, Bz), Bz * spacing);
            Vector3 vector3C = new Vector3(Cx * spacing, terrain.SurfaceHeight(Cx, Cz), Cz * spacing);
            Vector3 vector3D = new Vector3(Dx * spacing, terrain.SurfaceHeight(Dx, Dz), Dz * spacing);

            float deltaYx;
            float deltaYz;
            float terrainHeight;
            if ((Yx - Ax * spacing + Yz - Az * spacing) <= spacing)  {
                deltaYx = Vector3.Lerp(vector3A, vector3B, (Yx - Ax * spacing) / (Bx * spacing - Ax * spacing)).Y;
                deltaYz = Vector3.Lerp(vector3A, vector3C, (Yz - Az * spacing) / (Cz * spacing - Az * spacing)).Y;
                terrainHeight = deltaYx + deltaYz - terrain.SurfaceHeight(Ax, Az);
            }
            else  {
                deltaYx = Vector3.Lerp(vector3C, vector3D, (Yx - Cx * spacing) / (Dx * spacing - Cx * spacing)).Y;
                deltaYz = Vector3.Lerp(vector3B, vector3D, (Yz - Bz * spacing) / (Dz * spacing - Bz * spacing)).Y;
                terrainHeight = deltaYx + deltaYz - terrain.SurfaceHeight(Dx, Dz);
            }

            anObject3D.Translation = new Vector3(anObject3D.Translation.X,
                                                  terrainHeight, anObject3D.Translation.Z);
        }

        /// <summary>
        /// Set correct surface height base on its plane
        /// </summary>
        /// <param name="anObject3D">A movable object</param>
        public void SetSmoothSurfaceHeight(Object3D anObject3D) {
            float terrainHeight = terrain.GetExactTerrainHeight(anObject3D.Translation.X, anObject3D.Translation.Z, 150);
            anObject3D.Translation = new Vector3(anObject3D.Translation.X, terrainHeight, anObject3D.Translation.Z);
        }

        public void SetBlendingState(bool state) {
            if (state)
                display.BlendState = blending;
            else
                display.BlendState = notBlending;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize() {
            // TODO: Add your initialization logic here
            base.Initialize();
        }


        /// <summary>
        /// Set GraphicDevice display and rendering BasicEffect effect.  
        /// Create SpriteBatch, font, and font positions.
        /// Creates the traceViewport to display information and the sceneViewport
        /// to render the environment.
        /// Create and add all DrawableGameComponents and Cameras.
        /// </summary>
        protected override void LoadContent() {
            // initialize graphic components
            display = graphics.GraphicsDevice;
            effect = new BasicEffect(display);

            SetUpInspector();
            SetUpBlending();

            // create and add stage components
            // You must have a TopDownCamera, BoundingSphere3D, Terrain, and Agent in your stage!
            // Place objects at a position, provide rotation axis and rotation radians.
            // All location vectors are specified relative to the center of the stage.
            // Create a top-down "Whole stage" camera view, make it first camera in collection.
            topDownCamera = new Camera(this, Camera.CameraEnum.TopDownCamera);
            camera.Add(topDownCamera);

            // load bounding sphere and way point models
            boundingSphere3D = Content.Load<Model>("boundingSphereV3");
            wayPoint3D = Content.Load<Model>("100x50x100Marker");

            // create required entities  
            collidable = new List<Object3D>();
            terrain = new Terrain(this, "terrain", "heightTexture", "colorTexture");
            Components.Add(terrain);

            // add players and treasures
            CreatePlayerAndNpAgent();
            CreateTreasures();
            CreatePackOfDogs();

            fontPos = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2 + 100);
            winnerFontPos = new Vector2(graphics.GraphicsDevice.Viewport.Width / 2, graphics.GraphicsDevice.Viewport.Height / 2 - 200);
        }

        /// <summary>
        /// Create chaser and evader characters for our world
        /// </summary>
        private void CreatePlayerAndNpAgent() {
            // player
            player = new Player(
                this,
                "Chaser",
                new Vector3(
                    LocationConstant.PLAYER_LOCATION_X * SPACING, 
                    terrain.SurfaceHeight(LocationConstant.PLAYER_LOCATION_X, LocationConstant.PLAYER_LOCATION_Z), 
                    LocationConstant.PLAYER_LOCATION_Z * SPACING), // close to hill to test terrain following
                new Vector3(0, 1, 0),                              // face looking diagonally across stage 
                0.80f,
                "redAvatarV3");

            player.SetSpeed(20);
            Components.Add(player);
            
            // np agent
            npAgent = new NPAgent(
                 this,
                "Evader",
                 new Vector3(
                     LocationConstant.NPAGENT_LOCATION_X * SPACING, 
                     terrain.SurfaceHeight(LocationConstant.NPAGENT_LOCATION_X, LocationConstant.NPAGENT_LOCATION_Z), 
                     LocationConstant.NPAGENT_LOCATION_Z * SPACING),
                 new Vector3(0, 1, 0),
                 0.0f,
                 "magentaAvatarV3");

            // add a non-player character
            Components.Add(npAgent);
        }
        /// <summary>
        /// Set up blending state and colors
        /// </summary>
        private void SetUpBlending() {
            // set blending for bounding sphere drawing
            blending = new BlendState();
            blending.ColorSourceBlend = Blend.SourceAlpha;
            blending.ColorDestinationBlend = Blend.InverseSourceAlpha;
            blending.ColorBlendFunction = BlendFunction.Add;
            notBlending = new BlendState();
            notBlending = display.BlendState;
        }

        /// <summary>
        /// Set up inspector 
        /// </summary>
        private void SetUpInspector() {
            // set up Inspector display
            spriteBatch = new SpriteBatch(display);      // Create a new SpriteBatch
            inspectorFont = Content.Load<SpriteFont>("Courier New");  // load font
            // create viewport
            defaultViewport = GraphicsDevice.Viewport;
            inspectorViewport = defaultViewport;
            sceneViewport = defaultViewport;
            inspectorViewport.Height = InfoPaneSize * inspectorFont.LineSpacing;
            inspectorProjection = 
                Matrix.CreatePerspectiveFieldOfView(
                    (float) Math.PI / 4.0f,
                    inspectorViewport.Width / inspectorViewport.Height, 
                    1.0f, 
                    200.0f);

            sceneViewport.Height = defaultViewport.Height - inspectorViewport.Height;
            sceneViewport.Y = inspectorViewport.Height;
            sceneProjection = 
                Matrix.CreatePerspectiveFieldOfView(
                    (float) Math.PI / 4.0f,
                    sceneViewport.Width / sceneViewport.Height, 
                    1.0f, 
                    1000.0f);

            inspectorProjection = Matrix.CreatePerspectiveFieldOfView((float) Math.PI / 4.0f,
               inspectorViewport.Width / inspectorViewport.Height, 1.0f, 200.0f);
            sceneViewport.Height = defaultViewport.Height - inspectorViewport.Height;
            sceneViewport.Y = inspectorViewport.Height;
            sceneProjection = Matrix.CreatePerspectiveFieldOfView((float) Math.PI / 4.0f,
               sceneViewport.Width / sceneViewport.Height, 1.0f, 1000.0f);
            // create Inspector display
            Texture2D inspectorBackground = Content.Load<Texture2D>("inspectorBackground");
            inspector = new Inspector(display, inspectorViewport, inspectorFont, Color.Black, inspectorBackground);

            // create information display strings
            inspector.SetInfo(0, "AGXNASKv4 ");
            inspector.SetInfo(1, "Press keyboard for input (not case sensitive 'H'  'h')");
            inspector.SetInfo(2, "Inspector toggles:  'H' help or info   'M'  matrix or info   'I'  displays next info pane.");
            inspector.SetInfo(3, "Arrow keys move the player in, out, left, or right.  'R' resets player to initial orientation.");
            inspector.SetInfo(4, "Stage toggles:  'B' bounding spheres, 'C' cameras, 'F' fog, 'T' updates, 'Y' yon");

            // initialize empty info strings
            for (int i = 5; i < 20; i++) {
                inspector.SetInfo(i, "  ");
            }

            inspector.SetInfo(5, "matrices info pane, initially empty");
            inspector.SetInfo(10, "first info pane, initially empty");
            inspector.SetInfo(15, "second info pane, initially empty");
        }

        /// <summary>
        /// Add a pack of dogs
        /// </summary>
        private void CreatePackOfDogs() {
            Random random = new Random();
            dogs = new Pack(this, "dog", "dogV3", player.AgentObject);
            Components.Add(dogs);
            int count = 0;
            for (int x = -9; x < 10; x += 6) {
                for (int z = -3; z < 4; z += 6) {
                    float scale = (float)(0.5 + random.NextDouble());
                    float xPos = (384 + x) * SPACING;
                    float zPos = (384 + z) * SPACING;
                    dogs.AddObject(
                        new Vector3(xPos, terrain.SurfaceHeight((int)xPos / SPACING, (int)zPos / SPACING), zPos),
                        new Vector3(0, 1, 0), 0.0f,
                        new Vector3(scale, scale, scale));
                    count++;
                }
            }

            dogs.FlockSize = count;
            dogs.InitializeFlocing();
        }

        /// <summary>
        /// Add treasures to game
        /// </summary>
        private void CreateTreasures() {
            // create 5 treasures
            treasures = new List<Treasure>();
            treasures.Add(
                new Treasure(
                    this, "Treasure_1", "hidden_treasure", 
                    new Vector3(
                        LocationConstant.TREASURE_1_X * SPACING, 
                        terrain.SurfaceHeight(LocationConstant.TREASURE_1_X, LocationConstant.TREASURE_1_Z) + 50, 
                        LocationConstant.TREASURE_1_Z * SPACING), new Vector3(0, 1, 0), 1.0f));
            treasures[0].IsCollidable = true;
            Components.Add(treasures[0]);

            treasures.Add(
                new Treasure(
                    this, "Treasure_2", "hidden_treasure", 
                    new Vector3(
                        LocationConstant.TREASURE_2_X * SPACING, 
                        terrain.SurfaceHeight(LocationConstant.TREASURE_2_X, LocationConstant.TREASURE_2_Z) + 50, 
                        LocationConstant.TREASURE_2_Z * SPACING), new Vector3(0, 1, 0), 1.0f));
            treasures[1].IsCollidable = true;
            Components.Add(treasures[1]);

            treasures.Add(
                new Treasure(
                    this, "Treasure_3", "hidden_treasure", 
                    new Vector3(
                        LocationConstant.TREASURE_3_X * SPACING, 
                        terrain.SurfaceHeight(LocationConstant.TREASURE_3_X, LocationConstant.TREASURE_3_Z) + 50, 
                        LocationConstant.TREASURE_3_Z * SPACING), new Vector3(0, 1, 0), 1.0f));
            treasures[2].IsCollidable = true;
            Components.Add(treasures[2]);

            treasures.Add(
                new Treasure(
                    this, "Treasure_4", "hidden_treasure", 
                    new Vector3(
                        LocationConstant.TREASURE_4_X * SPACING, 
                        terrain.SurfaceHeight(LocationConstant.TREASURE_4_X, LocationConstant.TREASURE_4_Z) + 50, 
                        LocationConstant.TREASURE_4_Z * SPACING), new Vector3(0, 1, 0), 1.0f));
            treasures[3].IsCollidable = true;
            Components.Add(treasures[3]);

            treasures.Add(
                new Treasure(
                    this, "Treasure_5", "hidden_treasure", 
                    new Vector3(
                        LocationConstant.TREASURE_5_X * SPACING, 
                        terrain.SurfaceHeight(LocationConstant.TREASURE_5_X, LocationConstant.TREASURE_5_Z) + 50, 
                        LocationConstant.TREASURE_5_Z * SPACING), new Vector3(0, 1, 0), 1.0f));
            treasures[4].IsCollidable = true;
            Components.Add(treasures[4]);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
            // TODO: Unload any non ContentManager content here
        }

        private void UpdateTreasureState() {
            foreach (Treasure t in treasures) {
                if (Vector3.Distance(t.Translation, npAgent.AgentObject.Translation) < SNAP_DISTANCE) {
                    if (!t.IsOpen) {
                        npAgent.AddTaggedTreasure();
                        t.OpenIt();
                    }
                }
                else if (Vector3.Distance(t.Translation, player.AgentObject.Translation) < SNAP_DISTANCE) {
                    if (!t.IsOpen) {
                        player.AddTaggedTreasure();
                        t.OpenIt();
                    }
                }
                else {
                    // do nothing
                }
            }
        }

        private void UpdateInspector(TimeSpan ts) {
            if (fpsSecond >= 1.0) {
                inspector.SetInfo(10,
                   String.Format("{0} camera    Game time {1:D2}::{2:D2}::{3:D2}    {4:D} Updates/Seconds {5:D} Draws/Seconds",
                      currentCamera.Name, ts.Hours, ts.Minutes, ts.Seconds, updates.ToString(), draws.ToString()));
                draws = updates = 0;
                fpsSecond = 0.0;

                inspector.SetInfo(11,
                   string.Format("Player:   Location ({0,5:f0},{1,3:f0},{2,5:f0})  Looking at ({3,5:f2},{4,5:f2},{5,5:f2})",
                   player.AgentObject.Translation.X, player.AgentObject.Translation.Y, player.AgentObject.Translation.Z,
                   player.AgentObject.Forward.X, player.AgentObject.Forward.Y, player.AgentObject.Forward.Z));

                inspector.SetInfo(12,
                   string.Format("npAgent:  Location ({0,5:f0},{1,3:f0},{2,5:f0})  Looking at ({3,5:f2},{4,5:f2},{5,5:f2})",
                   npAgent.AgentObject.Translation.X, npAgent.AgentObject.Translation.Y, npAgent.AgentObject.Translation.Z,
                   npAgent.AgentObject.Forward.X, npAgent.AgentObject.Forward.Y, npAgent.AgentObject.Forward.Z));

                inspector.SetInfo(13, "Player tagged treasures: " + player.GetTaggedTreasure() + ", NpAgent tagged treasures: " + npAgent.GetTaggedTreasure());
                inspector.SetMatrices("player", "npAgent", player.AgentObject.Orientation, npAgent.AgentObject.Orientation);
            }
        }

        /// <summary>
        /// Process user keyboard and game-pad events that relate to the render 
        /// state of the stage
        /// </summary>
        private void HandleUserInputs() {
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            if (gamePadState.IsConnected) {
                if (gamePadState.Buttons.X == ButtonState.Pressed) {
                    Exit();
                }
                else if (gamePadState.Buttons.A == ButtonState.Pressed && oldGamePadState.Buttons.A != ButtonState.Pressed) {
                    NextCamera();
                }
                else if (gamePadState.Buttons.B == ButtonState.Pressed && oldGamePadState.Buttons.B != ButtonState.Pressed) {
                    Fog = !Fog;
                }
                else if (gamePadState.Buttons.Y == ButtonState.Pressed && oldGamePadState.Buttons.Y != ButtonState.Pressed) {
                    FixedStepRendering = !FixedStepRendering;
                }
                oldGamePadState = gamePadState;
            }
            else {
                KeyboardState keyboardState = Keyboard.GetState();
                if (keyboardState.IsKeyDown(Keys.Escape)) {
                    Exit();
                }
                else if (keyboardState.IsKeyDown(Keys.B) && !oldKeyboardState.IsKeyDown(Keys.B)) {
                    DrawBoundingSpheres = !DrawBoundingSpheres;
                }
                else if (keyboardState.IsKeyDown(Keys.C) && !oldKeyboardState.IsKeyDown(Keys.C)) {
                    NextCamera();
                }
                else if (keyboardState.IsKeyDown(Keys.F) && !oldKeyboardState.IsKeyDown(Keys.F)) {
                    Fog = !Fog;
                }
                else if (keyboardState.IsKeyDown(Keys.H) && !oldKeyboardState.IsKeyDown(Keys.H)) {
                    inspector.ShowHelp = !inspector.ShowHelp;
                    inspector.ShowMatrices = false;
                }
                else if (keyboardState.IsKeyDown(Keys.I) && !oldKeyboardState.IsKeyDown(Keys.I)) {
                    inspector.ShowInfo();
                }
                else if (keyboardState.IsKeyDown(Keys.M) && !oldKeyboardState.IsKeyDown(Keys.M)) {
                    inspector.ShowMatrices = !inspector.ShowMatrices;
                    inspector.ShowHelp = false;
                }
                else if (keyboardState.IsKeyDown(Keys.T) && !oldKeyboardState.IsKeyDown(Keys.T)) {
                    FixedStepRendering = !FixedStepRendering;
                }
                else if (keyboardState.IsKeyDown(Keys.Y) && !oldKeyboardState.IsKeyDown(Keys.Y)) {
                    YonFlag = !YonFlag;
                }
                else if (keyboardState.IsKeyDown(Keys.N) && !oldKeyboardState.IsKeyDown(Keys.N)) {
                    YonFlag = !YonFlag;  
                }
                else if (keyboardState.IsKeyDown(Keys.P) && !oldKeyboardState.IsKeyDown(Keys.P)) {
                    flockIndex = (flockIndex + 1) % 3;
                    dogs.FlockMode = flockModes[flockIndex];
                    ShowFlockMode(dogs.FlockMode);
                    if (gameMode == GameMode.AGENT_FLOCKING_0 || gameMode == GameMode.AGENT_FLOCKING_33 || gameMode == GameMode.AGENT_FLOCKING_67) {
                        if (flockIndex == 0)
                            gameMode = GameMode.AGENT_FLOCKING_0;
                        else if (flockIndex == 1)
                            gameMode = GameMode.AGENT_FLOCKING_33;
                        else if (flockIndex == 2)
                            gameMode = GameMode.AGENT_FLOCKING_67;
                    }
                }
                oldKeyboardState = keyboardState;    // Update saved state.
            }
        }

        private void ShowFlockMode(Pack.FlockEnum flockingMode) {
            if (flockingMode == Pack.FlockEnum.FLOCK_0_PERCENT) {
                inspector.SetInfo(18, "0% flocking");
            }

            if (flockingMode == Pack.FlockEnum.FLOCK_33_PERCENT) {
                inspector.SetInfo(18, "33% flocking");
            }

            if (flockingMode == Pack.FlockEnum.FLOCK_67_PERCENT) {
                inspector.SetInfo(18, "67% flocking");
            }
        }

        /// <summary>
        /// Uses an Inspector to display update and display information to player.
        /// All user input that affects rendering of the stage is processed either
        /// from the gamepad or keyboard.
        /// See Player.Update(...) for handling of user events that affect the player.
        /// The current camera's place is updated after all other GameComponents have 
        /// been updated.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime) {
            // set info pane values
            fpsSecond += gameTime.ElapsedGameTime.TotalSeconds;
            updates++;
            TimeSpan ts = gameTime.TotalGameTime;

            // display flocking mode
            ShowFlockMode(dogs.FlockMode);

            // update inspector
            UpdateInspector(ts);

            // process user's inputs 
            HandleUserInputs();

            // update treasures
            UpdateTreasureState();

            // update all GameComponents and DrawableGameComponents
            base.Update(gameTime);  
            currentCamera.UpdateViewMatrix();
        }

        private void DrawGameModeInfo() {
            // make sure we update np agent game mode
            if (gameMode == GameMode.NP_AGENT_EXPLORING || gameMode == GameMode.NP_AGENT_TREASURE_HUNTING || gameMode == GameMode.NP_AGENT_TREASURE_STOP) {
                gameMode = npAgent.NPAgentGameMode;
            }

            string output = "undefined";

            switch (gameMode) {
                case GameMode.AGENT_FLOCKING_0: 
                    output = "Agent - Flocking %0";
                    break;

                case GameMode.AGENT_FLOCKING_33:
                    output = "Agent - Flocking %33";
                    break;

                case GameMode.AGENT_FLOCKING_67:
                    output = "Agent - Flocking %67";
                    break;

                case GameMode.NP_AGENT_EXPLORING:
                    output = "NpAgent - Exploring";
                    break;

                case GameMode.NP_AGENT_TREASURE_HUNTING:
                    output = "NpAgent - Treasure Hunting";
                    break;

                case GameMode.NP_AGENT_TREASURE_STOP:
                    output = "NpAgent - Finished";
                    break;

                case GameMode.MAP_TOP_DOWN:
                    output = " ";
                    break;
            }

            spriteBatch.Begin();
            Vector2 fontOrigin = inspectorFont.MeasureString(output) / 2;
            spriteBatch.DrawString(inspectorFont, output, fontPos, Color.White, 0, fontOrigin, 2.0f, SpriteEffects.None, 0.5f);
            if (IsAllTreasureTagged())
                DrawWinner();
            spriteBatch.End();
        }

        private bool IsAllTreasureTagged() {
            foreach (Treasure t in treasures) {
                if (!t.IsOpen)
                    return false;
            }

            return true;
        }

        private void DrawWinner() {
            String winner = "";
            if (player.GetTaggedTreasure() > npAgent.GetTaggedTreasure())
                winner = "Winner is Player with " + player.GetTaggedTreasure() + " treasures tagged!";
            else
                winner = "Winner is NpAgent with " + npAgent.GetTaggedTreasure() + " treasures tagged!";

            Vector2 fontOrigin = inspectorFont.MeasureString(winner) / 2;
            spriteBatch.DrawString(inspectorFont, winner, winnerFontPos, Color.Red, 0, fontOrigin, 2.0f, SpriteEffects.None, 0.5f);
        }

        /// <summary>
        /// Draws information in the display viewport.
        /// Resets the GraphicsDevice's context and makes the sceneViewport active.
        /// Has Game invoke all DrawableGameComponents Draw(GameTime).
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime) {
            draws++;
            display.Viewport = defaultViewport; //sceneViewport;
            display.Clear(Color.CornflowerBlue);

            // Draw into inspectorViewport
            display.Viewport = inspectorViewport;
            spriteBatch.Begin();
            inspector.Draw(spriteBatch);
            spriteBatch.End();

            // need to restore state changed by spriteBatch
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // draw objects in stage 
            display.Viewport = sceneViewport;
            display.RasterizerState = RasterizerState.CullNone;

            base.Draw(gameTime);  // draw all GameComponents and DrawableGameComponents

            // draw game mode
            DrawGameModeInfo();
        }

    }
}