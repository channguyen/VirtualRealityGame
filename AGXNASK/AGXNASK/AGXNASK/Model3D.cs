using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AGXNASK {

    /// <summary>
    /// Stationary modeled object in the stage.  The SK565 Stage class creates several examples:
    /// the temple and the four dogs.
    /// </summary>
    public class Model3D : DrawableGameComponent {

        /**
         * General information
         * *******************
         */

        /*
         * Name
         */
        protected string name;

        /*
         * The stage it's in
         */
        protected Stage stage;

        /*
         * Its model 
         */
        protected Model model = null;

        /*
         * Is it collidable?
         */
        protected bool isCollidable = false;

        /**
         * Model3D's mesh BoundingSphere values
         * ************************************
         */

        /*
         * The vector represent bounding sphere center
         */
        protected Vector3 boundingSphereCenter;

        /*
         * Its radius
         */
        protected float boundingSphereRadius = 0.0f;

        /*
         * The matrix represents this bounding sphere
         */
        protected Matrix boundingSphereWorld;

        /**
         * Model3D's object instance collection 
         * ************************************
         */
        protected List<Object3D> instance;

        public Model3D(Stage theStage, string label, string fileOfModel)
            : base(theStage) {
            
            name = label;
            stage = theStage;
            instance = new List<Object3D>();
            model = stage.Content.Load<Model>(fileOfModel);

            // compute the translation to the model's bounding sphere
            // center and radius;
            float minX, minY, minZ, maxX, maxY, maxZ;
            minX = minY = minZ = Int32.MaxValue;
            maxX = maxY = maxZ = Int32.MinValue;

            for (int i = 0; i < model.Meshes.Count; i++) {
                // See if this mesh extends the bounding sphere.
                BoundingSphere aBoundingSphere = model.Meshes[i].BoundingSphere;
                if ((aBoundingSphere.Center.X - aBoundingSphere.Radius) < minX)
                    minX = aBoundingSphere.Center.X - aBoundingSphere.Radius;
                if ((aBoundingSphere.Center.Y - aBoundingSphere.Radius) < minY)
                    minY = aBoundingSphere.Center.Y - aBoundingSphere.Radius;
                if ((aBoundingSphere.Center.Z - aBoundingSphere.Radius) < minZ)
                    minZ = aBoundingSphere.Center.Z - aBoundingSphere.Radius;
                if ((aBoundingSphere.Center.X + aBoundingSphere.Radius) > maxX)
                    maxX = aBoundingSphere.Center.X + aBoundingSphere.Radius;
                if ((aBoundingSphere.Center.Y + aBoundingSphere.Radius) > maxY)
                    maxY = aBoundingSphere.Center.Y + aBoundingSphere.Radius;
                if ((aBoundingSphere.Center.Z + aBoundingSphere.Radius) > maxZ)
                    maxZ = aBoundingSphere.Center.Z + aBoundingSphere.Radius;
            }

            // get the diameter of model's bounding sphere
            // radius temporarily holds the largest diameter
            if ((maxX - minX) > boundingSphereRadius) 
                boundingSphereRadius = maxX - minX;
            if ((maxY - minY) > boundingSphereRadius) 
                boundingSphereRadius = maxY - minY;
            if ((maxZ - minZ) > boundingSphereRadius) 
                boundingSphereRadius = maxZ - minZ;

            // set boundingSphereRadius
            boundingSphereRadius = boundingSphereRadius * 1.1f / 2.0f;  // set the radius from largest diameter  

            // set the center of model's bounding sphere
            boundingSphereCenter =
              new Vector3(minX + boundingSphereRadius, minY + boundingSphereRadius, minZ + boundingSphereRadius);
            // need to scale boundingSphereRadius for each object instances in Object3D
        }


        /// <summary>
        /// Return the center of the model's bounding sphere
        /// </summary>
        public Vector3 BoundingSphereCenter {
            get {
                return boundingSphereCenter;
            }
        }

        /// <summary>
        /// Return the radius of the model's bounding sphere
        /// </summary>      
        public float BoundingSphereRadius {
            get {
                return boundingSphereRadius;
            }
        }

        public List<Object3D> Instance {
            get {
                return instance;
            }
        }

        public bool IsCollidable {
            get {
                return isCollidable;
            }
            set {
                isCollidable = value;
            }
        }


        public void AddObject(Vector3 position, Vector3 orientAxis, float radians, Vector3 scales) {
            Object3D obj3d = new Object3D(stage, this, String.Format("{0}.{1}", name, instance.Count), position, orientAxis, radians, scales);

            // need to do only once for Model3D
            obj3d.UpdateBoundingSphere();  
            instance.Add(obj3d);

            if (IsCollidable) 
                stage.Collidable.Add(obj3d);
        }

        public void AddObject(Vector3 position, Vector3 orientAxis, float radians) {
            Object3D obj3d = new Object3D(stage, this, String.Format("{0}.{1}", name, instance.Count), position, orientAxis, radians, Vector3.One);

            // need to do only once for Model3D
            obj3d.UpdateBoundingSphere();  
            instance.Add(obj3d);

            if (IsCollidable)  
                stage.Collidable.Add(obj3d);
        }

        public override void Draw(GameTime gameTime) {
            Matrix[] modelTransforms = new Matrix[model.Bones.Count];
            foreach (Object3D obj3d in instance) {
                foreach (ModelMesh mesh in model.Meshes) {
                    model.CopyAbsoluteBoneTransformsTo(modelTransforms);
                    foreach (BasicEffect effect in mesh.Effects) {
                        effect.EnableDefaultLighting();
                        if (stage.Fog) {
                            effect.FogColor = Color.CornflowerBlue.ToVector3();
                            effect.FogStart = stage.FogStart;
                            effect.FogEnd = stage.FogEnd;
                            effect.FogEnabled = true;
                        }
                        else {
                            effect.FogEnabled = false;
                        }

                        effect.DirectionalLight0.DiffuseColor = stage.DiffuseLight;
                        effect.AmbientLightColor = stage.AmbientLight;
                        effect.DirectionalLight0.Direction = stage.LightDirection;
                        effect.DirectionalLight0.Enabled = true;
                        effect.View = stage.View;
                        effect.Projection = stage.Projection;
                        effect.World = modelTransforms[mesh.ParentBone.Index] * obj3d.Orientation;
                    }
                    mesh.Draw();
                }

                // draw the bounding sphere with blending ?
                if (stage.DrawBoundingSpheres && IsCollidable) {
                    foreach (ModelMesh mesh in stage.BoundingSphere3D.Meshes) {
                        model.CopyAbsoluteBoneTransformsTo(modelTransforms);
                        foreach (BasicEffect effect in mesh.Effects) {
                            effect.EnableDefaultLighting();
                            if (stage.Fog) {
                                effect.FogColor = Color.CornflowerBlue.ToVector3();
                                effect.FogStart = 50;
                                effect.FogEnd = 500;
                                effect.FogEnabled = true;
                            }
                            else {
                                effect.FogEnabled = false;
                            }

                            effect.DirectionalLight0.DiffuseColor = stage.DiffuseLight;
                            effect.AmbientLightColor = stage.AmbientLight;
                            effect.DirectionalLight0.Direction = stage.LightDirection;
                            effect.DirectionalLight0.Enabled = true;
                            effect.View = stage.View;
                            effect.Projection = stage.Projection;
                            effect.World = obj3d.ObjectBoundingSphereWorld * modelTransforms[mesh.ParentBone.Index];
                        }
                        stage.SetBlendingState(true);
                        mesh.Draw();
                        stage.SetBlendingState(false);
                    }
                }
            }
        }
    }

}