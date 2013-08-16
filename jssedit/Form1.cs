using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace jssedit
{
    public partial class Form1 : Form
    {
        public string Filename;
        public bool Modified;

        public Form1()
        {
            InitializeComponent();
        }

        public void SetGraph(Graph g)
        {
            button1.SetGraph(g);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSS document (*.jss)|*.jss|All files (*.*)|*.*",
                Title = "Open document",
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                try
                {           
                    using (var file = new FileStream(dialog.FileName, FileMode.Open))
                    {
                        var formatter = new BinaryFormatter();
                        var graph = formatter.Deserialize(file) as Graph;
                        if (graph != null)
                        {
                            button1.SetGraph(graph);
                            Filename = dialog.FileName;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error during open: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Filename == null)
            {
                saveAsToolStripMenuItem_Click(sender, e);
                return;
            }

            // save test
            try
            {
                using (var file = new FileStream(Filename, FileMode.Create))
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(file, button1.GetGraph());
                }
            }
            catch (Exception ex)
            {
                 MessageBox.Show("Error during save: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);   
            }
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSS document (*.jss)|*.jss|All files (*.*)|*.*",
                Title = "Save as",
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Filename = dialog.FileName;
                saveToolStripMenuItem_Click(sender, e);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void splitter1_SplitterMoved(object sender, SplitterEventArgs e)
        {

        }


    }
}
