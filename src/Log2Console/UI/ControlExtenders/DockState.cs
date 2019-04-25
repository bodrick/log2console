using System.Drawing;
using System.Windows.Forms;

namespace Log2Console.UI.ControlExtenders
{
    internal struct DockState
    {
        /// <summary>
        ///     the docking control (usually a container class, e.g Panel)
        /// </summary>
        public ScrollableControl Container;

        /// <summary>
        ///     handle of the container that the user can use to select and move the container
        /// </summary>
        public Control Handle;

        /// <summary>
        ///     splitter that is attached to this panel for resizing.
        ///     this is optional
        /// </summary>
        public Splitter Splitter;

        /// <summary>
        ///     the parent of the container
        /// </summary>
        public Control OrgDockingParent;

        /// <summary>
        ///     the base docking host that contains all docking panels
        /// </summary>
        public Control OrgDockHost;

        /// <summary>
        ///     the original docking style, stored in order to reset the state
        /// </summary>
        public DockStyle OrgDockStyle;

        /// <summary>
        ///     the original bounds of the container
        /// </summary>
        public Rectangle OrgBounds;
    }
}