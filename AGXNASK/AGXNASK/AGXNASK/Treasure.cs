using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;

namespace AGXNASK {
    public class Treasure : Model3D {
        private const string HIDDEN_TREASURE_FILENAME = "hidden_treasure";
        private const string FOUND_TREASURE_FILENAME = "found_treasure";

        private Object3D treasureObject = null;
        private Model closeStateModel;
        private Model openStateModel;
        private float angle = 0.01f;
        private float rotate = 0;
        private bool isOpen;

        public Treasure(Stage theStage, string label, string fileOfModel, Vector3 position, Vector3 up, float scale)
            : base(theStage, label, fileOfModel) {

            AddObject(position, up, scale);
            treasureObject = instance.First<Object3D>();
            isOpen = false;

            closeStateModel = stage.Content.Load<Model>(HIDDEN_TREASURE_FILENAME);
            openStateModel = stage.Content.Load<Model>(FOUND_TREASURE_FILENAME);
        }

        public bool IsOpen {
            get {
                return isOpen;
            }
        }

        public Vector3 Translation {
            get {
                return treasureObject.Translation;
            }
        }

        public Object3D TreasureObject {
            get {
                return treasureObject;
            }
        }

        public override void Update(GameTime gameTime) {
            if (IsOpen) {
                model = openStateModel;
                RotateInYaw();
            }
            else {
                model = closeStateModel;
            }
            
            base.Update(gameTime);
        }

        public void OpenIt() {
            isOpen = true;
        }

        private void RotateInYaw() {
            Vector3 startLocation = treasureObject.Translation;
            treasureObject.Orientation *= Matrix.CreateTranslation(-1 * treasureObject.Translation);      // move to origin
            rotate++;                                                                                     // increase rotation step
            treasureObject.Orientation *= Matrix.CreateRotationY(angle * rotate);                         // rotate
            treasureObject.Orientation *= Matrix.CreateTranslation(startLocation);                        // move back
        }

        private void MoveUp() {
            Vector3 startLocation = treasureObject.Translation;
            startLocation.Y = 500;
            treasureObject.Orientation *= Matrix.CreateTranslation(-1 * treasureObject.Translation);        // move to origin
            rotate++;                                                                                       
            treasureObject.Orientation *= Matrix.CreateRotationY(angle * rotate);                           // rotate
            treasureObject.Orientation *= Matrix.CreateTranslation(startLocation);                          // move back
        }

        public override void Draw(GameTime gameTime) {
            base.Draw(gameTime);
        }
    }
}
