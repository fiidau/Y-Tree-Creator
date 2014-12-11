using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Collections;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Compression;

namespace YTreeCreator
{
    public partial class MainFrm : Form
    {
        bool USE_IGNORE_LIST = true;
        double MATCHING_MUTATIONS_FOR_HG_WHILE_COMPARING = 0.5; // percentage
        bool remove_false_positives = true;
        Hashtable snps_map = new Hashtable();

        string[] ignore_list = new string[] {};

        TreeNode hg_node = null;
        string tree_xml = "";
        string user_snps = "";
        List<Clade> roots = new List<Clade>();
        public MainFrm()
        {
            InitializeComponent();
        }

        private void MainFrm_Load(object sender, EventArgs e)
        {
            this.Text = "Y-Tree-Creator v" + Application.ProductVersion;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            timer1.Enabled = false;

            if (!Directory.Exists("data"))
            {
                MessageBox.Show("Data folder 'data' doesn't exit. Application will exit now!", "Required Folder Missing!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
            else
            {
                statusLbl.Text = "Building tree ... (Please wait)";
                backgroundWorker1.RunWorkerAsync();               
            }
        }


        private void buildTree(TreeNode parent, XmlElement elmt)
        {
            TreeNode tn = null;
            XmlAttribute attrib = null;
            XmlAttribute attrib_val = null;
            attrib = elmt.Attributes["Id"];
            string attrib_value = attrib.Value.Trim();

            attrib_val = elmt.Attributes["HG"];
            string value = attrib_val.Value.Trim();

            if (Clade.REMOVED.Contains(attrib_value))
                return;

            if (attrib_value != "")
            {
                tn = new TreeNode(attrib_value);
                snps_map.Add(tn, value);
                parent.Nodes.Add(tn);
            }

           

            //
            foreach (XmlElement el in elmt.ChildNodes)
            {
                if (tn != null)
                    buildTree(tn, el);
                else
                    buildTree(parent, el);
            }
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            snpTextBox.Text = (string)snps_map[node];
        }

        private void btnSelectOnTree_Click(object sender, EventArgs e)
        {
            user_snps = Regex.Replace(txtSNPs.Text.ToUpper().Trim(), "[\\s]", "");
            MATCHING_MUTATIONS_FOR_HG_WHILE_COMPARING = double.Parse(pMatch.Text) / 100;
            treeView1.CollapseAll();
            foreach (TreeNode node in snps_map.Keys)
            {
                if (node.ForeColor != Color.DarkGreen && node.ForeColor != Color.Red)
                {
                    node.ForeColor = Color.Gray;
                    node.BackColor = Color.White;
                }
            }

            foreach (TreeNode child in treeView1.TopNode.Nodes)
            {
                if (plotOnTree(child, user_snps.Split(new char[] { ',' }).ToList()))
                    break;
            }

            if (hg_node != null)
            {
                lblyhg.Text = hg_node.Text.Trim();
            }
            saveReportToolStripMenuItem.Enabled = true;
        }



        private bool plotOnTree(TreeNode parent, List<string> markers_to_check)
        {            
            List<string> hg_mutations = snps_map[parent].ToString().Split(new char[]{','}).ToList();
            List<string> common = markers_to_check.Intersect(hg_mutations).ToList();

            foreach (string mt in common)
            {
                markers_to_check.Remove(mt);
            }

            if (common.Count >= hg_mutations.Count * MATCHING_MUTATIONS_FOR_HG_WHILE_COMPARING)
           {
               // it's a match
               hg_node = parent;
               parent.ForeColor = Color.White;
               parent.BackColor = Color.DarkGreen;
               parent.EnsureVisible();
               foreach (TreeNode child in parent.Nodes)
               {
                   if (plotOnTree(child, markers_to_check))
                       break;
               }
               return true;
           }
           else 
            return false;
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start("http://www.y-str.org/");
        }

        private string getHtml(string snp_list)
        {
            string html = "";

            string[] list_to_highlight = snp_list.Split(new char[] { ',' });
            string[] user_snps_array = user_snps.Split(new char[] { ',' });

            StringBuilder sb = new StringBuilder();

            foreach(string mutation in list_to_highlight)
            {
                if (!user_snps_array.Contains(mutation))
                    sb.Append("<font color='#8f8f8f' face='Courier New'>" + mutation + "</font>\t");
                else
                    sb.Append("<font color='darkgreen'  face='Courier New'><b>" + mutation + "</b></font>\t");
            }
            html = sb.ToString().Trim().Replace("\t", ", ");

            return html;
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string xml = removeBackMutations(e.Result.ToString());            

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);

            TreeNode tnroot = new TreeNode("Adam");
            treeView1.Nodes.Add(tnroot);

            foreach (XmlElement el in doc.DocumentElement.ChildNodes)
            {
                buildTree(tnroot, el);
            }
            tnroot.Expand();
            int count = 0;
            foreach (TreeNode hg in snps_map.Keys)
            {
                if (hg.Nodes.Count == 0)
                    count++;
            }
            tree_xml = doc.OuterXml;

            buildToolStripMenuItem1.Enabled = true;
            buildButton.Enabled = true;
            statusLbl.Text = "Y-Tree successfully built with " + (snps_map.Count - Clade.REMOVED.Count).ToString() + " haplogroups and " + (count - Clade.REMOVED.Count).ToString() + " terminal clades.";
            textBox1.Enabled = true;
            button2.Enabled = true;
            btnSelectOnTree.Enabled = true;
            plotToolStripMenuItem.Enabled = true;
            saveToolStripMenuItem.Enabled = true;
            expandAllToolStripMenuItem.Enabled = true;
            collapseAllToolStripMenuItem.Enabled = true;
        }

        private string removeBackMutations(string xml)
        {
            if (!remove_false_positives)
                return xml;

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            foreach (XmlNode node in doc.SelectNodes("//Node"))
            {
                if (!node.HasChildNodes && node.Attributes["Id"].Value.ToString().StartsWith("HG"))
                {
                    Clade.REMOVED.Add(node.Attributes["Id"].Value);
                }                
            }
            return doc.OuterXml;
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            string[] files = Directory.GetFiles("data");
            roots = new List<Clade>();
            List<string> mutations = null;
            bool flag_added = false;
            int count = 0;
            foreach (string file in files)
            {
                count++;
                //Console.WriteLine(count + " / " + files.Length + " ... " + Path.GetFileName(file));
                mutations = File.ReadAllText(file).Trim().Replace(" ","").ToUpper().Split(new char[] { ',' }).ToList();
                List<string> to_remove = new List<string>();
                if (USE_IGNORE_LIST)
                {
                    foreach (string i in mutations)
                        foreach (string j in ignore_list)
                            if (i.Substring(1, i.Length - 2) == j || i.StartsWith(j))
                                to_remove.Add(i);
                    foreach (string i in to_remove)
                        mutations.Remove(i);
                }
                if (roots.Count == 0)
                    roots.Add(new Clade(mutations, allowedName(file)));
                else
                {
                    flag_added = false;
                    foreach (Clade root in roots)
                    {
                        if (root.addToClade(mutations, allowedName(file)))
                            flag_added = true;
                    }
                    if (!flag_added)
                        roots.Add(new Clade(mutations, allowedName(file)));
                }

            }
            //Console.WriteLine("Creating XML .. ");
            StringBuilder xml = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n<!-- Y-Tree XML generated by Y-Tree Creator by Felix Chandrakumar, y-str.org -->\r\n<Root>");
            foreach (Clade root in roots)
            {
                xml.Append(root.asXml());
                xml.Append("\r\n");
            }
            xml.Append("</Root>");            
            //Console.WriteLine("All tasks complete!");
            e.Result = xml;
            tree_xml = xml.ToString();
            /////////////////////////////////////////////////
        }

        private string allowedName(string file)
        {
            string tmp = Regex.Replace(Path.GetFileNameWithoutExtension(file), "[^A-Za-z0-9]", " ");
            tmp = tmp.Trim().Replace("  ", " ").Replace(" ", "_");
            return tmp;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            search();
        }

        private void search()
        {
            foreach (TreeNode node in snps_map.Keys)
            {
                if (node.Text.ToLower().IndexOf(textBox1.Text.ToLower()) != -1)
                {
                    node.Expand();
                    node.EnsureVisible();
                    break;
                }
            }
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == '\r' || e.KeyValue == '\n')
            {
                search();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            IgnoreListFrm frm = new IgnoreListFrm(ignore_list);
            frm.ShowDialog(this);
            ignore_list = frm.getMutations();
            frm.Dispose();
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show(" Y-Tree Creator\n ----------------\nVersion: " + Application.ProductVersion + "\n\nDeveloper: Felix Jeyareuben < i@fc.id.au>\nWebsite: y-str.org\n\nY-Tree SNPs from FamilyTreeDNA projects.\r\nApplication Icon is from flameia.com.\nSearch icon is from doublejdesign.co.uk.\n(Icons free for personal/non-commercial use)", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void buildToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            buildButton.PerformClick();
        }

        public byte[] Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return mso.ToArray();
            }
        }

        public byte[] Zip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Y-Tree Files|*.xml.gz";
            if(dlg.ShowDialog(this)==DialogResult.OK)
            {
                string filename=dlg.FileName;
                if (!filename.EndsWith(".xml.gz"))
                    filename = filename + ".xml.gz";
                //StringBuilder sb = new StringBuilder();
                File.WriteAllBytes(filename, Zip(Encoding.UTF8.GetBytes(tree_xml)));
                statusLbl.Text = "Y-Tree saved as " + filename;
            }
        }

        private void websiteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.y-str.org/");
        }

        private void saveReportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                StringBuilder report = new StringBuilder();
                string[] hg = hg_node.FullPath.Split(new char[] { '\\' });
                int i = 0;


                List<TreeNode> list = new List<TreeNode>();
                TreeNode tmp = hg_node;
                while (tmp != null)
                {
                    list.Add(tmp);
                    tmp = tmp.Parent;
                }


                report.Append("<html><head><title>Haplogroup Report for Y-DNA</title></head><body>");
                report.Append("<h1>Haplogroup Report for Y-DNA</h1>");
                report.Append("<b>User Entered Markers: </b><br><font face='Courier New'>" + user_snps.Replace(",", ", ") + "</font>");
                report.Append("<br>");
                report.Append("<br>");
                for (int c = list.Count - 1; c >= 0; c--)
                {
                    tmp = list[c];
                    //for (int o = 0; o < i; o++)
                    //    report.Append("&nbsp;");
                    report.Append("<div style='margin: 5px 5px 5px " + (i + 1) * 50 + "px'");
                    if(tmp.Text.StartsWith("HG"))
                        report.Append(" ⇨ <i><font color='#7f7f7f'>" + tmp.Text + "</font></i> ");
                    else
                        report.Append(" ⇨ <i><font color='black'><b>" + tmp.Text + "</b></font></i> ");
                    report.Append("<div style='background-color: #efefef; padding: 10px; font-family: Courier New'>");
                    if (tmp.Text != "Adam")
                        report.Append(getHtml(snps_map[tmp].ToString()));
                    report.Append("</div></div>");
                    i++;
                }

                report.Append("<br>");
                report.Append("<i>Generated on " + DateTime.Now.ToLongDateString() + " at " + DateTime.Now.ToLongTimeString() + " by <a href='http://www.y-str.org/'>Y-Tree Creator</a></i></body></html>");
                File.WriteAllText(saveFileDialog1.FileName, report.ToString(), Encoding.UTF8);
                Process.Start(saveFileDialog1.FileName);
            }
        }

        private void buildButton_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Building Y-Tree may take some time. I will allow " + int.Parse(mismatchBuild.Text).ToString() + " mismatching SNP(s) as part of the same haplogroup. Please be patient until the build process gets completed. The building process can take a few minutes to even an hour depending on the data files. Do you want to proceed?", "Question?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                remove_false_positives = cbRemoveBackMutations.Checked;
                Clade.ALLOWED_MISMATCHES_FOR_HG_WHILE_BUILDING = int.Parse(mismatchBuild.Text);               
                textBox1.Enabled = false;
                button2.Enabled = false;
                saveReportToolStripMenuItem.Enabled = false;
                btnSelectOnTree.Enabled = false;
                plotToolStripMenuItem.Enabled = false;
                buildToolStripMenuItem1.Enabled = false;
                buildButton.Enabled = false;
                
                roots.Clear();
                treeView1.Nodes.Clear();
                snps_map.Clear();
                timer1.Enabled = true;
            }
        }

        private void plotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnSelectOnTree.PerformClick();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Y-Tree Files|*.xml.gz";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                tree_xml = removeBackMutations(Encoding.UTF8.GetString(Unzip(File.ReadAllBytes(dlg.FileName))));
                Clade.ALLOWED_MISMATCHES_FOR_HG_WHILE_BUILDING = int.Parse(mismatchBuild.Text);
                textBox1.Enabled = false;
                button2.Enabled = false;
                saveReportToolStripMenuItem.Enabled = false;
                btnSelectOnTree.Enabled = false;
                plotToolStripMenuItem.Enabled = false;
                buildToolStripMenuItem1.Enabled = false;
                buildButton.Enabled = false;
                treeView1.Nodes.Clear();
                snps_map.Clear();


                string xml = tree_xml;

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                TreeNode tnroot = new TreeNode("Adam");
                treeView1.Nodes.Add(tnroot);

                foreach (XmlElement el in doc.DocumentElement.ChildNodes)
                {
                    buildTree(tnroot, el);
                }
                tnroot.Expand();

                buildToolStripMenuItem1.Enabled = true;
                buildButton.Enabled = true;
                statusLbl.Text = "Done.";
                textBox1.Enabled = true;
                button2.Enabled = true;
                btnSelectOnTree.Enabled = true;
                plotToolStripMenuItem.Enabled = true;
                saveToolStripMenuItem.Enabled = true;
                expandAllToolStripMenuItem.Enabled = true;
                collapseAllToolStripMenuItem.Enabled = true;
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Y-Tree Export Files|*.html";
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string filename = dlg.FileName;
                if (!filename.EndsWith(".html"))
                    filename = filename + ".html";

                StringBuilder html = new StringBuilder("<html>\r\n<head><title>Y-Tree</title>\r\n<style>\r\n");
                html.Append(YTreeCreator.Properties.Resources.CSS);
                html.Append("\r\n</style>\r\n</head>\r\n<body>");

                html.Append("<h2>Y-Tree</h2><hr>\r\n");


                html.Append("<div class='tree'>\r\n");
                html.Append("<i class='icon-plus-sign-alt expand-icon'></i><i class='icon-folder-open thumb-icon'></i><span class='border'><b>Adam</b></span></span>");
                html.Append("<ul>\r\n");
                foreach (Clade c in roots)
                {
                    html.Append(c.asHtml());
                }
                html.Append("</ul>\r\n");
                html.Append("\r\n</div><br>");

                html.Append("<br><font size='small'><i>Generated on " + DateTime.Now.ToLongDateString() + " at " + DateTime.Now.ToLongTimeString() + " using <a href='http://www.y-str.org/'>Y-Tree Creator</a></i></font></body></html>");

                File.WriteAllText(filename, html.ToString());
                statusLbl.Text = "Y-Tree saved as " + filename;
                Process.Start(filename);
            }
        }

        private void expandAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.ExpandAll();
        }

        private void collapseAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            treeView1.CollapseAll();
            treeView1.TopNode.Expand();
        }
    }
}
