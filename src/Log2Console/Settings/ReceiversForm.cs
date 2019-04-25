using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Log2Console.Receiver;

namespace Log2Console.Settings
{
    public partial class ReceiversForm : Form
    {
        public ReceiversForm(IEnumerable<IReceiver> receivers, bool quickFileOpen = false)
        {
            AddedReceivers = new List<IReceiver>();
            RemovedReceivers = new List<IReceiver>();

            InitializeComponent();
            removeReceiverBtn.Visible = !quickFileOpen;

            // Populate Receiver Types
            var receiverTypes = ReceiverFactory.Instance.ReceiverTypes;
            foreach (var kvp in receiverTypes)
            {
                ToolStripItem item = null;

                if (quickFileOpen)
                {
                    if (kvp.Value.Type == typeof(CsvFileReceiver))
                    {
                        item = addReceiverCombo.DropDownItems.Add(kvp.Value.Name);
                    }
                }
                else
                {
                    item = addReceiverCombo.DropDownItems.Add(kvp.Value.Name);
                }

                if (item != null) item.Tag = kvp.Value;
            }

            // Populate Existing Receivers
            foreach (var receiver in receivers)
            {
                AddReceiver(receiver);
            }
        }

        public List<IReceiver> AddedReceivers { get; protected set; }
        public List<IReceiver> RemovedReceivers { get; protected set; }
        public IReceiver SelectedReceiver { get; protected set; }

        private void AddReceiver(IReceiver receiver)
        {
            var displayName = string.IsNullOrEmpty(receiver.DisplayName)
                ? ReceiverUtils.GetTypeDescription(receiver.GetType())
                : receiver.DisplayName;
            var lvi = receiversListView.Items.Add(displayName);
            lvi.Tag = receiver;
            lvi.Selected = true;
        }


        private void AddReceiverCombo_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem.Tag is ReceiverFactory.ReceiverInfo info)
            {
                // Instantiates a new receiver based on the selected type
                var receiver = ReceiverFactory.Instance.Create(info.Type.FullName);

                AddedReceivers.Add(receiver);
                AddReceiver(receiver);
            }
        }

        private void RemoveReceiverBtn_Click(object sender, EventArgs e)
        {
            var receiver = GetSelectedReceiver();
            if (receiver == null)
            {
                return;
            }

            var dr = MessageBox.Show(this, "Confirm Delete?", "Confirmation", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.Yes)
            {
                return;
            }

            receiversListView.Items.Remove(GetSelectedItem());

            if (AddedReceivers.Find(r => r == receiver) != null)
            {
                AddedReceivers.Remove(receiver);
            }
            else
            {
                RemovedReceivers.Add(receiver);
            }
        }

        private void ReceiversListView_SelectedIndexChanged(object sender, EventArgs e)
        {
            var receiver = GetSelectedReceiver();

            removeReceiverBtn.Enabled = receiver != null;
            receiverPropertyGrid.SelectedObject = receiver;

            if (receiver != null)
            {
                sampleClientConfigTextBox.Text = receiver.SampleClientConfig;
                SelectedReceiver = receiver;
            }
        }

        private ListViewItem GetSelectedItem() =>
            receiversListView.SelectedItems.Count > 0 ? receiversListView.SelectedItems[0] : null;

        private IReceiver GetSelectedReceiver()
        {
            if (receiversListView.SelectedItems.Count <= 0)
            {
                return null;
            }

            var lvi = GetSelectedItem();
            return lvi?.Tag as IReceiver;
        }
    }
}