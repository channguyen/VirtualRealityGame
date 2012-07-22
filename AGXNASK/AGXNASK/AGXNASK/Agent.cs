using System.Linq;
using Microsoft.Xna.Framework;

namespace AGXNASK {

    /// <summary>
    /// 
    /// A model that moves.  
    /// Has three Cameras:  first, follow, above.
    /// Camera agentCamera references the currently used camera {first, follow, above}
    /// Follow camera shows the MovableMesh from behind and up.
    /// Above camera looks down on avatar.
    /// The agentCamera (active camera) is updated by the avatar's Update().
    /// 
    /// </summary>
    public abstract class Agent : MovableModel3D {

        private const int SNAP_DISTANCE = 1000;
        protected Object3D agentObject = null;
        protected Camera agentCamera;
        protected Camera first;
        protected Camera follow;
        protected Camera above;
        protected int taggedTreasures;

        public enum CameraCase {
            FirstCamera,
            FollowCamera,
            AboveCamera
        }

        /// <summary>
        /// 
        /// Create an Agent.
        /// All Agents are collidable and have a single instance Object3D named agentObject.
        /// Set StepSize, create first, follow and above cameras.
        /// Set first as agentCamera
        /// 
        /// </summary>
        public Agent(Stage stage, string label, Vector3 position, Vector3 orientAxis, float radians, string meshFile)
            : base(stage, label, meshFile) {

            AddObject(position, orientAxis, radians);
            agentObject = instance.First<Object3D>();

            // create 3 cameras
            first = new Camera(stage, agentObject, Camera.CameraEnum.FirstCamera);
            follow = new Camera(stage, agentObject, Camera.CameraEnum.FollowCamera);
            above = new Camera(stage, agentObject, Camera.CameraEnum.AboveCamera);

            // add 3 cameras to Stage
            stage.AddCamera(first);
            stage.AddCamera(follow);
            stage.AddCamera(above);

            // initialize the first camera
            agentCamera = first;
            taggedTreasures = 0;
        }

        // Properties  
        protected int TaggedTreasures {
            get {
                return taggedTreasures;
            }
        }

        public Object3D AgentObject {
            get {
                return agentObject;
            }
        }

        public Camera AvatarCamera {
            get {
                return agentCamera;
            }
            set {
                agentCamera = value;
            }
        }

        public Camera First {
            get {
                return first;
            }
        }

        public Camera Follow {
            get {
                return follow;
            }
        }

        public Camera Above {
            get {
                return above;
            }
        }

        protected void AddTaggedTreasure() {
            taggedTreasures++;
        }

        public override string ToString() {
            return agentObject.Name;
        }

        public void UpdateCamera() {
            agentCamera.UpdateViewMatrix();
        }

        public override void Update(GameTime gameTime) {
            AgentObject.UpdateMovableObject();
            base.Update(gameTime);

            // Agent is in correct (X,Z) position on the terrain 
            // set height to be on terrain -- this is a crude "first approximation" solution.
            stage.SetSurfaceHeight(agentObject);
        }

        public void SetSpeed(int stepSize) {
            AgentObject.StepSize = stepSize;
        }
    }
}