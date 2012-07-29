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

        private Vector3 leaderLastPosition;
        private Vector3 alphaLastPosition;


        // flocking rules
        private float visibility;
        private float bindingDistance;
        private float separationDistance;
        private float maxTurnAngle;
        private int flockSize;
        private bool[,] neighbors;
        private bool[] onReturn;

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

        public float Visibility {
            get {
                return visibility;
            }
            set {
                visibility = value;
            }
        }

        public float BindingDistance {
            get {
                return bindingDistance;
            }
            set {
                bindingDistance = value;
            }
        }

        public float SeparationDistance {
            get {
                return separationDistance;
            }
            set {
                separationDistance = value;
            }
        }

        public float MaxTurnAngle {
            get {
                return maxTurnAngle;
            }
            set {
                if (Math.Abs(value) <= Math.PI * 2) {
                    maxTurnAngle = value;
                }
            }
        }

        public int FlockSize {
            get {
                return flockSize;
            }
            set {
                flockSize = value;
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

            InitializeFlocing();
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

            InitializeFlocing();
        }

        /// <summary>
        /// Initialize all flocking variables
        /// </summary>
        public void InitializeFlocing() {
            visibility = (float)(2 * Math.PI / 3);
            bindingDistance = 45000;
            separationDistance = 1200;
            maxTurnAngle = (float)Math.PI / 4;
            onReturn = new bool[flockSize];
            for (int i = 0; i < flockSize; ++i) {
                onReturn[i] = false;
            }
        }

        /// <summary>
        /// Implemented from ideas and methods as provided
        /// O"Reilly Book: AI for Game Developers
        /// </summary>
        /// <param name="index"></param>
        private void InterceptLeader(int index) {
            Vector3 evaderPosition = new Vector3(leader.Translation.X, 0, leader.Translation.Z);
            Vector3 chaserPosition = new Vector3(instance[index].Translation.X, 0, instance[index].Translation.Z);

            if (leaderLastPosition != null && leaderLastPosition != evaderPosition && alphaLastPosition != null) {
                Vector3 evaderVelocity = evaderPosition - leaderLastPosition;
                Vector3 chaserVelocity = chaserPosition - alphaLastPosition;
                Vector3 relativeVelocity = evaderVelocity - chaserVelocity;
                Vector3 distance = evaderPosition - chaserPosition;
                float timeToClose = distance.Length() / relativeVelocity.Length();
                Vector3 projectedPosition = evaderPosition + evaderVelocity * timeToClose;
                instance[index].TurnToFace(projectedPosition);
            }
            else {
                instance[index].TurnToFace(leader.Translation);
            }

            leaderLastPosition = evaderPosition;
            alphaLastPosition = chaserPosition;
        }

        public void UpdateFlockingMode() {
            switch (flockMode) {
                case FlockEnum.FLOCK_0_PERCENT:
                    break;

                case FlockEnum.FLOCK_33_PERCENT:
                    bindingDistance = 10000;
                    separationDistance = 2000;
                    break;

                case FlockEnum.FLOCK_67_PERCENT:
                    bindingDistance = 5000;
                    separationDistance = 1000;
                    break;
            }
        }

        private void ApplyFlockingRules(int index) {
            int count;
            float distance;
            float angle;

            bool isNeighbor;
            bool inRange;
            bool inSight;

            Vector3 toTarget;
            Vector3 averagePosition;
            Vector3 averageBearing;

            MapNeighbors();
            isNeighbor = inRange = inSight = false;
            toTarget = leader.Translation - instance[index].Translation;
            distance = toTarget.Length();

            if (distance > bindingDistance) {
                onReturn[index] = true;
                if (distance > 1.5 * bindingDistance && index == 0) {
                    InterceptLeader(index);
                }
                else {
                    toTarget = leader.Translation - instance[index].Translation;
                    angle = GetTurnAngle(instance[index].Forward, toTarget);
                    instance[index].Yaw = angle;
                }
            }
            else {
                float cohesionAngle = 0.0f;
                float bearingAngle = 0.0f;
                float separationAngle = 0.0f;
                float separationWeight = 0.0f;
                float separationBearing = 0.0f;
                float minSeparation = 0.0f;
                float maxSeparation = 0.0f;

                int leaderWeight = 2;
                averagePosition = new Vector3(leader.Translation.X, 0, leader.Translation.Z);
                averageBearing = new Vector3(leader.Forward.X, 0, leader.Forward.Z);
                averagePosition *= leaderWeight;
                averageBearing *= leaderWeight;

                count = leaderWeight;

                for (int i = 0; i < flockSize; ++i) {
                    if (i == index) {
                        continue;
                    }

                    toTarget = instance[i].Translation - instance[index].Translation;
                    isNeighbor = neighbors[index, i];
                    inSight = Math.Abs(GetTurnAngle(instance[index].Forward, toTarget)) <= visibility;

                    if (isNeighbor && inSight) {
                        averagePosition += new Vector3(instance[i].Translation.X, 0, instance[i].Translation.Z);
                        averageBearing += new Vector3(instance[i].Forward.X, 0, instance[i].Forward.Z);
                        if (toTarget.Length() < separationDistance) {
                            separationWeight = (separationDistance - toTarget.Length()) / SeparationDistance;
                            separationBearing = GetTurnAngle(instance[index].Forward, toTarget);
                            angle = separationBearing * separationWeight * -1.0f;
                            if (angle > 0 && angle > maxSeparation) {
                                maxSeparation = angle;
                            }
                            else if (angle < 0 && angle < minSeparation) {
                                minSeparation = angle;
                            }

                            separationAngle += angle;
                        }

                        count++;
                    }

                    if (separationAngle > 0 && separationAngle > maxSeparation) {
                        separationAngle = maxSeparation;
                    }
                    else if (separationAngle < 0 && separationAngle < minSeparation) {
                        separationAngle = minSeparation;
                    }
                }

                if (count > 0) {
                    distance = 0.0f;
                    float angleChange = 0.0f;
                    float steering = 1.0f;
                    float deltaWeight = 0.0f;

                    averagePosition = averagePosition / count;
                    toTarget = averagePosition - instance[index].Translation;
                    toTarget.Y = 0;
                    angleChange = GetTurnAngle(instance[index].Forward, toTarget);

                    distance = toTarget.Length();
                    cohesionAngle = angleChange;
                    if (distance > bindingDistance) {
                        cohesionAngle = angleChange;
                        steering = 0.0f;
                    }
                    else {
                        cohesionAngle = angleChange * (distance / bindingDistance);
                        steering = steering - (distance / bindingDistance);
                    }

                    if (steering > 0) {
                        bearingAngle = 0.0f;
                        averageBearing = averageBearing / count;
                        angleChange = GetTurnAngle(instance[index].Forward, averageBearing);
                        if (Math.Abs(Vector3.Dot(Vector3.Normalize(instance[index].Forward), Vector3.Normalize(averageBearing))) < 1) {
                            deltaWeight = (float) Math.Acos(Vector3.Dot(Vector3.Normalize(instance[index].Forward), Vector3.Normalize(averageBearing)));
                            deltaWeight = deltaWeight / (float) Math.PI;
                            bearingAngle = angleChange * deltaWeight;
                            bearingAngle *= steering;
                            steering -= deltaWeight;
                        }
                    }

                    if (steering > 0) {
                        if (float.IsNaN(separationAngle)) {
                            separationAngle = 0.0f;
                        }

                        if (float.IsNaN(instance[index].Yaw)) {
                            instance[index].Yaw = 0.0f;
                        }
                    }

                    float finalAngle = cohesionAngle + bearingAngle + separationAngle;
                    if (Math.Abs(finalAngle) > maxTurnAngle) {
                        if (finalAngle < 0) {
                            instance[index].Yaw = maxTurnAngle - 1;
                        }
                        else {
                            instance[index].Yaw = maxTurnAngle;
                        }
                    }
                    else {
                        instance[index].Yaw = finalAngle;
                    }
                }
            }
        }

        private void MapNeighbors() {
            bool isNeighbor = false;
            neighbors = new bool[flockSize, flockSize];
            for (int i = 0; i < flockSize - 1; ++i) {
                for (int j = i + 1; j < flockSize; ++j) {
                    // avoid checking itself
                    if (i == j) {
                        continue;
                    }

                    isNeighbor = false;
                    if (Vector3.Distance(instance[i].Translation, instance[j].Translation) <= bindingDistance) {
                        isNeighbor = true;
                    }

                    neighbors[i, j] = neighbors[j, i] = isNeighbor;
                }
            }
        }

        public float GetTurnAngle(Vector3 vectorA, Vector3 vectorB) {
            if (vectorB.Length() == 0)
                return 0;

            Vector3 cross;
            vectorA.Normalize();
            vectorB.Normalize();

            float dot = 0;
            float acos = 0;
            float angle = 0;

            dot = Vector3.Dot(vectorA, vectorB);
            if (dot > 1.0) {
                dot = 1.0f;
            }
            else if (dot < -1.0) {
                dot = -1.0f;
            }
            acos = (float)Math.Acos(dot);
            cross = Vector3.Cross(vectorA, vectorB);

            angle = acos * Math.Sign(cross.Y);

            return angle;
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
                ApplyFlockingRules(i);
                instance[i].UpdateMovableObject();
                stage.SetSurfaceHeight(instance[i]);
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
            UpdateFlockingMode();

            if (flockMode == FlockEnum.FLOCK_0_PERCENT) {
                MoveRandomly();
            }
            else {
                MoveByFlocking();
            }

            base.Update(gameTime);
        }
    }

}
