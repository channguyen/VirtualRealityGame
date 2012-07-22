using Microsoft.Xna.Framework;

namespace AGXNASK {

    /// <summary>
    /// Defines the Update(GameTime) method for moving an instance of a model.
    /// Model instances are Object3Ds.
    /// Movements: 
    ///   step (forward), stepSize, vertical (+ up, - down), 
    ///   yaw, pitch, and roll.
    /// While abstract, subclasses invoke their base.Update(GameTime) to apply
    /// the inherited movement step values.
    /// </summary>

    public class MovableModel3D : Model3D {

        //   public MovableModel3D(Stage theStage, string label, Vector3 position, 
        //   Vector3 orientAxis, float radians, string meshFile)
        //      : base(theStage, label, position, orientAxis, radians, meshFile) 
        public MovableModel3D(Stage theStage, string label, string meshFile)
            : base(theStage, label, meshFile) {

        }

        public void reset() {
            foreach (Object3D obj in instance) 
                obj.Reset();
        }

        ///<summary>
        ///  pass through
        ///</summary>
        // override virtual DrawableGameComponent methods                   
        public override void Update(GameTime gameTime) {
            foreach (Object3D obj in instance)  
                obj.UpdateBoundingSphere();

            base.Update(gameTime);
        }
    }
}
