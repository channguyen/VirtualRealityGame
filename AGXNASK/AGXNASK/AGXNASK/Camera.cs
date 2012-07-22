using Microsoft.Xna.Framework;

namespace AGXNASK {

    /// <summary>
    /// A viewpoint in the stage.  Cameras have a viewMatrix that is its position and orientation 
    /// in the stage.  There are four cameras:  TopDownCamera, FirstCamera, FollowCamera, AboveCamera.
    /// The stage has one TopDownCamera named "Whole stage camera".  This is the starting camera
    /// for SK565.  The TopDownCamera is stationary.  
    /// The other three cameras are associated with an agent.  They are the agent's
    /// first (person view), follow (person view), and above (look down on agent view).  
    /// Player handles user input to call stage.nextCamera() to selects the currentCamera, 
    /// update the agent's current avatarCamera, update currentCamera's viewMatrix 
    /// with updateViewMatrix().
    /// </summary>

    public class Camera {
        public enum CameraEnum {
            TopDownCamera,
            FirstCamera,
            FollowCamera,
            AboveCamera
        }

        private Object3D agent;
        private int terrainCenter;
        private int offset = 300;
        private Matrix viewMatrix;
        private Stage scene;
        CameraEnum whichCamera;

        public Camera(Stage aScene, CameraEnum whichCameraType) {
            Name = "Whole stage";
            scene = aScene;
            whichCamera = whichCameraType;
            terrainCenter = scene.TerrainSize / 2;
            UpdateViewMatrix();
        }

        public Camera(Stage aScene, Object3D anAgentObject, CameraEnum whichCameraType) {
            scene = aScene;
            agent = anAgentObject;
            whichCamera = whichCameraType;
        }

        // Properties

        public string Name {
            get; set;
        }

        public Matrix ViewMatrix {
            get {
                return viewMatrix;
            }
        }

        /// <summary>
        /// When an agent updates its place in the stage it calls its currentCamera's
        /// updateViewMatrix() to place the camera's viewMatrix.
        /// </summary>
        public void UpdateViewMatrix() {
            switch (whichCamera) {
                case CameraEnum.TopDownCamera:
                    viewMatrix = Matrix.CreateLookAt(
                       new Vector3(terrainCenter, scene.FarYon - 50, terrainCenter),
                       new Vector3(terrainCenter, 0, terrainCenter),
                       new Vector3(0, 0, -1));
                    break;

                case CameraEnum.FirstCamera:
                    viewMatrix = Matrix.CreateLookAt(agent.Translation, agent.Translation + agent.Forward, agent.Orientation.Up);
                    viewMatrix *= Matrix.CreateTranslation(0, -offset, 0);
                    break;

                case CameraEnum.FollowCamera:
                    viewMatrix = Matrix.CreateLookAt(agent.Translation, agent.Translation + agent.Forward, agent.Orientation.Up);
                    viewMatrix *= Matrix.CreateTranslation(0, -2 * offset, -8 * offset);
                    break;

                case CameraEnum.AboveCamera:
                    viewMatrix = Matrix.CreateLookAt(
                       new Vector3(agent.Translation.X, agent.Translation.Y + 3 * offset, agent.Translation.Z),
                       agent.Translation,  
                       new Vector3(0, 0, -1));
                    break;
            }
        }
    }
}
