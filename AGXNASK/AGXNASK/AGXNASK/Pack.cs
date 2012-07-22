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
    /// Pack represents a "flock" of MovableObject3D's Object3Ds.
    /// Usually the "player" is the leader and is set in the Stage's LoadContent().
    /// With no leader, determine a "virtual leader" from the flock's members.
    /// Model3D's inherited List<Object3D> instance holds all members of the pack.
    /// </summary>
    public class Pack : MovableModel3D {
        public enum FlockEnum {
            FLOCK_67_PERCENT,
            FLOCK_33_PERCENT,
            FLOCK_0_PERCENT
        }

        /*
         * Neighbor distance
         */
        private const double NEIGHBOR_RADIUS = 4000.0;

        /*
         * The closest possible distance between neighbor
         */
        private const double SEPARATION_DISTANCE = 300.0;

        /*
         * The minimum distance between boid
         */
        private const double OUTER_RADIUS = 5000.0;

        /* 
         * The radius of the field of view 
         */
        private const double INNER_RADIUS = 500.0;

        /*
         * The maximum angle
         */
        private const double MAX_ANGLE = Math.PI / 4;


        /*
         * Signal flocking
         */
        private bool flogFlag = true;

        /*
         * The leader
         */
        private Object3D leader;

        /*
         * Random generator
         */
        private Random random = null;

        /*
         * Flocking mode
         */
        private FlockEnum flockMode;

        /*
         * Flocking counter 
         */
        private int counter;


        public FlockEnum FlockMode {
            get {
                return flockMode;
            }
            set {
                flockMode = value;
            }
        }

        public Object3D Leader {
            get {
                return leader;
            }
            set {
                leader = value;
            }
        }

        /// <summary>
        /// Construct a leaderless pack.
        /// </summary>
        /// <param name="theStage"> the scene</param>
        /// <param name="label"> name of pack</param>
        /// <param name="meshFile"> model of pack instance</param>
        public Pack(Stage theStage, string label, string meshFile)
            : base(theStage, label, meshFile) {

            random = new Random();
            isCollidable = true;
            leader = null;
            flockMode = FlockEnum.FLOCK_0_PERCENT;
        }

        /// <summary>
        /// Construct a pack with an Object3D leader
        /// </summary>
        /// <param name="theStage"> the scene </param>
        /// <param name="label"> name of pack</param>
        /// <param name="meshFile"> model of a pack instance</param>
        /// <param name="aLeader"> Object3D alignment and pack center </param>
        public Pack(Stage theStage, string label, string meshFile, Object3D aLeader)
            : base(theStage, label, meshFile) {

            random = new Random();
            isCollidable = true;
            leader = aLeader;
            flockMode = FlockEnum.FLOCK_0_PERCENT;
        }

        /// <summary>
        /// Check if a boid neighbor is within a wide field of view
        /// of another boid
        /// </summary>
        /// <param name="boid">the current boid</param>
        /// <param name="neighbor">its neighbor</param>
        /// <returns>true/false</returns>
        private bool IsInWiderView(Object3D boid, Object3D neighbor) {
            // outside circle is automatically false
            if (Vector3.Distance(boid.Translation, neighbor.Translation) > OUTER_RADIUS) {
                return false;
            }

            // put them in x-z plane
            Vector3 boidForward = new Vector3(boid.Forward.X, 0, boid.Forward.Z);
            boidForward.Normalize();

            Vector3 neighborLocation = new Vector3(neighbor.Translation.X, 0, neighbor.Translation.Z);
            neighborLocation.Normalize();

            // this is either left 135 degree or right 135 degree
            float deg135 = (float)Math.PI * 135.0f / 180.0f;
            float dot = Vector3.Dot(boidForward, neighborLocation);
            float angle = 0.0f;
            if (dot >= -1.0f && dot <= 1.0f) {
                angle = (float)Math.Acos(dot);
                if (angle <= deg135)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Get the turn angle between a boid and a position
        /// and divide by the distance between them to get 
        /// a reasonable angle with respect to distance
        /// </summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        private double GetTurnAngle(Object3D boid, Vector3 v) {
            // put them in the same plane
            Vector3 vInXZ = new Vector3(v.X, 0, v.Z);
            Vector3 boidLocationInXZ = new Vector3(boid.Translation.X, 0, boid.Translation.Z);
            Vector3 target = vInXZ - boidLocationInXZ;
            Vector3 boidForward = boid.Forward;

            // normalize the forward vector of boid
            boidForward.Normalize();
            target.Normalize();

            double dot = Vector3.Dot(target, boidForward);
            if (dot < -1)
                dot = -1;

            if (dot > 1)
                dot = 1;

            return Math.Acos(dot) * Math.Sign(Vector3.Cross(target, boidForward).Y);
        }

        private double ToDegree(float radian) {
            return radian;
        }

        /// <summary>
        /// Original algorithm from AGXNASK
        /// </summary>
        private void MoveRandomly() {
            float angle = 0.3f;
            foreach (Object3D obj in instance) {
                obj.Yaw = 0.0f;
                // change direction 4 time a second  0.07 = 4/60
                if (random.NextDouble() < 0.07) {
                    if (random.NextDouble() < 0.5)
                        obj.Yaw -= angle; // turn left
                    else
                        obj.Yaw += angle; // turn right
                }

                obj.UpdateMovableObject();
                stage.SetSurfaceHeight(obj);
            }
        }

        private void MoveByFlocking() {
            for (int i = 0; i < instance.Count; ++i) {
                if (Vector3.Distance(instance[i].Translation, leader.Translation) >= OUTER_RADIUS) {
                    instance[i].TurnToFace(leader.Translation);
                }
                else {
                    Cohere(i);
                    Align(i);
                    Separate(i);
                }

                instance[i].UpdateMovableObject();
                stage.SetSurfaceHeight(instance[i]);
            }
        }

        private void Cohere(int i) {
            Vector3 averagePosition = new Vector3(0.0f, 0.0f, 0.0f);
            int n = 0;
            for (int j = 0; j < instance.Count; ++j) {
                if (i != j) {
                    double d = Vector3.Distance(instance[i].Translation, instance[j].Translation);
                    if (d > 2000 && d < NEIGHBOR_RADIUS) {
                        averagePosition += instance[j].Translation;
                        n++;
                    }
                }
            }

            if (n > 0) {
                averagePosition += leader.Translation;
                averagePosition /= (n + 1);
                instance[i].TurnToFace(averagePosition);
            }
        }

        private void Align(int i) {
            Vector3 averageForward = new Vector3(0.0f, 0.0f, 0.0f);
            int n = 0;
            for (int j = 0; j < instance.Count; ++j) {
                if (i != j) {
                    double d = Vector3.Distance(instance[i].Translation, instance[j].Translation);
                    if (d > 2000 && d < NEIGHBOR_RADIUS) {
                        averageForward += instance[j].Forward;
                        n++;
                    }
                }
            }

            if (n > 0) {
                averageForward += leader.Forward;
                averageForward /= (n + 1);
                instance[i].TurnToFace(averageForward);
            }
        }

        private void Separate(int i) {
            Vector3 averageForward = new Vector3(0.0f, 0.0f, 0.0f);
            for (int j = 0; j < instance.Count; ++j) {
                double d = Vector3.Distance(instance[i].Translation, instance[j].Translation);
                if (d < SEPARATION_DISTANCE) {
                    instance[j].TurnAway(instance[i].Translation);
                }
            }
        }

        public double ToDegree(double radian) {
            return (radian * 180 / Math.PI);
        }

        /// <summary>
        /// Each pack member's orientation matrix will be updated.
        /// Distribution has pack of dogs moving randomly.  
        /// Supports leaderless and leader based "flocking" 
        /// </summary>      
        public override void Update(GameTime gameTime) {
            counter++;
            double randValue = random.NextDouble();
            if (flockMode == FlockEnum.FLOCK_0_PERCENT) {
                MoveRandomly();
            }
            else if (flockMode == FlockEnum.FLOCK_33_PERCENT) {
                Debug.WriteLine("counter = " + counter);
                if (counter % 7 == 0) {
                    MoveByFlocking();
                }
            }
            else { // flockMode == FlockEnum.FLOCK_67_PERCENT
                if (counter % 3 == 0) {
                    MoveByFlocking();
                }
            }
              
            base.Update(gameTime);
        }
    }

}
