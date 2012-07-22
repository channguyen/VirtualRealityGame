/*
 * MatrixInfo.cs 
 * 
 * Shows the view and world matrix values for a combination of view, rotate, and translate
 * transformations.
 * 
 * Uses Inspector.cs for information display.
 * 
 * View matrices:  top (10, 75, 10),  front (5, 50, 50),  right (50, 50, 10)
 * World matrices:  identity,  0.6 radians X rotation,  0.4 Y rotation, 1.0 Z rotation
 * 
 * Key mappings used to select view, rotation and translation:
 *    view:    T top       F front     R right
 *    rotate:  s identity  X 0.6       Y 0.4    Z 1.0 
 *    reset:   F12 -- top view, rotate identity, translate none  
 *    translate:  use arrow keys for up +Z, down -Z, right, and left   
 * 
 * Updated to XNA4
 * 
 * Mike Barnes
 * 2/ 8 /2012  last updated
 */

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

namespace MatrixInfo {

class MatrixInfo : Microsoft.Xna.Framework.Game {
   // variables required for use with Inspector
   private const int InfoPaneSize = 5;   // number of lines / info display pane
   private const int InfoDisplayStrings = 20;  // number of total display strings
   private Inspector inspector;
   private GraphicsDeviceManager graphics;
   private GraphicsDevice display;
   private BasicEffect effect;
   private SpriteBatch spriteBatch;
   private SpriteFont inspectorFont;
   // Viewports for split screen display
   private Viewport defaultViewport;
   private Viewport inspectorViewport, sceneViewport;     // top and bottom "windows"
   // matrices
   private Matrix sceneProjection, inspectorProjection;
   // matrices
   private Matrix world;
   private Matrix [] view;  // front, right, top;
   private Matrix [] rotate; // identity, rotate: 33x, 22y, 56z;
   private Matrix [] translate; // (0,0,0) (0,0,10), (0,0,-10) (20,0,0), (-20,0,0),
   private int nVertices = 9;
   private VertexPositionColor[] vertex;
   private int viewIndex, rotateIndex, translateIndex;
   // input and display
   KeyboardState oldKeyboardState; 
   bool stateChange;
   private string [] viewString = {"top", "front", "right"};
   private string [] rotateString = {"no", "0.6 on X", "0.4 on Y", "1.0 on Z"};
   private string [] translateString = { "No", "Positive Z", "Negative Z", "Right", "Left"};
   
   public MatrixInfo() {
      graphics = new GraphicsDeviceManager(this);
      Content.RootDirectory = "Content";
      IsMouseVisible = true;  // make mouse cursor visible
      viewIndex = rotateIndex = translateIndex = 0;
      // create colored vertices
      vertex = new VertexPositionColor[nVertices];
      vertex[0] = new VertexPositionColor(new Vector3(  0,  0,   0), Color.Red);
      vertex[1] = new VertexPositionColor(new Vector3( 20,  0,   0), Color.Red);
      vertex[2] = new VertexPositionColor(new Vector3(  0,  0,   0), Color.Green);
      vertex[3] = new VertexPositionColor(new Vector3(  0, 20,   0), Color.Green);
      vertex[4] = new VertexPositionColor(new Vector3(  0,  0,   0), Color.Blue);
      vertex[5] = new VertexPositionColor(new Vector3(  0,  0,  20), Color.Blue);
      vertex[6] = new VertexPositionColor(new Vector3(-20, -5,   0), Color.Gray);
      vertex[7] = new VertexPositionColor(new Vector3( 20, -5,   0), Color.Gray);
      vertex[8] = new VertexPositionColor(new Vector3(  0, -5,  20), Color.LightGray);
      view = new Matrix[3];
      view[0] = Matrix.CreateLookAt(new Vector3( 10, 100,  10), Vector3.Zero, Vector3.Forward); // top
      view[1] = Matrix.CreateLookAt(new Vector3(  5,  50, 100), Vector3.Zero, Vector3.Up);      // front
      view[2] = Matrix.CreateLookAt(new Vector3(100,  50,  10), Vector3.Zero, Vector3.Up);      // right
      rotate = new Matrix[4];
      rotate[0] = Matrix.Identity;
      rotate[1] = Matrix.CreateRotationX(0.6f); // about 33 degrees
      rotate[2] = Matrix.CreateRotationY(0.4f); // about 22 degrees
      rotate[3] = Matrix.CreateRotationZ(1.0f); // about 56 degrees
      translate = new Matrix[5];
      translate[0] = Matrix.Identity;
      translate[1] = Matrix.CreateTranslation(  0, 0, -5);  // back
      translate[2] = Matrix.CreateTranslation(  0, 0,  5);  // forward
      translate[3] = Matrix.CreateTranslation( 25, 0,  0);  // right
      translate[4] = Matrix.CreateTranslation(-25, 0,  0);  // left
      // initial world matrix
      world = rotate[rotateIndex] * translate[translateIndex];
      }

   protected override void LoadContent() {
      display = graphics.GraphicsDevice;
      effect = new BasicEffect(display);
      spriteBatch = new SpriteBatch(display);
      effect.VertexColorEnabled = true;
      inspectorFont = Content.Load<SpriteFont>("Courier New");  // load font
      // viewports
      defaultViewport = GraphicsDevice.Viewport;
      inspectorViewport = defaultViewport;
      sceneViewport = defaultViewport;
      inspectorViewport.Height = InfoPaneSize * inspectorFont.LineSpacing;
      inspectorProjection = Matrix.CreatePerspectiveFieldOfView((float) Math.PI/4.0f,
         inspectorViewport.Width/inspectorViewport.Height, 1.0f, 200.0f);
      sceneViewport.Height = defaultViewport.Height - inspectorViewport.Height;
      sceneViewport.Y = inspectorViewport.Height;
      sceneProjection = Matrix.CreatePerspectiveFieldOfView((float) Math.PI/4.0f,
         sceneViewport.Width /sceneViewport.Height, 1.0f, 1000.0f);
      // create Inspector display
      Texture2D inspectorBackground = Content.Load<Texture2D>("inspectorBackground");
      inspector = new Inspector(display, inspectorViewport, inspectorFont, Color.Black, inspectorBackground);
      // create information display strings
      // help strings
      inspector.setInfo(0, "MatrixInfo's help example.");
      inspector.setInfo(1, "Press:");
      inspector.setInfo(2, "toggles:  H || h help    M || m  matrices   I || i  next info pane.");
      inspector.setInfo(3, "X, Y, Z rotates model,  R, F, T sets view, arrow keys translate left/right, in/out ");
      inspector.setInfo(4, "S resets the rotations and V reset to initial view.");
      // create empty info strings
      for (int i = 5; i < 20; i++)
         inspector.setInfo(i, " ");
      // turn off miscellaneous info pane for this program
      inspector.ShowMatrices = false;
      // Load example model for scene here 
      // initialize matrices
      effect.View = view[viewIndex];
      effect.World = rotate[rotateIndex] * translate[translateIndex];
      }

   protected override void Update(GameTime gameTime) {
      stateChange = false; // assume no input
      KeyboardState keyboardState = Keyboard.GetState();
      if (keyboardState.IsKeyDown(Keys.Escape)) Exit();
      // key event handlers needed for Inspector
      // set help display on
      else if (keyboardState.IsKeyDown(Keys.H) && !oldKeyboardState.IsKeyDown(Keys.H)) {
         inspector.ShowHelp = ! inspector.ShowHelp;
         inspector.ShowMatrices = false;
         inspector.setInfo(12, "H pressed"); 
         stateChange = true;}
      else if (keyboardState.IsKeyDown(Keys.M) && !oldKeyboardState.IsKeyDown(Keys.M)) {
         inspector.ShowMatrices = ! inspector.ShowMatrices;
         inspector.ShowHelp = false;
         inspector.setInfo(12, "M pressed");
         stateChange = true;}
      // set info display on
      else if (keyboardState.IsKeyDown(Keys.I) && !oldKeyboardState.IsKeyDown(Keys.I)) {
         inspector.showInfo();
         inspector.setInfo(12, "I pressed"); 
         stateChange = true; }
      // set miscellaneous display on  -- not used in this example, handler deleted
      // reset
      else if (keyboardState.IsKeyDown(Keys.V) && !oldKeyboardState.IsKeyDown(Keys.V)) {
         translateIndex = 0; viewIndex = 0; rotateIndex = 0; stateChange = true;
         inspector.setInfo(12, "V pressed");  }
      // view
      else if (Keyboard.GetState().IsKeyDown(Keys.T) && !oldKeyboardState.IsKeyDown(Keys.T)) {
         viewIndex = 0; stateChange = true;
         inspector.setInfo(12, "T pressed"); }
      else if (Keyboard.GetState().IsKeyDown(Keys.F) && !oldKeyboardState.IsKeyDown(Keys.F)){
         viewIndex = 1; stateChange = true;
         inspector.setInfo(12, "F pressed"); }
      else if (Keyboard.GetState().IsKeyDown(Keys.R) && !oldKeyboardState.IsKeyDown(Keys.R)){
         viewIndex = 2; stateChange = true;
         inspector.setInfo(12, "R pressed");}
      // rotate
      else if (Keyboard.GetState().IsKeyDown(Keys.S) && !oldKeyboardState.IsKeyDown(Keys.S)){
         rotateIndex = 0; stateChange = true;
         inspector.setInfo(12, "S pressed");}
      else if (Keyboard.GetState().IsKeyDown(Keys.X) && !oldKeyboardState.IsKeyDown(Keys.X)){
         rotateIndex = 1; stateChange = true;
         inspector.setInfo(12, "X pressed"); }
      else if (Keyboard.GetState().IsKeyDown(Keys.Y) && !oldKeyboardState.IsKeyDown(Keys.Y)){
         rotateIndex = 2; stateChange = true;
         inspector.setInfo(12, "Y pressed"); }
      else if (Keyboard.GetState().IsKeyDown(Keys.Z) && !oldKeyboardState.IsKeyDown(Keys.Z)){
         rotateIndex = 3; stateChange = true;
         inspector.setInfo(12, "Z pressed");}
      // translate 
      else if (keyboardState.IsKeyDown(Keys.Up) && !oldKeyboardState.IsKeyDown(Keys.Up)){
         translateIndex = 1; stateChange = true;
         inspector.setInfo(12, "Up arrow pressed");}
      else if (keyboardState.IsKeyDown(Keys.Down) && !oldKeyboardState.IsKeyDown(Keys.Down)){
         translateIndex = 2; stateChange = true; 
         inspector.setInfo(12, "Down arrow pressed");}
      else if (keyboardState.IsKeyDown(Keys.Right) && !oldKeyboardState.IsKeyDown(Keys.Right)){
         translateIndex = 3; stateChange = true;
         inspector.setInfo(12, "Right arrow pressed");}
      else if (keyboardState.IsKeyDown(Keys.Left) && !oldKeyboardState.IsKeyDown(Keys.Left)){
         translateIndex = 4; stateChange = true;
         inspector.setInfo(12, "Left arrow pressed");}

      oldKeyboardState = Keyboard.GetState();

      if (stateChange) {
         effect.View = view[viewIndex];
         world = rotate[rotateIndex] * translate[translateIndex];
         // set effect.World in draw so surface doesn't change
         inspector.setMatrices("World", "View", world, view[viewIndex]);
         inspector.setInfo(11, 
            String.Format("{0} translation, {1} rotation, seen from the {2} view",
               translateString[translateIndex], rotateString[rotateIndex],
               viewString[viewIndex]));
         }
      base.Update(gameTime);
      }

   protected override void Draw(GameTime gameTime) {
      display.Viewport = defaultViewport;
      display.Clear(Color.CornflowerBlue);
      // Draw into inspectorViewport
      display.Viewport = inspectorViewport;
      spriteBatch.Begin();
      inspector.Draw(spriteBatch);
      spriteBatch.End();
      // need to restore render state changed by spriteBatch
      // see documentation on SpriteBatch.End() -- all of these probably not needed.
      GraphicsDevice.BlendState = BlendState.Opaque;
      GraphicsDevice.DepthStencilState = DepthStencilState.Default;
      GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
 //     display.SamplerStates[0].AddressU = TextureAddressMode.Wrap;
  //    display.SamplerStates[0].AddressV = TextureAddressMode.Wrap;
      // draw scene 
      display.Viewport = sceneViewport;
  //    display.RasterizerState = RasterizerState.CullNone;
      // draw triangle floor  
      effect.World = Matrix.Identity;
      effect.Projection = sceneProjection;
      foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
         pass.Apply();
         display.DrawUserPrimitives<VertexPositionColor>(
            PrimitiveType.TriangleList, vertex, 6, 1);
            }
      // draw axis lines
      effect.World = world;
      foreach (EffectPass pass in effect.CurrentTechnique.Passes) {
         pass.Apply();
         display.DrawUserPrimitives<VertexPositionColor>(
            PrimitiveType.LineList, vertex, 0, 3);
         }
      base.Draw(gameTime);
      }

   static void Main(string[] args) {
      using (MatrixInfo game = new MatrixInfo()) {
         game.Run(); }
      }
   }
 }
