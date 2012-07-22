using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AGXNASK {
    /// <summary>
    /// Provides a top 5 line  viewport information display.  
    /// This display can be used for help, game feedback, visual debugging.
    /// Inspector is used with a XNA application designed to have two viewports:
    ///     top viewport for displaying Inspector information
    ///     bottom viewport for display a graphic stage
    ///  
    /// Inspector's client must provide viewport and font values.
    ///  
    /// Inspector manages 20 lines of information display:
    ///    help     5 lines displayed with infoCount == 0 && showHelp, set with setInfo()
    ///    info     5 lines displayed with infoCount == 0 && !showHelp, set with setInfo();
    ///    matrix   5 lines displayed with infoCount == 1, set with setMatrix();
    ///    misc     5 lines displayed with infoCount == 2 && showMisc, set with setInfo();
    ///    
    /// Inspector expects its client's to respond to the following key events:
    ///    H, h  showHelp()  -- toggles between help and info displays for Draw
    ///    I, i  toggles between  help/info, matrix, or misc displays for Draw();
    ///          The display of misc can be enabled/disabled with ShowMisc property.
    /// 
    /// Mike Barnes
    /// 1/ 25 /2010  last updated
    /// </summary>
    public class Inspector {
        /**
         * Constants
         * *********
         */
        /*
         * Number of lines / info display pane
         */
        private const int INFO_PANE_SIZE = 5;   

        /* 
         * Number of total display strings
         */ 
        private const int INFO_DISPLAY_STRING = 20; 

        /*
         * The matrix base
         */
        private const int MATRIX_BASE = 5;

        /*
         * Base to offset for display strings
         */
        private const int FIRST_BASE = 10;  

        /* 
         * second pane for display strings
         */ 
        private const int SECOND_BASE = 15; 

        /**
         * Screen Viewport and text fonts and 
         * display information variables
         * **********************************
         */

        /*
         * General information
         */
        private Viewport infoViewport;

        /*
         * The font of text
         */
        private SpriteFont infoFont;

        /*
         * Color of that font
         */
        private Color fontColor;

        /*
         * The background texture
         */
        private Texture2D infoBackground;

        /*
         * Positions of fonts
         */
        private Vector2[] FontPos;

        /*
         * First 5 are for help 
         */
        private string[] infoString;  

        /*
         * Count variables
         */
        private int infoCount;
        private int infoBase;

        /*
         * Initially show help
         */
        private bool showHelp = true; 
        
        /*
         * Initially don't show matrices
         */
        private bool showMatrices = false;  

        /// <summary>
        /// Inspector's constructor
        /// </summary>
        /// <param name="aDisplay"> the client's GraphicsDevice</param>
        /// <param name="aViewport"> the client's inspector viewport</param>
        /// <param name="informationFont"> font to use -- Courier New</param>     
        public Inspector(GraphicsDevice aDisplay, Viewport aViewport, SpriteFont informationFont, Color color, Texture2D background) {
            infoFont = informationFont;
            infoBackground = background;
            fontColor = color;
            infoCount = 0;  // 0 for first info pane, 1 for second info pane

            // create and initialize display strings
            infoString = new string[INFO_DISPLAY_STRING];
            for (int i = 0; i < INFO_DISPLAY_STRING; i++)
                infoString[i] = " ";

            // initialize font and display strings values
            FontPos = new Vector2[INFO_PANE_SIZE];
            for (int i = 0; i < INFO_PANE_SIZE; i++)
                FontPos[i] = new Vector2(infoViewport.X + 10.0f, infoViewport.Y + (i * infoFont.LineSpacing));
        }

        /// <summary>
        /// Set the boolean flag to show help or information in the first pane
        /// </summary>
        public bool ShowHelp {
            get {
                return showHelp;
            }
            set {
                showHelp = value;
            }
        }

        /// <summary>
        /// Set the boolean flag to show the third, miscellaneous, pane or not
        /// </summary>      
        public bool ShowMatrices {
            get {
                return showMatrices;
            }
            set {
                showMatrices = value;
            }
        }

        /// <summary>
        /// ShowInfo() is an event handler for the client's 'I', 'i' 
        /// user input.  Toggles the information display pane to be drawn.
        /// </summary>
        public void ShowInfo() {
            if (showHelp || showMatrices)
                showHelp = showMatrices = false;
            else
                infoCount = (infoCount + 1) % 2;
        }

        /// <summary>
        /// SetInfo() used to set individual string values for display.
        /// Can be used to set any of 20 (0..19) string values.
        /// Help is considered to be strings 0..4
        /// Info in strings 5..9
        /// Matrix in strings 10..14
        /// Miscellaneous in strings 15..19
        /// </summary>
        /// <param name="stringIndex"> 0..19 index of string to set</param>
        /// <param name="str"> value of string</param>
        public void SetInfo(int stringIndex, string str) {
            if (stringIndex >= 0 && stringIndex < INFO_DISPLAY_STRING)
                infoString[stringIndex] = str;
        }

        /// <summary>
        /// SetMatrices() displays two labeled matrices in the second info pane.
        /// Usually this is a "player's" or "NPavatar's" world, and current camera matrices.
        /// </summary>
        /// <param name="label1"> Description of first matrix</param>
        /// <param name="label2"> Description of second matrix</param>
        /// <param name="m1"> first matrix</param>
        /// <param name="m2"> second matrix</param>
        public void SetMatrices(string label1, string label2, Matrix m1, Matrix m2) {
            infoBase = MATRIX_BASE;
            infoString[infoBase++] =
               string.Format("      | {0,-43:s} |  {1,-43:s}", label1, label2);
            infoString[infoBase++] =
               string.Format("right | {0,10:f2} {1,10:f2} {2,10:f2} {3,10:f2} |  {4,10:f2} {5,10:f2} {6,10:f2} {7,10:f2}",
               m1.M11, m1.M12, m1.M13, m1.M14, m2.M11, m2.M12, m2.M13, m2.M14);
            infoString[infoBase++] =
               string.Format("   up | {0,10:f2} {1,10:f2} {2,10:f2} {3,10:f2} |  {4,10:f2} {5,10:f2} {6,10:f2} {7,10:f2}",
               m1.M21, m1.M22, m1.M23, m1.M24, m2.M21, m2.M22, m2.M23, m2.M24);
            infoString[infoBase++] =
               string.Format(" back | {0,10:f2} {1,10:f2} {2,10:f2} {3,10:f2} |  {4,10:f2} {5,10:f2} {6,10:f2} {7,10:f2}",
               m1.M31, m1.M32, m1.M33, m1.M34, m2.M31, m2.M32, m2.M33, m2.M34);
            infoString[infoBase++] =
               string.Format("  pos | {0,10:f2} {1,10:f2} {2,10:f2} {3,10:f2} |  {4,10:f2} {5,10:f2} {6,10:f2} {7,10:f2}",
               m1.M41, m1.M42, m1.M43, m1.M44, m2.M41, m2.M42, m2.M43, m2.M44);
        }

        /// <summary>
        /// Draw the current information pane in the inspector display.
        /// Called by the client's Draw(...) method.
        /// </summary>
        /// <param name="spriteBatch"> needed to set display strings</param>
        public void Draw(SpriteBatch spriteBatch) {
            Vector2 FontOrigin;
            spriteBatch.Draw(infoBackground, new Vector2(0, 0), Color.White);

            if (showHelp) { // strings 0..4
                for (int i = 0; i < INFO_PANE_SIZE; i++) {
                    FontOrigin = infoFont.MeasureString(infoString[i]);
                    spriteBatch.DrawString(infoFont, infoString[i], FontPos[i], fontColor);
                }
            }
            else if (showMatrices) { // show info  display strings 5..9
                infoBase = MATRIX_BASE;
                for (int i = 0; i < INFO_PANE_SIZE; i++) {
                    FontOrigin = infoFont.MeasureString(infoString[infoBase + i]);
                    spriteBatch.DrawString(infoFont, infoString[infoBase + i], FontPos[i], fontColor);
                }
            }
            else if (infoCount == 0) { // show matrix information strings 10..14
                infoBase = FIRST_BASE;
                for (int i = 0; i < INFO_PANE_SIZE; i++) {
                    FontOrigin = infoFont.MeasureString(infoString[infoBase + i]);
                    spriteBatch.DrawString(infoFont, infoString[infoBase + i], FontPos[i], fontColor);
                }
            }
            else if (infoCount == 1) { // show miscellaneous info stings 15..19
                infoBase = SECOND_BASE;
                for (int i = 0; i < INFO_PANE_SIZE; i++) {
                    FontOrigin = infoFont.MeasureString(infoString[infoBase + i]);
                    spriteBatch.DrawString(infoFont, infoString[infoBase + i], FontPos[i], fontColor);
                }
            }
        }
    }
}
