using System;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace AGXNASK {

    /// <summary>
    /// A WayPoint or Marker to be used in path following or path finding.
    /// Four types of WAYPOINT:
    /// 1   VERTEX, a terrain vertex
    /// 2   WAYPOINT, a node to follow in a path
    /// 3   A_STAR, a node in an A* open or closed set
    /// 4   PATH, a node in a found path (result of A*)
    /// </summary>
    public class NavNode : IComparable<NavNode> {

        public enum NavNodeEnum {
            VERTEX, 
            WAYPOINT, 
            A_STAR, 
            PATH,
            WALL
        };

        /**
         * total distance 
         *      distance = distanceFromSource + distanceToGoal
         */
        private double distance;

        /**
         * distance from a source point 
         *      the movement cost from the starting point
         *      to a given node on map, following the path 
         *      generated to get there.
         */
        private double distanceFromSource;

        /**
         * distance from goal
         *      the estimated movement cost to move
         *      from that given square on map to the final
         *      destination. This is often referred as
         *      the heuristic cost.
         */
        private double distanceToGoal;

        /**
         * the parent of the current node
         *      used to get trace A* path      
         */
        private NavNode parent;

        /**
         * Node location
         */
        private Vector3 translation;

        /**
         * Node type
         */
        private NavNodeEnum navigatable;

        /**
         * Node color
         */
        private Vector3 nodeColor;
        private bool isActive;


        /// <summary>
        /// Make a VERTEX NavNode
        /// </summary>
        /// <param name="pos"> location of WAYPOINT</param>
        public NavNode(Vector3 pos) {
            translation = pos;
            Navigatable = NavNodeEnum.VERTEX;
            isActive = true;
        }

        /// <summary>
        /// Make a WAYPOINT and set its Navigational type
        /// </summary>
        /// <param name="pos"> location of WAYPOINT</param>
        /// <param name="nType"> Navigational type {VERTEX, WAYPOINT, A_STAR, PATH} </param>
        public NavNode(Vector3 pos, NavNodeEnum nType) {
            translation = pos;
            Navigatable = nType;
            isActive = true;
        }

    public bool IsActive {
            get {
                return isActive;
            }

            set {
                isActive = value;
            }
        }

        public Vector3 NodeColor {
            get {
                return nodeColor;
            }
        }

        public double Distance {
            get {
                return distance;
            }
            set {
                distance = value;
            }
        }

        public double DistanceFromSource {
            get {
                return distanceFromSource;
            }
            set {
                distanceFromSource = value;
            }
        }

        public double DistanceToGoal {
            get {
                return distanceToGoal;
            }
            set {
                distanceToGoal = value;
            }
        }

        public NavNode Parent {
            get {
                return parent;
            }
            set {
                parent = value;
            }
        }

        /// <summary>
        /// When changing the Navigatable type the WAYPOINT's nodeColor is 
        /// also updated.
        /// </summary>
        public NavNodeEnum Navigatable {
            get {
                return navigatable;
            }
            set {
                navigatable = value;
                switch (navigatable) {
                    case NavNodeEnum.VERTEX: 
                        nodeColor = Color.Yellow.ToVector3(); 
                        break;  

                    case NavNodeEnum.WAYPOINT: 
                        nodeColor = Color.Green.ToVector3(); 
                        break; 

                    case NavNodeEnum.A_STAR: 
                        nodeColor = Color.Blue.ToVector3(); 
                        break;  

                    case NavNodeEnum.PATH: 
                        nodeColor = Color.White.ToVector3(); 
                        break; 

                    case NavNodeEnum.WALL: 
                        nodeColor = Color.Aquamarine.ToVector3(); 
                        break; 
                }
            }
        }

        public Vector3 Translation {
            get {
                return translation;
            }
        }

        /// <summary>
        /// Check if two nodes are at the same 
        /// location in x-z plane.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        public bool IsSameLocation(NavNode n) {
            int x = (int) (Translation.X / 150);
            int z = (int) (Translation.Z / 150);

            int xN = (int) (n.Translation.X / 150);
            int zN = (int) (n.Translation.Z / 150);

            return (x == xN && z == zN);
        }

        /// <summary>
        /// Useful in A* path finding 
        /// when inserting into an min priority queue open set ordered on distance
        /// </summary>
        /// <param name="n"> goal node </param>
        /// <returns> usual comparison values:  -1, 0, 1 </returns>
        public int CompareTo(NavNode n) {
            if (distance < n.Distance)  
                return -1;
            else if (distance > n.Distance) 
                return 1;
            else 
                return 0;
        }
    }
}
