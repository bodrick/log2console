using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Log2Console
{
    public static class Utils
    {
        public static void Export2Excel(ListView listView, string fileName)
        {
            using (var sw = new StreamWriter(fileName, false))
            {
                sw.AutoFlush = true;
                var sb = new StringBuilder();
                foreach (ColumnHeader ch in listView.Columns)
                {
                    sb.Append(ch.Text + ",");
                }

                sb.AppendLine();
                foreach (ListViewItem lvi in listView.Items)
                {
                    foreach (ListViewItem.ListViewSubItem lvs in lvi.SubItems)
                    {
                        if (lvs.Text.Trim() == string.Empty)
                        {
                            sb.Append(" ,");
                        }
                        else
                        {
                            sb.Append(lvs.Text + ",");
                        }
                    }

                    sb.AppendLine();
                }

                sw.Write(sb.ToString());
                sw.Close();
            }

            var fil = new FileInfo(fileName);
            if (fil.Exists)
            {
                MessageBox.Show("Process Completed", "Export to Excel", MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }
    }
}