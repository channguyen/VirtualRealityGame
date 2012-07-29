using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;


namespace AGXNASK {
    /// <summary>
    /// Scene.cs  the "main class" for AGXNASK
    /// Scene declares and initializes program specific entities and user interaction.
    /// See AGXNASKv4-doc.pdf file for class diagram and usage information. 
    /// </summary>
    public class Scene : Stage {
        public Scene() {
            // default
        }

        /// <summary>
        /// Set GraphicDevice display and rendering BasicEffect effect.  
        /// Create SpriteBatch, font, and font positions.
        /// Creates the traceViewport to display information and the sceneViewport
        /// to render the environment.
        /// Create and add all DrawableGameComponents and Cameras.
        /// </summary>
        protected override void LoadContent() {
            base.LoadContent();  // create the Scene entities -- Inspector.
            CreateClouds();
            CreateWalls();
            CreateAdditionalModels();
            CreatePackOfSponge();

            // get the the next camera
            NextCamera();
        }

        /// <summary>
        /// Models from users
        /// </summary>
        private void CreateAdditionalModels() {
            // create a temple
            Model3D m3d = new Model3D(this, "temple", "TempleV3");
            m3d.IsCollidable = true;  // must be set before addObject(...) and Model3D doesn't set it
            m3d.AddObject(new Vector3(340 * SPACING, terrain.SurfaceHeight(340, 340), 340 * SPACING), new Vector3(0, 1, 0), 0.79f);
            Components.Add(m3d);

            Model3D tajMahal = new Model3D(this, "Taj Mahal", "TajMahal");
            tajMahal.IsCollidable = true;
            tajMahal.AddObject(
                new Vector3(LocationConstant.TAJ_MAHAL_X * SPACING, terrain.SurfaceHeight(LocationConstant.TAJ_MAHAL_X, LocationConstant.TAJ_MAHAL_Z) + 2500, LocationConstant.TAJ_MAHAL_Z * SPACING), new Vector3(0, 1, 0), 0.0f);
            Components.Add(tajMahal);

            Model3D rockBuilding = new Model3D(this, "Rock Building", "RockBuilding");
            rockBuilding.IsCollidable = true;
            rockBuilding.AddObject(
                new Vector3(LocationConstant.ROCK_BUILDING_X * SPACING, terrain.SurfaceHeight(LocationConstant.ROCK_BUILDING_X, LocationConstant.ROCK_BUILDING_Z), LocationConstant.ROCK_BUILDING_Z * SPACING), new Vector3(0, 1, 0), 0.0f);
            Components.Add(rockBuilding);

            Model3D pineappleBuilding = new Model3D(this, "Pineapple Building", "PineappleBuilding");
            pineappleBuilding.IsCollidable = true;
            pineappleBuilding.AddObject(
                new Vector3(LocationConstant.PINEAAPLE_X * SPACING, terrain.SurfaceHeight(LocationConstant.PINEAAPLE_X, LocationConstant.PINEAAPLE_Z), LocationConstant.PINEAAPLE_Z * SPACING), new Vector3(0, 1, 0), 0.0f);
            Components.Add(pineappleBuilding);
        }

        /// <summary>
        /// Add a pack of spongbot 
        /// </summary>
        private void CreatePackOfSponge() {
            Random random = new Random();
            Pack pack = new Pack(this, "Sponge Bot", "SpongeBot", player.AgentObject);
            Components.Add(pack);
            for (int x = -9; x < 10; x += 6) {
                for (int z = -3; z < 4; z += 6) {
                    float scale = (float)(0.5 + random.NextDouble());
                    float xPos = (284 + x) * SPACING;
                    float zPos = (284 + z) * SPACING;
                    pack.AddObject(
                        new Vector3(xPos, terrain.SurfaceHeight((int)xPos / SPACING, (int)zPos / SPACING), zPos),
                        new Vector3(0, 1, 0), 0.0f,
                        new Vector3(scale, scale, scale));
                }
            }
        }

        /// <summary>
        /// Add some clouds
        /// </summary>
        private void CreateClouds() {
            Random random = new Random();
            Cloud cloud = new Cloud(this, "cloud", "cloudV3");
            // add 9 cloud instances
            for (int x = RANGE / 4; x < RANGE; x += (RANGE / 4)) {
                for (int z = RANGE / 4; z < RANGE; z += (RANGE / 4)) {
                    cloud.AddObject(
                        new Vector3(x * SPACING, terrain.SurfaceHeight(x, z) + 2000, z * SPACING),
                        new Vector3(0, 1, 0), 0.0f,
                        new Vector3(random.Next(3) + 1, random.Next(3) + 1, random.Next(3) + 1));
                }
            }
            Components.Add(cloud);
        }

        /// <summary>
        /// Add walls for path finding
        /// </summary>
        private void CreateWalls() {
            Wall wall = new Wall(this, "wall", "100x100x100Brick");
            Components.Add(wall);
        }

        protected override void Update(GameTime gameTime) {
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime) {
            base.Draw(gameTime);
        }

        static void Main(string[] args) {
            using (Scene stage = new Scene()) {
                stage.Run();
            }
        }
    }
}