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


namespace AGXNASK {

    /// <summary>
    /// IndexVertexBuffers defines variables and properties shared
    /// by all indexed-vertex meshes.
    /// Since the vertex type can change, vertices should be defined
    /// in subclasses.
    /// </summary>
    public abstract class IndexVertexBuffers : DrawableGameComponent {
        protected Stage stage;
        protected string name;
        protected int range, nVertices, nIndices;
        protected VertexBuffer vb = null;
        protected IndexBuffer ib = null;
        protected int[] indices;  // indexes for IndexBuffer -- define face vertice indexes clockwise 

        public IndexVertexBuffers(Stage theStage, string label)
            : base(theStage) {

            stage = theStage;
            name = label;
        }

        // Properties

        public VertexBuffer VB {
            get { 
                return vb; 
            }
            set { 
                vb = value; 
            }
        }

        public IndexBuffer IB {
            get { 
                return ib; 
            }
            set { 
                ib = value; 
            }
        }
    }
}
