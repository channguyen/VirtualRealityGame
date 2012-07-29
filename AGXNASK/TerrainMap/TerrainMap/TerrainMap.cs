using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace TerrainMap {

    /// <summary>
    /// Generate and save two 2D textures:  heightTexture.png and colorTexture.png.
    /// File heightTexture.png stores a terrain's height values 0..255.
    /// File colorTexture.png stores the terrain's vertex color values.
    /// The files are saved in the execution directory.
    /// 
    /// Pressing 't' will toggle the display between the height and color
    /// texture maps.  As distributed, the heightTexture will look all black
    /// because the values range from 0 to 3.
    /// 
    /// The heightTexture will be mostly black since in the SK565v3 release there
    /// are two height areas:  grass plain and pyramid.  The pyramid (upper left corner)'
    /// will show grayscale values. 
    /// Grass height values range from 0..3 -- which is black in greyscale.
    /// 
    /// Note:  using grayscale in a texture to represent height constrains the 
    /// range of heights from 0 to 255.  Often you need to scale the values into this range
    /// before saving the texture.  In your world's terrain you can then scale these 
    /// values to the range you want.  This program does not scale since no values
    /// become greater than 255.
    /// 
    /// Normally one thinks of a 2D texture as having [u, v] coordinates. 
    /// In createHeightTexture() the height and in createColorTexture the color 
    /// values are created.
    /// The heightMap and colorMap used are [u, v] -- 2D.  They are converted to a 
    /// 1D textureMap1D[u*v] when the colorTexture's values are set.
    /// This is necessary because the method
    ///       newTexture.SetData<Color>(textureMap1D);
    /// requires a 1D array, not a 2D array.
    /// 
    /// Program design was influenced by Riemer Grootjans example 3.7
    /// Create a texture and save to file.
    /// In XNA 2.0 Game Programming Recipe:  A Problem-Solution Approach,
    /// pp 176-178, Apress, 2008.
    /// 
    /// updated for XNA4
    /// </summary>

    public class TerrainMap : Game {
        // textures should be powers of 2 for mip-mapping
        private const int TEXTURE_WIDTH = 512;
        private const int TEXTURE_HEIGHT = 512;
        private GraphicsDeviceManager graphics;
        private GraphicsDevice device;
        private SpriteBatch spriteBatch;
        private Texture2D heightTexture;
        private Texture2D colorTexture;
        private Color[,] colorMap;
        private Color[,] heightMap;
        private Color[] textureMap1D;  // hold the generated values for the texture.
        private Random random;
        private bool showHeight = false;
        private KeyboardState oldState;

        /// <summary>
        /// Constructor
        /// </summary>
        public TerrainMap() {
            graphics = new GraphicsDeviceManager(this);
            Window.Title = "Terrain Maps " + TEXTURE_WIDTH + " by " + TEXTURE_HEIGHT + " to change map 't'";
            Content.RootDirectory = "Content";
            random = new Random();
        }

        /// <summary>
        /// Set the window size based on the texture dimensions.
        /// </summary>

        protected override void Initialize() {
            // Game object exists, set its window size 
            graphics.PreferredBackBufferWidth = TEXTURE_WIDTH;
            graphics.PreferredBackBufferHeight = TEXTURE_HEIGHT;
            graphics.ApplyChanges();
            base.Initialize();
        }

        /// <summary>
        /// Create and save two textures:  
        ///   heightTexture.png 
        ///   colorTexture.png
        /// </summary>

        protected override void LoadContent() {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            device = graphics.GraphicsDevice;
            heightTexture = createHeightTexture();

            using (Stream stream = File.OpenWrite("heightTexture.png")) {
                heightTexture.SaveAsPng(stream, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            }

            colorTexture = createColorTexture();
            using (Stream stream = File.OpenWrite("colorTexture.png")) {
                colorTexture.SaveAsPng(stream, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            }
        }

        /// <summary>
        /// Create a height map as a texture of byte values (0..255) 
        /// that can be viewed as a grayscale bitmap.  
        /// The scene will have a plain of grass (heights 0..3) and
        /// a pyramid (height > 5).
        /// </summary>
        /// <returns>height texture</returns>
        private int[,] GenerateBrownianMotion() {
            int[,] heightMap = new int[TEXTURE_WIDTH, TEXTURE_HEIGHT];
            const int nCenter = 3;

            // initialize heightMap to 0
            for (int i = 0; i < TEXTURE_WIDTH; ++i) {
                for (int j = 0; j < TEXTURE_HEIGHT; ++j) {
                    heightMap[i, j] = 0;
                }
            }

            int[,] center = new int[nCenter, 2] { { 50, 50 }, { 265, 450 }, { 450, 50 } };
            int step = 7;
            int radius = 47;
            int initialCenter = random.Next(nCenter);
            int x = center[initialCenter, 0];
            int z = center[initialCenter, 1];
            int randomSign;

            for (int c = 0; c < 10000; ++c) {
                int xLowerRange = (x - radius) >= 0 ? (x - radius) : 0;
                int xUpperRange = (x + radius) < TEXTURE_WIDTH ? (x + radius) : (TEXTURE_WIDTH - 1);
                int zLowerRange = (z - radius) >= 0 ? (z - radius) : 0;
                int zUpperRange = (z + radius) < TEXTURE_HEIGHT ? (z + radius) : (TEXTURE_HEIGHT - 1);

                // add 1 to all heightMap[x, z] cells withing +/- radius
                for (int i = xLowerRange; i <= xUpperRange; ++i) {
                    for (int j = zLowerRange; j <= zUpperRange; ++j) {
                        if (IsWithinCircle(x, z, radius, i, j))
                            heightMap[i, j] += 1;
                    }
                }

                /*
                 * step range (-7, 7)
                 */
                step = -7 + random.Next(15);
                randomSign = random.Next(4);
                switch (randomSign) {
                    case 0:
                        x += step;
                        z += step;
                        break;

                    case 1:
                        x += step;
                        z -= step;
                        break;

                    case 2:
                        x -= step;
                        z += step;
                        break;

                    case 3:
                        x -= step;
                        z -= step;
                        break;
                }

                if (!IsInRange(x, z)) {
                    initialCenter = random.Next(nCenter);
                    x = center[initialCenter, 0];
                    z = center[initialCenter, 1];
                }
            }

            return heightMap;
        }

        private bool IsWithinCircle(int originX, int originY, int radius, int x, int y) {
            int x0 = Math.Abs(x - originX);
            int y0 = Math.Abs(y - originY);
            if (Math.Sqrt(x0 * x0 + y0 * y0) <= radius) {
                return true;
            }
            return false;
        }

        private bool IsInRange(int x, int z) {
            return (x >= 0 && x < TEXTURE_WIDTH && z >= 0 && z <= TEXTURE_HEIGHT);
        }

        private Texture2D createHeightTexture() {
            float height;
            Vector3 colorVec3;
            heightMap = new Color[TEXTURE_WIDTH, TEXTURE_HEIGHT];
            int[,] heightMapValue = GenerateBrownianMotion();

            // first create the "plain" heights
            for (int x = 0; x < TEXTURE_WIDTH; x++) {
                for (int z = 0; z < TEXTURE_HEIGHT; z++) {
                    // float version of byte value 
                    // height = ((float) random.Next(3))/255.0f; 

                    height = (heightMapValue[x, z] % 255) / 255.0f;
                    colorVec3 = new Vector3(height, height, height);
                    heightMap[x, z] = new Color(colorVec3);

                }
            }

            // Second create the pyramid with a base of 100 by 100 and a diagonal from center
            // to edge of 141 centered at (128, 128) with a number of steps (100 / 5).  
            // The "brick" height of each step is 20.
            int centerX = 128;
            int centerZ = 128;
            int pyramidSide = 100;
            int halfWidth = pyramidSide / 2;
            int pyramidDiagonal = (int)Math.Sqrt(2 * Math.Pow(pyramidSide, 2));
            int brick = 20;
            int stepSize = 5;
            int[,] pyramidHeight = new int[pyramidSide, pyramidSide];

            // initialize heights
            for (int x = 0; x < pyramidSide; x++)
                for (int z = 0; z < pyramidSide; z++)
                    pyramidHeight[x, z] = 0;

            // create heights for pyramid
            for (int s = 0; s < pyramidDiagonal; s += stepSize)
                for (int x = s; x < pyramidSide - s; x++)
                    for (int z = s; z < pyramidSide - s; z++)
                        pyramidHeight[x, z] += brick;

            // convert corresponding heightMap color to pyramidHeight equivalent color
            for (int x = 0; x < pyramidSide; x++) {
                for (int z = 0; z < pyramidSide; z++) {
                    height = pyramidHeight[x, z] / 255.0f; // convert to grayscale 0.0 to 255.0f
                    heightMap[centerX - halfWidth + x, centerZ - halfWidth + z] = new Color(new Vector3(height, height, height));
                }
            }

            // convert heightMap[,] to textureMap1D[]
            textureMap1D = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT];
            int i = 0;
            for (int x = 0; x < TEXTURE_WIDTH; x++) {
                for (int z = 0; z < TEXTURE_HEIGHT; z++) {
                    textureMap1D[i] = heightMap[x, z];
                    i++;
                }
            }

            // create the texture to return.       
            Texture2D newTexture = new Texture2D(device, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            newTexture.SetData<Color>(textureMap1D);
            return newTexture;
        }

        /// <summary>
        /// Create a color texture that will be used to "color" the terrain.
        /// Some comments about color that might explain some of the code in createColorTexture().
        /// Colors can be converted to vector4s.   vector4Value =  colorValue / 255.0
        /// color's (RGBA), color.ToVector4()
        /// Color.DarkGreen (R:0 G:100 B:0 A:255)    vector4 (X:0 Y:0.392 Z:0 W:1)  
        /// Color.Green     (R:0 G:128 B:0 A:255)    vector4 (X:0 Y:0.502 Z:0 W:1)  
        /// Color.OliveDrab (R:107 G:142 B:35 A:255) vector4 (X:0.420 Y:0.557 Z:0.137, W:1) 
        /// You can create colors with new Color(byte, byte, byte, byte) where byte = 0..255
        /// or, new Color(byte, byte, byte).
        /// 
        /// The Color conversion to Vector4 and back is used to add noise.
        /// You could just have Color.
        /// </summary>
        /// <returns>color texture</returns>

        private Texture2D createColorTexture() {
            int grassHeight = 5;
            Vector4 colorVec4 = new Vector4();
            colorMap = new Color[TEXTURE_WIDTH, TEXTURE_HEIGHT];
            for (int x = 0; x < TEXTURE_WIDTH; x++) {
                for (int z = 0; z < TEXTURE_HEIGHT; z++) {
                    // make random grass
                    if (heightMap[x, z].R < grassHeight) {
                        switch (random.Next(3)) {
                            case 0:
                                colorVec4 = new Color(0, 100, 0, 255).ToVector4();
                                break;
                            case 1:
                                colorVec4 = Color.Green.ToVector4();
                                break;
                            case 2:
                                colorVec4 = Color.OliveDrab.ToVector4();
                                break;
                        }
                    }

                    // color the pyramid based on height
                    else if (heightMap[x, z].R < 50) {
                        colorVec4 = Color.BurlyWood.ToVector4();
                    }
                    else if (heightMap[x, z].R < 90) {
                        colorVec4 = Color.Wheat.ToVector4();
                    }
                    else if (heightMap[x, z].R < 130) {
                        colorVec4 = Color.DarkGray.ToVector4();
                    }
                    else if (heightMap[x, z].R < 170) {
                        colorVec4 = Color.LightGray.ToVector4();
                    }
                    else if (heightMap[x, z].R < 200) {
                        colorVec4 = Color.SlateGray.ToVector4();
                    }
                    else if (heightMap[x, z].R < 230) {
                        colorVec4 = Color.White.ToVector4();
                    }
                    else if (heightMap[x, z].R < 250) {
                        colorVec4 = Color.WhiteSmoke.ToVector4();
                    }
                    else {
                        colorVec4 = Color.Snow.ToVector4();
                    }

                    // add some noise to the color
                    colorVec4 = colorVec4 + new Vector4((float)(random.NextDouble() / 20.0));
                    colorMap[x, z] = new Color(colorVec4);
                }
            }

            // convert colorMap[,] to textureMap1D[]
            textureMap1D = new Color[TEXTURE_WIDTH * TEXTURE_HEIGHT];
            int i = 0;
            for (int x = 0; x < TEXTURE_WIDTH; x++)
                for (int z = 0; z < TEXTURE_HEIGHT; z++) {
                    textureMap1D[i] = colorMap[x, z];
                    i++;
                }
            // create the texture to return.   
            Texture2D newTexture = new Texture2D(device, TEXTURE_WIDTH, TEXTURE_HEIGHT);
            newTexture.SetData<Color>(textureMap1D);
            return newTexture;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent() {
        }

        /// <summary>
        /// Process user keyboard input.
        /// Pressing 'T' or 't' will toggle the display between the height and color textures
        /// </summary>

        protected override void Update(GameTime gameTime) {
            KeyboardState keyboardState = Keyboard.GetState();

            if (keyboardState.IsKeyDown(Keys.Escape)) {
                Exit();
            }
            else if (Keyboard.GetState().IsKeyDown(Keys.T) && !oldState.IsKeyDown(Keys.T)) {
                showHeight = !showHeight;
            }

            // Update saved state
            oldState = keyboardState;
            base.Update(gameTime);
        }

        /// <summary>
        /// Display the textures.
        /// </summary>
        /// <param name="gameTime"></param>

        protected override void Draw(GameTime gameTime) {
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1, 0);
            spriteBatch.Begin();
            if (showHeight) {
                spriteBatch.Draw(heightTexture, Vector2.Zero, Color.White);
            }
            else {
                spriteBatch.Draw(colorTexture, Vector2.Zero, Color.White);
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        static void Main(string[] args) {
            using (var game = new TerrainMap()) {
                game.Run();
            }
        }
    }
}
