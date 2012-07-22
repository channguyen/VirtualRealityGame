using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AGXNASK {

    /// <summary>
    /// Path represents a collection of NavNodes that a movable Object3D can traverse.
    /// Paths have a PathType of
    /// <list type="number"> SINGLE, traverse list once, set done to true </list>
    /// <list type="number"> REVERSE, loop by reversing path after each traversal </list>
    /// <list type=="number"> LOOP, loop by starting at paths first node again </list>
    /// 
    /// 2/14/2012 last changed
    /// </summary>
    public class Path : DrawableGameComponent {
        public enum PathType { SINGLE, REVERSE, LOOP };
        private List<NavNode> node;
        private int nextNode;
        private PathType pathType;
        private bool done;
        private Stage stage;
        private bool drawFlag;

        /// <summary>
        /// Create a path
        /// </summary>
        /// <param name="theStage"> "world's stage" </param>
        /// <param name="apath"> collection of nodes in path</param>
        /// <param name="aPathType"> SINGLE, REVERSE, or LOOP path traversal</param>
        public Path(Stage theStage, List<NavNode> aPath, PathType aPathType, bool flag)
            : base(theStage) {
            
            node = aPath;
            nextNode = 0;
            pathType = aPathType;
            stage = theStage;
            done = false;
            drawFlag = flag;
        }

        /// <summary>
        /// Create a path from XZ nodes defined in a pathFile.
        /// The file must be accessible from the executable environment.
        /// </summary>
        /// <param name="theStage"> "world's stage" </param>
        /// <param name="aPathType"> SINGLE, REVERSE, or LOOP path traversal</param>
        /// <param name="pathFile"> text file, each line a node of X Z values, separated by a single space </x></param>
        public Path(Stage theStage, PathType aPathType, string pathFile)
            : base(theStage) {

            node = new List<NavNode>();
            stage = theStage;
            nextNode = 0;
            pathType = aPathType;
            done = false;
           
            // read file
            using (StreamReader fileIn = File.OpenText(pathFile)) {
                int x, z;
                string line;
                string[] tokens;
                line = fileIn.ReadLine();

                do {
                    // use default separators
                    tokens = line.Split(new char[] { });  
                    x = Int32.Parse(tokens[0]);
                    z = Int32.Parse(tokens[1]);
                    node.Add(new NavNode(new Vector3(x, 0, z), NavNode.NavNodeEnum.WAYPOINT));
                    line = fileIn.ReadLine();
                } 
                while (line != null);
            }
        }

        // Properties

        public int Count { 
            get { 
                return node.Count; 
            } 
        }

        public bool Done { 
            get { 
                return done; 
            } 
        }

        public List<NavNode> Nodes {
            get {
                return node;
            }
        }

        /// <summary>
        /// Gets the next node in the path using path's PathType
        /// </summary>
        public NavNode NextNode {
            get {
                NavNode n = null;
                if (node.Count > 0 && nextNode < node.Count - 1) { 
                    n = node[nextNode];
                    nextNode++;
                }
                else if (node.Count - 1 == nextNode && pathType == PathType.SINGLE) {
                    n = node[nextNode];
                    done = true;
                }
                else if (node.Count - 1 == nextNode && pathType == PathType.REVERSE) {
                    node.Reverse();
                    nextNode = 0;  // set to next node
                    n = node[nextNode];
                    nextNode++;
                }
                else if (node.Count - 1 == nextNode && pathType == PathType.LOOP) {
                    n = node[nextNode];
                    nextNode = 0;
                }
                return n;
            }
        }

        public NavNode CurrentNode {
            get {
                NavNode n = null;
                if (node.Count > 0 && nextNode < node.Count - 1) { // take next step on path
                    n = node[nextNode];
                }
                // at end of current path, decide what to do:  stop, reverse path, loop?
                else if (node.Count - 1 == nextNode && pathType == PathType.SINGLE) {
                    n = node[nextNode];
                    done = true;
                }
                else if (node.Count - 1 == nextNode && pathType == PathType.REVERSE) {
                    node.Reverse();
                    n = node[nextNode];
                }
                else if (node.Count - 1 == nextNode && pathType == PathType.LOOP) {
                    n = node[nextNode];
                }
                return n;
            }
        }

        /// <summary>
        /// Draw method required for DrawableGameComponent object
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime) {
            if (drawFlag) {
                Matrix[] modelTransforms = new Matrix[stage.WayPoint3D.Bones.Count];
                foreach (NavNode navNode in node) {
                    // draw the Path markers
                    foreach (ModelMesh mesh in stage.WayPoint3D.Meshes) {
                        stage.WayPoint3D.CopyAbsoluteBoneTransformsTo(modelTransforms);
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

                            effect.DirectionalLight0.DiffuseColor = navNode.NodeColor;
                            effect.AmbientLightColor = navNode.NodeColor;
                            effect.DirectionalLight0.Direction = stage.LightDirection;
                            effect.DirectionalLight0.Enabled = true;
                            effect.View = stage.View;
                            effect.Projection = stage.Projection;
                            effect.World = Matrix.CreateTranslation(navNode.Translation) * modelTransforms[mesh.ParentBone.Index];
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