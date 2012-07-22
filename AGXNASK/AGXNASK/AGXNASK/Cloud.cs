using System;
using Microsoft.Xna.Framework;

namespace AGXNASK {

    /// <summary>
    /// An example of how to override the MovableModel3D's Update(GameTime) to 
    /// animate a model's objects.  The actual update of values is done by calling 
    /// each instance object and setting its (Pitch, Yaw, Roll, or Step property. 
    /// Then call base.Update(GameTime) method of MovableModel3D to apply transformations.
    /// </summary>
    public class Cloud : MovableModel3D {
        private Random random;

        // Constructor
        public Cloud(Stage stage, string label, string meshFile)
            : base(stage, label, meshFile) {
            random = new Random();
        }

        public override void Update(GameTime gameTime) {
            foreach (Object3D obj in instance) {
                obj.Yaw = 0.0f;
                if (random.NextDouble() < 0.34) {
                    obj.Yaw = 0.01f;
                    obj.UpdateMovableObject();
                }
            }
            base.Update(gameTime);
        }

    }
}
