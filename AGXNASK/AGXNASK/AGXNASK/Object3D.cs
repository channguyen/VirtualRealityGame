using System;
using Microsoft.Xna.Framework;

namespace AGXNASK {

    /// <summary>
    /// Defines location and orientation.
    /// Object's orientation is a 4 by 4 XNA Matrix. 
    /// Object's location is Vector3 describing it position in the stage.
    /// Has good examples of C# Properties (Location, Orientation, Right, Up, and At).
    /// These properties show how the 4 by 4 XNA Matrix values are
    /// stored and what they represent.
    /// Properties Right, Up, and At get and set values in matrix orientation.
    /// Right, the object's local X axis, is the lateral unit vector.
    /// Up, the object's local Y axis, is the vertical unit vector.
    /// At, the object's local Z axis, is the forward unit vector.
    /// 
    /// 2/14/2012  last changed
    /// </summary>

    public class Object3D {

        /*
         * The model of this object
         */
        private Model3D model;
        
        /* 
         * String identifier
         */
        private string name;               
        
        /*
         * Framework stage object
         */
        private Stage stage;                

        /*
         * Object's orientation 
         */
        private Matrix orientation;         

        /*
         * Object's scale factor
         */
        private Vector3 scales;             

        /*
         * Changes in rotation
         */
        private float pitch;
        private float roll;
        private float yaw;
        
        /*
         * Values for stepping
         */
        private int step;
        private int stepSize;
        
        /*
         * The object bounding sphere
         */
        private Vector3 objectBoundingSphereCenter;
        
        /*
         * Its radius
         */
        private float objectBoundingSphereRadius = 0.0f;

        /*
         * Its matrix
         */
        private Matrix objectBoundingSphereWorld;

        // constructors

        /// <summary>
        /// Object that places and orients itself.
        /// </summary>
        /// <param name="theStage"> the stage containing object </param> 
        /// <param name="aModel">how the object looks</param> 
        /// <param name="label"> name of object </param> 
        /// <param name="position"> position in stage </param> 
        /// <param name="orientAxis"> axis to orient on </param> 
        /// <param name="radians"> orientation rotation </param> 
        public Object3D(Stage theStage, Model3D aModel, string label, Vector3 position, Vector3 orientAxis, float radians) {
            scales = Vector3.One;
            stage = theStage;
            model = aModel;
            name = label;

            step = 1;
            stepSize = 10;
            pitch = yaw = roll = 0.0f;

            orientation = Matrix.Identity;
            orientation *= Matrix.CreateFromAxisAngle(orientAxis, radians);
            orientation *= Matrix.CreateTranslation(position);

            ScaleObjectBoundingSphere();
        }

        /// <summary>
        /// Object that places, orients, and scales itself.
        /// </summary>
        /// <param name="theStage"> the stage containing object </param> 
        /// <param name="aModel">how the object looks</param> 
        /// <param name="label"> name of object </param> 
        /// <param name="position"> position in stage </param> 
        /// <param name="orientAxis"> axis to orient on </param> 
        /// <param name="radians"> orientation rotation </param> 
        /// <param name="objectScales">re-scale Model3D </param>  
        public Object3D(Stage theStage, Model3D aModel, string label, Vector3 position, Vector3 orientAxis, float radians, Vector3 objectScales) {
            stage = theStage;
            name = label;
            scales = objectScales;
            model = aModel;
            step = 1;
            stepSize = 10;
            pitch = yaw = roll = 0.0f;

            // set object orientation
            /*
             *  ---------------------
             *  | Rx | Ry | Rz | Rw |  right 
             *  ---------------------
             *  | Ux | Uy | Uz | Uw |  up
             *  ---------------------
             *  | Bx | By | Bz | Bw |  backward = -forward
             *  ---------------------
             *  | Tx | Ty | Yz | Tw |  translation
             *  ---------------------
             */
            orientation = Matrix.Identity;
            orientation *= Matrix.CreateScale(scales);
            orientation *= Matrix.CreateFromAxisAngle(orientAxis, radians);
            orientation *= Matrix.CreateTranslation(position);

            // scale it's bounding sphere
            ScaleObjectBoundingSphere();
        }

        public string Name {
            get {
                return name;
            }
            set {
                name = value;
            }
        }

        public Matrix ObjectBoundingSphereWorld {
            get {
                return objectBoundingSphereWorld;
            }
        }

        public Matrix Orientation {
            get {
                return orientation;
            }
            set {
                orientation = value;
            }
        }

        public Vector3 Translation {
            get {
                return orientation.Translation;
            }
            set {
                orientation.Translation = value;
            }
        }

        public Vector3 Up {
            get {
                return orientation.Up;
            }
            set {
                orientation.Up = value;
            }
        }

        public Vector3 Down {
            get {
                return orientation.Down;
            }
            set {
                orientation.Down = value;
            }
        }

        public Vector3 Right {
            get {
                return orientation.Right;
            }
            set {
                orientation.Right = value;
            }
        }

        public Vector3 Left {
            get {
                return orientation.Left;
            }
            set {
                orientation.Left = value;
            }
        }

        public Vector3 Forward {
            get {
                return orientation.Forward;
            }
            set {
                orientation.Forward = value;
            }
        }

        public Vector3 Backward {
            get {
                return orientation.Backward;
            }
            set {
                orientation.Backward = value;
            }
        }

        public float Pitch {
            get {
                return pitch;
            }
            set {
                pitch = value;
            }
        }

        public float Yaw {
            get {
                return yaw;
            }
            set {
                yaw = value;
            }
        }

        public float Roll {
            get {
                return roll;
            }
            set {
                roll = value;
            }
        }

        public int Step {
            get {
                return step;
            }
            set {
                step = value;
            }
        }

        public int StepSize {
            get {
                return stepSize;
            }
            set {
                stepSize = value;
            }
        }

        public void Reset() {
            pitch = roll = yaw = 0;
            step = 0;
        }

        // Methods

        /// <summary>
        ///  Does the Object3D's new position collide with any Collidable Object3Ds ?
        /// </summary>
        /// <param name="position"> position Object3D wants to move to </param>
        /// <returns> true when there is a collision </returns>
        private bool Collision(Vector3 position) {
            foreach (Object3D obj3d in stage.Collidable) {
                if (!this.Equals(obj3d) &&
                      Vector3.Distance(position, obj3d.Translation) <= objectBoundingSphereRadius + obj3d.objectBoundingSphereRadius)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Scale the object's bounding sphere from
        /// its radius
        /// </summary>
        private void ScaleObjectBoundingSphere() {
            if (scales.X >= scales.Y && scales.X >= scales.Z)
                objectBoundingSphereRadius = model.BoundingSphereRadius * scales.X;
            else if (scales.Y >= scales.X && scales.Y >= scales.Z)
                objectBoundingSphereRadius = model.BoundingSphereRadius * scales.Y;
            else
                objectBoundingSphereRadius = model.BoundingSphereRadius * scales.Z;
        }

        /// <summary>
        /// Update the object's orientation matrix so that it is rotated to 
        /// look at target. AGXNASK is terrain based -- so all turn are wrt flat XZ plane.
        /// AGXNASK assumes models are made to "look" -Z 
        /// </summary>
        /// <param name="target"> to look at</param>
        public void TurnToFace(Vector3 target) {
            Vector3 axis;
            Vector3 toTarget;
            Vector3 toObj;

            double radian;
            double aCosDot;

            // put both vector on the XZ plane of Y == 0
            toObj = new Vector3(Translation.X, 0, Translation.Z);
            target = new Vector3(target.X, 0, target.Z);
            toTarget = target - toObj;

            // normalize
            toObj.Normalize();
            toTarget.Normalize();

            // make sure vectors are not co-linear by a little nudge in X and Z
            if (toTarget == toObj || Vector3.Negate(toTarget) == toObj) {
                toTarget.X += 0.05f;
                toTarget.Z += 0.05f;
                toTarget.Normalize();
            }

            // determine axis for rotation 
            axis = Vector3.Cross(toTarget, Forward);  // order of arguments mater
            axis.Normalize();

            // get cosine of rotation
            aCosDot = Math.Acos(Vector3.Dot(toTarget, Forward));

            // test and adjust direction of rotation into radians
            if (aCosDot == 0) {
                radian = Math.PI * 2;
            }
            else if (aCosDot == Math.PI) {
                radian = Math.PI;
            }
            else if (axis.X + axis.Y + axis.Z >= 0) {
                radian = (float) (2 * Math.PI - aCosDot);
            }
            else {
                radian = -aCosDot;
            }

            // display stage info
            stage.SetInfo(19, string.Format("radian to rotate = {0,5:f2}, axis for rotation ({1,5:f2}, {2,5:f2}, {3,5:f2})", radian, axis.X, axis.Y, axis.Z));

            if (Double.IsNaN(radian)) {  // validity check, this should not happen
                stage.SetInfo(19, "radian NaN");
                return;
            }
            else {  // valid radian perform transformation
                Vector3 objectLocation = Translation;                               // save location
                orientation *= Matrix.CreateTranslation(-1 * objectLocation);       // translate back to the origin
                // all terrain rotations are really on Y
                orientation *= Matrix.CreateFromAxisAngle(axis, (float) radian);    // rotate
                // correct for flipped from negative axis of rotation
                orientation.Up = Vector3.Up;  
                orientation *= Matrix.CreateTranslation(objectLocation);            // translate back to its location
            }
        }

        /// <summary>
        /// Update the object's orientation matrix so that it is rotated to 
        /// look at target. AGXNASK is terrain based -- so all turn are wrt flat XZ plane.
        /// AGXNASK assumes models are made to "look" -Z 
        /// </summary>
        /// <param name="target"> to look at</param>
        public void TurnAway(Vector3 target) {
            Vector3 axis;
            Vector3 toTarget;
            Vector3 toObj;

            double radian;
            double aCosDot;

            // put both vector on the XZ plane of Y == 0
            toObj = new Vector3(Translation.X, 0, Translation.Z);
            target = new Vector3(target.X, 0, target.Z);
            toTarget = target - toObj;

            // normalize
            toObj.Normalize();
            toTarget.Normalize();

            // make sure vectors are not co-linear by a little nudge in X and Z
            if (toTarget == toObj || Vector3.Negate(toTarget) == toObj) {
                toTarget.X += 0.05f;
                toTarget.Z += 0.05f;
                toTarget.Normalize();
            }

            // determine axis for rotation 
            axis = Vector3.Cross(toTarget, Forward);  // order of arguments mater
            axis.Normalize();

            // get cosine of rotation
            aCosDot = Math.Acos(Vector3.Dot(toTarget, Forward));

            // test and adjust direction of rotation into radians
            if (aCosDot == 0) {
                radian = Math.PI * 2;
            }
            else if (aCosDot == Math.PI) {
                radian = Math.PI;
            }
            else if (axis.X + axis.Y + axis.Z >= 0) {
                radian = (float)(2 * Math.PI - aCosDot);
            }
            else {
                radian = -aCosDot;
            }

            // flip the angle
            radian = -1 * radian;

            // display stage info
            stage.SetInfo(19, string.Format("radian to rotate = {0,5:f2}, axis for rotation ({1,5:f2}, {2,5:f2}, {3,5:f2})", radian, axis.X, axis.Y, axis.Z));

            if (Double.IsNaN(radian)) {  // validity check, this should not happen
                stage.SetInfo(19, "radian NaN");
                return;
            }
            else {  // valid radian perform transformation
                Vector3 objectLocation = Translation;                               // save location
                orientation *= Matrix.CreateTranslation(-1 * objectLocation);       // translate back to the origin
                // all terrain rotations are really on Y
                orientation *= Matrix.CreateFromAxisAngle(axis, (float)radian);    // rotate
                // correct for flipped from negative axis of rotation
                orientation.Up = Vector3.Up;
                orientation *= Matrix.CreateTranslation(objectLocation);            // translate back to its location
            }
        }


        /// <summary>
        /// Here we move to a new location for testing 
        /// collision. Then we move back no matter collision
        /// has occurred or not
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsCollideWith(Vector3 location) {
            Vector3 startLocation = Translation;

            Orientation *= Matrix.CreateTranslation(-1 * Translation);   // move to origin
            Orientation *= Matrix.CreateRotationY(yaw);                  // rotate in yaw   - y
            Orientation *= Matrix.CreateRotationX(pitch);                // rotate in pitch - x
            Orientation *= Matrix.CreateRotationZ(roll);                 // rotate in roll  - z

            Orientation *= Matrix.CreateTranslation(location);           // fake move

            // if collision, reset location and return
            if (Collision(location)) {
                Orientation *= Matrix.CreateTranslation(-1 * location);  // move back to the origin
                Orientation *= Matrix.CreateTranslation(startLocation);  // now move to previous location
                return true;
            }

            Orientation *= Matrix.CreateTranslation(-1 * location);      // move back to the origin
            Orientation *= Matrix.CreateTranslation(startLocation);      // now move to previous location
            return false;
        }

        /// <summary>
        /// The location is first saved and the model is translated
        /// to the origin before any rotations are applied.  Objects rotate about their
        /// center.  After rotations, the location is reset and updated iff it is not
        /// outside the range of the stage (stage.withinRange(String name, Vector3 location)).  
        /// When movement would exceed the stage's boundaries the model is not moved 
        /// and a message is displayed.
        /// </summary>
        public void UpdateMovableObject() {
            Vector3 startLocation = Translation;
            Vector3 stopLocation = Translation;

            Orientation *= Matrix.CreateTranslation(-1 * Translation);   // move to origin
            Orientation *= Matrix.CreateRotationY(yaw);                  // rotate in yaw   - y
            Orientation *= Matrix.CreateRotationX(pitch);                // rotate in pitch - x
            Orientation *= Matrix.CreateRotationZ(roll);                 // rotate in roll  - z

            stopLocation += ((step * stepSize) * Forward);               // move forward    

            // if collision, reset location and return
            if (model.IsCollidable && Collision(stopLocation)) {
                Orientation *= Matrix.CreateTranslation(startLocation);  // don't move
                return;
            }

            // no collision test if move on terrain
            if (stage.WithinRange(this.Name, stopLocation)) {
                Orientation *= Matrix.CreateTranslation(stopLocation);   // move forward
            }
            else { // off terrain, reset location
                Orientation *= Matrix.CreateTranslation(startLocation);  // don't move
            }
        }

        public void UpdateBoundingSphere() {
            objectBoundingSphereCenter = Translation;  // set center to instance
            objectBoundingSphereWorld = Matrix.CreateScale(objectBoundingSphereRadius);
            objectBoundingSphereWorld *= Matrix.CreateTranslation(objectBoundingSphereCenter);
        }
    }
}