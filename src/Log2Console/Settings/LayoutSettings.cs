using System.Drawing;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Log2Console.Settings
{
    public class LayoutSettings
    {
        public Rectangle WindowPosition { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public FormWindowState WindowState { get; set; }

        public bool ShowLogDetailView { get; set; }

        public Size LogDetailViewSize { get; set; }

        public bool ShowLoggerTree { get; set; }

        public Size LoggerTreeSize { get; set; }

        public int[] LogListViewColumnsWidths { get; set; }

        public void Set(Rectangle position, FormWindowState state, Control detailView, Control loggerTree)
        {
            WindowPosition = position;
            WindowState = state;
            ShowLogDetailView = detailView.Visible;
            LogDetailViewSize = detailView.Size;
            ShowLoggerTree = loggerTree.Visible;
            LoggerTreeSize = loggerTree.Size;
        }
    }
}