using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace AGXNASK {

    /// <summary>
    /// Represents the user / player interacting with the stage. 
    /// The Update(Gametime) handles both user keyboard and gamepad controller input.
    /// If there is a gamepad attached the keyboard inputs are not processed.
    /// </summary>

    public class Player : Agent {
        /*
         * Game pad
         */
        private GamePadState oldGamePadState;

        /*
         * Keyboard
         */
        private KeyboardState oldKeyboardState;

        /*
         * Rotate times
         */
        private int rotate;

        /*
         * Vertical times
         */
        private int vertical; 
          
        /*
         * Angle of rotation
         */
        private float angle;

        /*
         * Initial orientation matrix
         */
        private Matrix initialOrientation;

        public Player(Stage theStage, string label, Vector3 pos,  
                      Vector3 orientAxis, float radians, string meshFile)

            : base(theStage, label, pos, orientAxis, radians, meshFile) {  // change names for on-screen display of current camera

            first.Name = "First";
            follow.Name = "Follow";
            above.Name = "Above";

            IsCollidable = true;  // Player test collisions
            rotate = 0;
            angle = 0.01f;
            initialOrientation = agentObject.Orientation;
        }

        /// <summary>
        /// Handle player input that affects the player.
        /// See Stage.Update(...) for handling user input that affects
        /// how the stage is rendered.
        /// First check if gamepad is connected, if true use gamepad
        /// otherwise assume and use keyboard.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Update(GameTime gameTime) {
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            if (gamePadState.IsConnected) {
                if (gamePadState.Buttons.X == ButtonState.Pressed) {
                    stage.Exit();
                }
                else if (gamePadState.Buttons.A == ButtonState.Pressed && oldGamePadState.Buttons.A != ButtonState.Pressed) {
                    stage.NextCamera();
                }
                else if (gamePadState.Buttons.B == ButtonState.Pressed && oldGamePadState.Buttons.B != ButtonState.Pressed) {
                    stage.Fog = !stage.Fog;
                }
                else if (gamePadState.Buttons.RightShoulder == ButtonState.Pressed && oldGamePadState.Buttons.RightShoulder != ButtonState.Pressed) {
                    stage.FixedStepRendering = !stage.FixedStepRendering;
                }

                // allow more than one gamePadState to be pressed
                if (gamePadState.DPad.Up == ButtonState.Pressed) 
                    agentObject.Step++;

                if (gamePadState.DPad.Down == ButtonState.Pressed) 
                    agentObject.Step--;

                if (gamePadState.DPad.Left == ButtonState.Pressed) 
                    rotate++;

                if (gamePadState.DPad.Right == ButtonState.Pressed) 
                    rotate--;

                oldGamePadState = gamePadState;
            }
            else { // no gamepad assume use of keyboard
                KeyboardState keyboardState = Keyboard.GetState();
                if (keyboardState.IsKeyDown(Keys.R) && !oldKeyboardState.IsKeyDown(Keys.R))
                    agentObject.Orientation = initialOrientation;

                // allow more than one keyboardState to be pressed
                if (keyboardState.IsKeyDown(Keys.Up))
                    agentObject.Step++;

                if (keyboardState.IsKeyDown(Keys.Down))
                    agentObject.Step--;

                if (keyboardState.IsKeyDown(Keys.Left)) 
                    rotate++;
                
                if (keyboardState.IsKeyDown(Keys.Right)) 
                    rotate--;

                if (keyboardState.IsKeyDown(Keys.PageUp)) 
                    vertical++;    

                if (keyboardState.IsKeyDown(Keys.PageDown)) 
                    vertical--;

                // Update saved state.
                oldKeyboardState = keyboardState;    
            }

            agentObject.Yaw = rotate * angle;
            
            // call to Agent's Update method
            base.Update(gameTime);

            rotate = 0;
            agentObject.Step = 0;
            vertical = 0;
        }

        public new void AddTaggedTreasure() {
            base.AddTaggedTreasure();
        }

        public int GetTaggedTreasure() {
            return TaggedTreasures;
        }
    }
}
