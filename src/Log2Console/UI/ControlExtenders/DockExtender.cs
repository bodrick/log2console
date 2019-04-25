// Copyright 2007 Herre Kuijpers - <herre@xs4all.nl>
//
// This source file(s) may be redistributed, altered and customized
// by any means PROVIDING the authors name and all copyright
// notices remain intact.
// THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED. USE IT AT YOUR OWN RISK. THE AUTHOR ACCEPTS NO
// LIABILITY FOR ANY DATA DAMAGE/LOSS THAT THIS PRODUCT MAY CAUSE.
//-----------------------------------------------------------------------

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Log2Console.UI.ControlExtenders
{
    public class DockExtender
    {
        private readonly Control _dockHost;

        // this is the blue overlay that presents a preview how the control will be docked
        internal Overlay Overlay = new Overlay();

        public DockExtender(Control dockHost)
        {
            _dockHost = dockHost;
            Floaties = new Floaties();
        }

        public Floaties Floaties { get; }

        /// <summary>
        ///     display the container control that is either floating or docked
        /// </summary>
        /// <param name="container"></param>
        public void Show(Control container)
        {
            var f = Floaties.Find(container);
            f?.Show();
        }

        /// <summary>
        ///     this will gracefully hide the container control
        ///     making sure that the floating window is also closed
        /// </summary>
        /// <param name="container"></param>
        public void Hide(Control container)
        {
            var f = Floaties.Find(container);
            f?.Hide();
        }

        /// <summary>
        ///     Attach a container control and use it as a grip handle. The container must support mouse move events.
        /// </summary>
        /// <param name="container">container to make dockable/floatable</param>
        /// <returns>the floaty that manages the container's behaviour</returns>
        public IFloaty Attach(ScrollableControl container) => Attach(container, container, null);

        /// <summary>
        ///     Attach a container and a grip handle. The handle must support mouse move events.
        /// </summary>
        /// <param name="container">container to make dockable/floatable</param>
        /// <param name="handle">grip handle used to drag the container</param>
        /// <returns>the floaty that manages the container's behaviour</returns>
        public IFloaty Attach(ScrollableControl container, Control handle) => Attach(container, handle, null);

        /// <summary>
        ///     attach this class to any dockable type of container control
        ///     to make it dockable.
        ///     Attach a container control and use it as a grip handle. The handle must support mouse move events.
        ///     Supply a splitter control to allow resizing of the docked container
        /// </summary>
        /// <param name="container">control to be dockable</param>
        /// <param name="handle">handle to be used to track the mouse movement (e.g. caption of the container)</param>
        /// <param name="splitter">splitter to resize the docked container (optional)</param>
        /// <exception cref="ArgumentException">container cannot be null</exception>
        public IFloaty Attach(ScrollableControl container, Control handle, Splitter splitter)
        {
            var dockState = new DockState
            {
                Container = container ?? throw new ArgumentException("container cannot be null"),
                Handle = handle ?? throw new ArgumentException("handle cannot be null"),
                OrgDockHost = _dockHost,
                Splitter = splitter
            };

            var floaty = new Floaty(this);
            floaty.Attach(dockState);
            Floaties.Add(floaty);
            return floaty;
        }

        // finds the potential dockhost control at the specified location
        internal Control FindDockHost(Floaty floaty, Point pt)
        {
            Control c = null;
            if (FormIsHit(floaty.DockState.OrgDockHost, pt))
            {
                c = floaty.DockState.OrgDockHost; //assume top level control
            }

            if (floaty.DockOnHostOnly)
            {
                return c;
            }

            foreach (Floaty f in Floaties)
            {
                if (f.DockState.Container.Visible && FormIsHit(f.DockState.Container, pt))
                {
                    // add this line to disallow docking inside floaties
                    //if (f.Visible) continue;

                    c = f.DockState.Container; // found suitable floating form
                    break;
                }
            }

            return c;
        }

        // finds the potential dockhost control at the specified location
        internal bool FormIsHit(Control c, Point pt)
        {
            if (c == null) return false;

            var pc = c.PointToClient(pt);
            var hit = c.ClientRectangle.IntersectsWith(new Rectangle(pc,
                new Size(1, 1))); //.TopLevelControl; // this is tricky
            return hit;
        }
    }
}