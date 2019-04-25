using System.Collections.Generic;
using System.Windows.Forms;

namespace Log2Console.UI.ControlExtenders
{
    /// <summary>
    ///     define a Floaty collection used for enumerating all defined floaties
    /// </summary>
    public class Floaties : List<IFloaty>
    {
        public IFloaty Find(Control container)
        {
            foreach (var floaty in this)
            {
                var f = (Floaty)floaty;
                if (f.DockState.Container.Equals(container))
                {
                    return f;
                }
            }

            return null;
        }
    }
}