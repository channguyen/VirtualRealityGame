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
    /// A non-playing character that moves.  Override the inherited Update(GameTime)
    /// to implement a movement (strategy?) algorithm.
    /// Distribution NPAgent moves along an "exploration" path that is created by
    /// method makePath().  The exploration path is traversed in a reverse path loop.
    /// Paths can also be specified in text files of Vector3 values.  
    /// In this case create the Path with a string argument for the file's address.
    /// </summary>

    public class NPAgent : Agent {
        
        /*
         * NpAgent path mode
         */
        private enum ModeEnum {
            EXPLORING,
            TREASURE_HUNTING
        }

        /*
         * The range of seeing treasure
         */
        private const int DETECT_TREASURE_RADIUS = 4000;

        /*
         * The next node to move
         */
        private NavNode nextGoal;

        /*
         * The current path
         */
        private Path currentPath;

        /*
         * The default path
         */
        private Path previousPath;

        /*
         * The range to open treasure
         */
        private int snapDistance = 10;

        /*
         * The number of turn npAgent made
         */
        private int turnCount = 0;

        /*
         * The current mode
         */
        private ModeEnum mode;

        /*
         * Array to mark treasure found
         */
        private bool[] foundTreasures;

        /*
         * The path queue
         */
        private Queue<Path> treasurePathQueue;

        /*
         * The exploring path queue
         */
        private Queue<Path> explorePathQueue;

        /*
         * Signify all collidable has been loaded
         */
        private bool flagSignal = false;

        /*
         * No more path to move
         */
        private bool done = false;


        /*
         * Private mode of NpAgent
         */
        private Stage.GameMode npAgentGameMode;

        public Stage.GameMode NPAgentGameMode {
            get {
                return npAgentGameMode;
            }
            set {
                npAgentGameMode = value;
            }
        }

        /// <summary>
        /// Create a NPC. 
        /// AGXNASK distribution has npAgent move following a Path.
        /// </summary>
        /// <param name="theStage"> the world</param>
        /// <param name="label"> name of </param>
        /// <param name="pos"> initial position </param>
        /// <param name="orientAxis"> initial rotation axis</param>
        /// <param name="radians"> initial rotation</param>
        /// <param name="meshFile"> Direct X *.x Model in Contents directory </param>

        public NPAgent(Stage theStage, string label, Vector3 pos, Vector3 orientAxis, float radians, string meshFile)
            : base(theStage, label, pos, orientAxis, radians, meshFile) {

            IsCollidable = true;

            first.Name = "npFirst";
            follow.Name = "npFollow";
            above.Name = "npAbove";

            // initialize all treasures
            InitializeMarkTreasures();

            // initialize treasure path queue
            treasurePathQueue = new Queue<Path>();
            // initialize exploring path queue
            explorePathQueue = new Queue<Path>();

            // add all predefined path to explore path queue
            // AddPathToExploreQueue();

            Path initialPath = new Path(stage, MakeExploringPaths(), Path.PathType.SINGLE, true);

             // set it to current path
            currentPath = initialPath;

            // set the mode
            mode = ModeEnum.EXPLORING;
            npAgentGameMode = Stage.GameMode.NP_AGENT_EXPLORING;

            stage.Components.Add(currentPath);
            nextGoal = currentPath.NextNode;
            agentObject.TurnToFace(nextGoal.Translation);
        }

        private void InitializeMarkTreasures() {
            foundTreasures = new bool[Stage.NUM_TREASURES];
            // initially all treasures are not found
            for (int i = 0; i < Stage.NUM_TREASURES; ++i) {
                foundTreasures[i] = false;
            }
        }

        /// <summary>
        /// Default path of AGXNASK 
        /// </summary>
        /// <returns></returns>
        private List<NavNode> MakeRegularPathNodes() {
            List<NavNode> path = new List<NavNode>();
            int spacing = stage.Terrain.Spacing;
            path.Add(new NavNode(new Vector3(505 * spacing, stage.Terrain.SurfaceHeight(505, 505), 505 * spacing),
                     NavNode.NavNodeEnum.VERTEX));
            path.Add(new NavNode(new Vector3(500 * spacing, stage.Terrain.SurfaceHeight(500, 500), 500 * spacing), 
                     NavNode.NavNodeEnum.VERTEX));
            path.Add(new NavNode(new Vector3(495 * spacing, stage.Terrain.SurfaceHeight(495, 495), 495 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(495 * spacing, stage.Terrain.SurfaceHeight(495, 505), 505 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(100 * spacing, stage.Terrain.SurfaceHeight(100, 500), 500 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(100 * spacing, stage.Terrain.SurfaceHeight(100, 100), 100 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(500 * spacing, stage.Terrain.SurfaceHeight(500, 100), 100 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(500 * spacing, stage.Terrain.SurfaceHeight(500, 495), 495 * spacing),
                     NavNode.NavNodeEnum.A_STAR));
            path.Add(new NavNode(new Vector3(495 * spacing, stage.Terrain.SurfaceHeight(495, 105), 105 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(105 * spacing, stage.Terrain.SurfaceHeight(105, 105), 105 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));
            path.Add(new NavNode(new Vector3(105 * spacing, stage.Terrain.SurfaceHeight(105, 495), 495 * spacing),
                     NavNode.NavNodeEnum.WAYPOINT));

            return path;
        }

        /// <summary>
        /// Create exploring path using A* algorithm
        /// to avoid collision
        /// </summary>
        private void AddPathToExploreQueue() {
           /**
            *      We move as follows
            *           + path:
            *                   s -> x1 -> x2 -> x3 -> x4 -> ...
            *                   
            *           + direction:
            *                   right -> down -> right -> up -> right -> down ....
            *                   
            *           + cover an rectangle area of 52 * spacing since the distance
            *             of seeing is 4000 pixles = 26 * 150
            *             
            *        =================
            *        = Map 512 x 512 =
            *        =================
            *     
            *                  100          200          300          400          500
            *      ------------|------------|------------|------------|------------|
            *      |                                                               |
            *      | s ------> x1          x4 ----------->                         | 
            *      |           |            ^            .                         | 
            *      |           |            |            .                         |
            *  100 -           |            |            .                         |
            *      |           |            |            .                         |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *  200 -           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *  300 -           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *      |           |            |                                      |
            *  400 -           |            |                                      |
            *      |           |            |                                      |
            *      |           V            |                                      |
            *      |           x2 ----------> x3                                   |
            *      |                                                               |
            *  500 -----------------------------------------------------------------
            *  
            */
            Vector3 start = new Vector3(0, 0, 0);
            Vector3 goal = new Vector3(0, 0, 0);

            Vector3 x0  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 78),   78  * stage.Spacing);
            Vector3 x1  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 130),  130 * stage.Spacing);
            Vector3 x2  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 182),  182 * stage.Spacing);
            Vector3 x3  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 234),  234 * stage.Spacing);
            Vector3 x4  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 286),  286 * stage.Spacing);
            Vector3 x5  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 338),  338 * stage.Spacing);
            Vector3 x6  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 390),  390 * stage.Spacing);
            Vector3 x7  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 442),  442 * stage.Spacing);
            Vector3 x8  = new Vector3(26 * stage.Spacing, stage.SurfaceHeight(26, 494),  494 * stage.Spacing);


            Vector3 z0  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 26),   26  * stage.Spacing);
            Vector3 z1  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 78),   78  * stage.Spacing);
            Vector3 z2  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 130),  130 * stage.Spacing);
            Vector3 z3  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 182),  182 * stage.Spacing);
            Vector3 z4  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 234),  234 * stage.Spacing);
            Vector3 z5  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 286),  286 * stage.Spacing);
            Vector3 z6  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 338),  338 * stage.Spacing);
            Vector3 z7  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 390),  390 * stage.Spacing);
            Vector3 z8  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 442),  442 * stage.Spacing);
            Vector3 z9  = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 494),  494 * stage.Spacing);

            /*
             * Debugging only to show path pattern
             */
            bool ifDebug = false;
            bool displayPath = ifDebug;

            // right
            start = z0; goal = z1;
            Path p1 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p1);
            }
            explorePathQueue.Enqueue(p1);

            // down 
            start = z1; goal = x0;
            Path p2 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p2);
            }
            explorePathQueue.Enqueue(p2);

            // right
            start = x0; goal = x1;
            Path p3 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p3);
            }
            explorePathQueue.Enqueue(p3);

            // up
            start = x1; goal = z2;
            Path p4 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p4);
            }
            explorePathQueue.Enqueue(p4);

            // right
            start = z2; goal = z3;
            Path p5 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p5);
            }
            explorePathQueue.Enqueue(p5);

            // down 
            start = z3; goal = x2;
            Path p6 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p6);
            }
            explorePathQueue.Enqueue(p6);

            // right 
            start = x2; goal = x3;
            Path p7 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p7);
            }
            explorePathQueue.Enqueue(p7);

            // up 
            start = x3; goal = z4;
            Path p8 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p8);
            }
            explorePathQueue.Enqueue(p8);

            // right 
            start = z4; goal = z5;
            Path p9 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p9);
            }
            explorePathQueue.Enqueue(p9);

            // down 
            start = z5; goal = x4;
            Path p10 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) {
                stage.Components.Add(p10);
            }
            explorePathQueue.Enqueue(p10);

            // right 
            start = x4; goal = x5;
            Path p11 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p11);
            explorePathQueue.Enqueue(p11);

            // up 
            start = x5; goal = z6;
            Path p12 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p12);
            explorePathQueue.Enqueue(p12);

            // right 
            start = z6; goal = z7;
            Path p13 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug) 
                stage.Components.Add(p13);
            explorePathQueue.Enqueue(p13);

            // down 
            start = z7; goal = x6;
            Path p14 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p14);
            explorePathQueue.Enqueue(p14);

            // right 
            start = x6; goal = x7;
            Path p15 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p15);
            explorePathQueue.Enqueue(p15);

            // up 
            start = x7; goal = z8;
            Path p16 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p16);
            explorePathQueue.Enqueue(p16);

            // right 
            start = z8; goal = z9;
            Path p17 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p17);
            explorePathQueue.Enqueue(p17);

            // down 
            start = z9; goal = x8;
            Path p18 = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.WAYPOINT), Path.PathType.SINGLE, displayPath);
            if (ifDebug)
                stage.Components.Add(p18);
            explorePathQueue.Enqueue(p18);
       }

        /// <summary>
        /// The initial exploring path. It might
        /// look redundant but it's very important because
        /// we have to wait for stage to load all collidable 
        /// object. If we construct the whole path without waiting
        /// for the stage, our A* path will incorrect since there 
        /// are no collidable nodes.
        /// </summary>
        /// <returns>a list of nav nodes</returns>
        private List<NavNode> MakeExploringPaths() {
            Vector3 u = new Vector3(486 * stage.Spacing, stage.SurfaceHeight(486, 26), 26 * stage.Spacing);
            List<NavNode> nodes = new List<NavNode>();
            nodes.Add(new NavNode(u, NavNode.NavNodeEnum.VERTEX));
            return nodes;
        }

        /// <summary>
        /// Check to see if npAgent is collide with a
        /// particular location
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public bool IsCollideWithThisLocation(Vector3 location) {
            return AgentObject.IsCollideWith(location);
        }

        /// <summary>
        /// Check to see if a node is already in a set
        /// based on their position NOT distance
        /// </summary>
        /// <param name="current">a NavNode</param>
        /// <param name="set">the current set (either closeSet or openSet)</param>
        /// <returns>true if it's in, false otherwise</returns>
        private bool IsNodeIn(NavNode current, PriorityQueue<NavNode> set) {
            List<NavNode> nodes = set.GetList();
            foreach (NavNode n in nodes) {
                if (n.Translation.X == current.Translation.X && n.Translation.Z == current.Translation.Z)
                    return true;
            }
            return false;
        }

        /// <summary>
        ///       ----------------
        ///       - A* Algorithm -
        ///       ----------------
        ///       
        /// OPEN = priority queue contain START
        /// CLOSED = empty set
        /// 
        /// while lowest rank in OPEN is not the GOAL:
        ///     current = remove lowest rank item from OPEN
        ///     add current to CLOSED
        ///     
        ///     for neighbors of current
        ///         cost = g(current) + movement_cost(current, neighbor)
        ///         
        ///         if neighbor in OPEN and cost less than g(neighbor)
        ///             remove neighbor from OPEN, because new path is better
        ///             
        ///         if neighbor in CLOSED and cost less than g(neighbor)
        ///             remove neighbor from CLOSED
        ///             
        ///         if neighbor not in OPEN and neighbor not in CLOSED
        ///             set g(neighbor) to cost
        ///             add neighbor to OPEN
        ///             set priority queue rank to g(neighbor) + h(neighbor)
        ///             set neighbor's parent to current
        ///             
        /// 
        /// reconstruct reverse path from goal to start
        /// by following parent pointers
        ///  
        /// </summary>
        /// <param name="startPostion">start</param>
        /// <param name="goalPosition">destination</param>
        /// <param name="nodeType">color of node</param>
        /// <returns></returns>
        private List<NavNode> MakeAStarPath(Vector3 startPostion, Vector3 goalPosition, NavNode.NavNodeEnum nodeType) {
            /** 
             * A* implementation
             *      Summary:
             *              1) Add the starting node to the open set
             *              2) Repeat the following:
             *                      a) Look for the lowest F cost on the open set. 
             *                      b) Move it to the closed set
             *                      c) For each of the 8 adjacency node to the current node:
             *                              + If it is NOT walkable or if it is on the CLOSED SET, just ignore it.
             *                              + Otherwise:
             *                                  - If it is NOT on the OPEN SET, add it to the OPEN SET. Make the "current node" as 
             *                                    parent of this adjacency node. Record F, G, H for this node.
             *                                  - If it is on the OPEN SET already, check to see if this path to that square is 
             *                                    better using G cost as the measure. A lower G means that this is a better path. 
             *                                    If so, change the parent of the node to the "current node", and recalculate
             *                                    the G and F cost of the node. 
             *                      d) Stop when we:
             *                              + Add the goal node to the closed set, in which case the path has been found.
             *                              + Fail to find the goal node, and the open set is empty. In this case, there is NO path.
             */
            // spacing between node on map (= 150)
            int spacing = stage.Terrain.Spacing;

            /**
             * A* path
             *      this is our final path
             */
            List<NavNode> path = new List<NavNode>();

            /**
             * The starting point 
             */
            NavNode start = new NavNode(startPostion);
            start.DistanceFromSource = 0.0;
            start.DistanceToGoal = 0.0;
            start.Distance = 0.0;
            start.Parent = null;

            /**
             * The goal
             */
            NavNode goal = new NavNode(goalPosition);
            goal.DistanceFromSource = 0.0;
            goal.DistanceToGoal = 0.0;
            goal.Distance = 0.0;
            goal.Parent = null;

            // open set 
            PriorityQueue<NavNode> openSet = new PriorityQueue<NavNode>();

            // close set
            PriorityQueue<NavNode> closedSet = new PriorityQueue<NavNode>();

            // add starting point to open set (part 1) 
            openSet.Add(start);

           
            while (!openSet.Empty) {

                // get the current node with lowest cost F and remove it from open set
                NavNode current = openSet.Pop();

                // add current to close set
                closedSet.Add(current);

                // if it's equal to our goal, we're done (part d)
                if (current.IsSameLocation(goal)) {
                    while (current.Parent != null) {
                        path.Add(current);
                        current.Navigatable = nodeType;
                        current = current.Parent;
                    }
                    path.Reverse();
                    return path;
                }
                else {
                    // for each of the 8 adjacency neighbors 
                    // NOTE: the neighbor list already removed un-walkable nodes
                    List<NavNode> neighbors = GetNeighbors(current.Translation);

                    foreach (NavNode n in neighbors) {
                        // if it's on the closed set, just ignore it
                        if (IsNodeIn(n, closedSet)) {
                            continue;
                        }
                        else {
                            if (!IsNodeIn(n, openSet)) {
                                // make the "current node" as parent of this neighbor
                                n.Parent = current;
                                // record new F, G, H
                                n.DistanceFromSource = current.DistanceFromSource + CalculateDistanceFromSource(current, n);
                                n.DistanceToGoal = CalculateHeuristicDinstanceToGoal(n, goal);
                                n.Distance = n.DistanceFromSource + n.DistanceToGoal;

                                // add this neighbor to the OPEN SET
                                openSet.Add(n);
                            }
                            else { // it's already on the OPEN SET
                                double costFromThisPathToN = current.DistanceFromSource + CalculateDistanceFromSource(current, n);
                                // we have a better path, going from "current node"
                                if (costFromThisPathToN < n.DistanceFromSource) {
                                    // recalculate G and F for this neighbor
                                    n.Parent = current;
                                    n.DistanceFromSource = costFromThisPathToN;
                                    n.Distance = n.DistanceFromSource + n.DistanceToGoal;
                                }
                            }
                        }
                    }
                }
            }

            return path;
        }

        /// <summary>
        /// Calculate the distance between a node n
        /// and its neighbor m. It's either
        /// 150 or 150 * \sqrt{2}
        /// </summary>
        /// <param name="n">a node n</param>
        /// <param name="m">neighbor of m</param>
        /// <returns>the distance between 2 nodes</returns>
        public double CalculateDistanceFromSource(NavNode n, NavNode m) {
            // first we convert their actual location
            // to node on graph. Then we truncate
            // all the decimal points. So it returns
            // a pair of (x, z) -> (512, 512)
            int xN = (int)(n.Translation.X / 150);
            int zN = (int)(n.Translation.Z / 150);

            int xM = (int)(m.Translation.X / 150);
            int zM = (int)(m.Translation.Z / 150);

            // next we find their location
            int temp = Math.Abs(xN - xM) + Math.Abs(zN - zM);
            // this is corner
            if (temp == 2)
                return stage.Spacing * Math.Sqrt(2);
            else
                return stage.Spacing;
        }

        /// <summary>
        /// Calculate the distance between a node n
        /// and its goal using Manhattan formula.
        /// We count the number squares vertically and 
        /// horizontally to from n to the goal
        /// </summary>
        /// <param name="n">a node on graph</param>
        /// <param name="goal">a goal</param>
        /// <returns>their distance</returns>
        public double CalculateHeuristicDinstanceToGoal(NavNode n, NavNode goal) {
            // first we convert their actual location
            // to node on graph. Then we truncate
            // all the decimal points. So it returns
            // a pair of (x, z) -> (512, 512)
            int xN = (int)(n.Translation.X / 150);
            int zN = (int)(n.Translation.Z / 150);

            int xGoal = (int)(goal.Translation.X / 150);
            int zGoal = (int)(goal.Translation.Z / 150);

            // then we take the number squares times 150
            return stage.Spacing * (Math.Abs(xN - xGoal) + Math.Abs(zN - zGoal));
        }

        /// <summary>
        /// Check to see if there is any treasure 
        /// around user's location
        /// </summary>
        /// <returns>the index of the treasure, -1 if not found</returns>
        private int DetectTreasure() {
            for (int i = 0; i < stage.Treasures.Count; ++i) {
                if (!stage.Treasures[i].IsOpen && Vector3.Distance(agentObject.Translation, stage.Treasures[i].Translation) < DETECT_TREASURE_RADIUS) {
                    stage.Treasures[i].OpenIt();
                    AddTaggedTreasure();
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Get all neighbors of a node on map.
        /// There are 512 x 512 nodes
        /// and 8 possible neighbors for each node
        /// </summary>
        /// <param name="node">a position on map</param>
        /// <returns></returns>
        public List<NavNode> GetNeighbors(Vector3 node) {
            // initialize collection 
            List<NavNode> neighbors = new List<NavNode>();

            // convert to x, z coordinate to map to (512, 512)
            int x = (int)(node.X / 150);
            int z = (int)(node.Z / 150);
            int spacing = stage.Spacing;
            Terrain terrain = stage.Terrain;

            /*
             *      8 possible adjacent neighbors
             * ----------------------------------------------------
             * | (x - 1, z - 1) | (x, z - 1)    | (x + 1, z - 1)  |
             * ----------------------------------------------------
             * | (x - 1, z)     | (x, z)        | (x + 1, z)      |
             * ----------------------------------------------------
             * | (x - 1, z + 1) | (x, z + 1)    | (x + 1, z + 1)  |
             * ----------------------------------------------------
             */
            // left
            if (IsInRange(x - 1, z)) {
                Vector3 left = new Vector3((x - 1) * spacing, terrain.SurfaceHeight(x - 1, z), z * spacing);
                if (IsWalkable(left)) {
                    NavNode n = new NavNode(left, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // right
            if (IsInRange(x + 1, z)) {
                Vector3 right = new Vector3((x + 1) * spacing, terrain.SurfaceHeight(x + 1, z), z * spacing);
                if (IsWalkable(right)) {
                    NavNode n = new NavNode(right, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // up
            if (IsInRange(x, z - 1)) {
                Vector3 up = new Vector3(x * spacing, terrain.SurfaceHeight(x, z - 1), (z - 1) * spacing);
                if (IsWalkable(up)) {
                    NavNode n = new NavNode(up, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // down
            if (IsInRange(x, z + 1)) {
                Vector3 down = new Vector3(x * spacing, terrain.SurfaceHeight(x, z + 1), (z + 1) * spacing);
                if (IsWalkable(down)) {
                    NavNode n = new NavNode(down, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // upper left
            if (IsInRange(x - 1, z - 1)) {
                Vector3 upperLeft = new Vector3((x - 1) * spacing, terrain.SurfaceHeight(x - 1, z - 1), (z - 1) * spacing);
                if (IsWalkable(upperLeft)) {
                    NavNode n = new NavNode(upperLeft, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // upper right
            if (IsInRange(x + 1, z - 1)) {
                Vector3 upperRight = new Vector3((x + 1) * spacing, terrain.SurfaceHeight(x + 1, z - 1), (z - 1) * spacing);
                if (IsWalkable(upperRight)) {
                    NavNode n = new NavNode(upperRight, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // lower left
            if (IsInRange(x - 1, z + 1)) {
                Vector3 lowerLeft = new Vector3((x - 1) * spacing, terrain.SurfaceHeight(x - 1, z + 1), (z + 1) * spacing);
                if (IsWalkable(lowerLeft)) {
                    NavNode n = new NavNode(lowerLeft, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            // lower Right 
            if (IsInRange(x + 1, z + 1)) {
                Vector3 lowerRight = new Vector3((x + 1) * spacing, terrain.SurfaceHeight(x + 1, z + 1), (z + 1) * spacing);
                if (IsWalkable(lowerRight)) {
                    NavNode n = new NavNode(lowerRight, NavNode.NavNodeEnum.A_STAR);
                    neighbors.Add(n);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Check if a node on map is walkable 
        /// </summary>
        /// <param name="position">position on map</param>
        /// <returns>true/false</returns>
        private bool IsWalkable(Vector3 position) {
            int xPosition = (int)(position.X / 150);
            int zPosition = (int)(position.Z / 150);

            Vector3 stopLocation = new Vector3(position.X, stage.Terrain.SurfaceHeight(xPosition, zPosition), position.Z);
            if (IsCollideWithThisLocation(stopLocation))
                return false;
            return true;
        }

        /// <summary>
        /// Check if a node coordinate is in 
        /// valid range (0, 0) -> (512, 512)
        /// </summary>
        /// <param name="x">coordinate in x plane</param>
        /// <param name="z">coordinate in z plane</param>
        /// <returns>true/false</returns>
        private bool IsInRange(int x, int z) {
            return (x >= 0 && x < stage.Range && z >= 0 && z < stage.Range);
        }
        
        /// <summary>
        /// Here we allow switching from regular path to treasure path if only if 
        /// the treasure path is not done, otherwise it will do nothing (disable)
        /// </summary>
        private void ChangeToTreasurePath(int idx) {
            AddNewTreasurePath(idx);

            // save the previous path if any
            if (mode == ModeEnum.EXPLORING) {
                if (!currentPath.Done) {
                    previousPath = currentPath;
                }
            }

            // update the mode
            mode = ModeEnum.TREASURE_HUNTING;
            // set new path
            currentPath = treasurePathQueue.Dequeue();
            // add path to stage
            stage.Components.Add(currentPath);
            // get the first target
            nextGoal = currentPath.NextNode;
            // orient towards the first path goal
            agentObject.TurnToFace(nextGoal.Translation);
        }

        /// <summary>
        /// Build an A* path to treasure
        /// and add it to the treasure path queue
        /// </summary>
        /// <param name="idx">index of treasure</param>
        private void AddNewTreasurePath(int idx) {
            // build A* path to goal
            int startX = (int)(agentObject.Translation.X / 150);
            int startZ = (int)(agentObject.Translation.Z / 150);
            int goalX = (int)(stage.Treasures[idx].Translation.X / 150);
            int goalZ = (int)(stage.Treasures[idx].Translation.Z / 150);

            Vector3 start =
                new Vector3(startX * stage.Terrain.Spacing, stage.Terrain.SurfaceHeight(startX, startZ), startZ * stage.Terrain.Spacing);
            Vector3 goal =
                new Vector3(goalX * stage.Terrain.Spacing, stage.Terrain.SurfaceHeight(goalX, goalZ), goalZ * stage.Terrain.Spacing);

            // create A* path forward to treasure
            Path forwardPath = new Path(stage, MakeAStarPath(start, goal, NavNode.NavNodeEnum.A_STAR), Path.PathType.SINGLE, true);
            // add this path to queue
            treasurePathQueue.Enqueue(forwardPath);

            // create A* path backward to previous position
            if (goalX == LocationConstant.TREASURE_1_X && goalZ == LocationConstant.TREASURE_1_Z) {
                Path backwardPath = new Path(stage, MakeAStarPath(goal, start, NavNode.NavNodeEnum.PATH), Path.PathType.SINGLE, true);
                // add this path to queue
                treasurePathQueue.Enqueue(backwardPath);
            }
        }

        public Object3D AgentObject {
            get {
                return agentObject;
            }
        }

        private void displayAgentInfo() {
            // display information about agent
            stage.SetInfo(15,
               string.Format("npAvatar:  Location ({0:f0},{1:f0},{2:f0})  Looking at ({3:f2},{4:f2},{5:f2})",
                  agentObject.Translation.X, agentObject.Translation.Y, agentObject.Translation.Z,
                  agentObject.Forward.X, agentObject.Forward.Y, agentObject.Forward.Z));

            stage.SetInfo(16,
               string.Format("nextGoal:  ({0:f0},{1:f0},{2:f0})", nextGoal.Translation.X, nextGoal.Translation.Y, nextGoal.Translation.Z));
        }

        private void Move() {
            // update mode for each move
            if (mode == ModeEnum.EXPLORING)
                npAgentGameMode = Stage.GameMode.NP_AGENT_EXPLORING;
            else if (mode == ModeEnum.TREASURE_HUNTING)
                npAgentGameMode = Stage.GameMode.NP_AGENT_TREASURE_HUNTING;

            if (!flagSignal) {
                AddPathToExploreQueue();
                flagSignal = true;
            }

            displayAgentInfo();

            // see if at or close to nextGoal, distance measured in the flat XZ plane
            float distance = Vector3.Distance(
               new Vector3(nextGoal.Translation.X, 0, nextGoal.Translation.Z),
               new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z));

            if (distance <= snapDistance) {
                stage.SetInfo(17, string.Format("distance to goal = {0,5:f2}", distance));

                // snap to nextGoal and orient toward the new nextGoal 
                nextGoal = currentPath.NextNode;
                agentObject.TurnToFace(nextGoal.Translation);

                /*
                 * Here we have two options when the current path is done
                 *      if npAgent is in TREASURE_HUNTING mode
                 *      if npAgent is in EXPLORING mode
                 */
                if (currentPath.Done) {
                    Debug.WriteLine("Path traversal is done.");
                    if (mode == ModeEnum.TREASURE_HUNTING) {
                        HandleTreasureMode(); 
                    }
                    else if (mode == ModeEnum.EXPLORING) {
                        HandleExploringMode(); 
                    }
                }
                else {
                    turnCount++;
                    // stage.SetInfo(18, string.Format("turnToFace count = {0}", turnCount));
                }
            }
        }

        private void HandleTreasureMode() {
            if (treasurePathQueue.Count == 0) {
                // switch mode
                mode = ModeEnum.EXPLORING;
                npAgentGameMode = Stage.GameMode.NP_AGENT_EXPLORING;

                // resume to previous exploring path if any
                if (previousPath != null && !previousPath.Done) {
                    currentPath = previousPath;
                    // nextGoal = currentPath.NextNode;
                    // agentObject.TurnToFace(nextGoal.Translation);
                }
                else {
                    HandleExploringMode();
                }
            }
            else {
                // get another path from treasure path queue
                currentPath = treasurePathQueue.Dequeue();
                // add path to stage two show trace
                stage.Components.Add(currentPath);
                // get first path goal
                nextGoal = currentPath.NextNode;
                // orient towards the first path goal
                agentObject.TurnToFace(nextGoal.Translation);
            }
        }

        private void HandleExploringMode() {
            if (explorePathQueue.Count == 0) {
                done = true;
                npAgentGameMode = Stage.GameMode.NP_AGENT_TREASURE_STOP;
            }
            else {
                // get another path from explore path queue
                currentPath = explorePathQueue.Dequeue();
                // get first path goal
                nextGoal = currentPath.NextNode;
                // orient towards the first path goal
                agentObject.TurnToFace(nextGoal.Translation);
            }
        }

        /// <summary>
        /// A very simple limited random walk.  Repeatedly moves skipSteps forward then
        /// randomly decides how to turn (left, right, or not to turn).  Does not move
        /// very well -- its just an example...
        /// </summary>
        public override void Update(GameTime gameTime) {
            // find a treasure withing detection range
            int idx = DetectTreasure();

            // there is still treasure
            if (idx != -1) {
                npAgentGameMode = Stage.GameMode.NP_AGENT_TREASURE_HUNTING;
                ChangeToTreasurePath(idx);
            }

            if (!done)
                Move();
            
            base.Update(gameTime);
        }

        public new void AddTaggedTreasure() {
            base.AddTaggedTreasure();
        }

        public int GetTaggedTreasure() {
            return taggedTreasures;
        }
    }
}