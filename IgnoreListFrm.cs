using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace YTreeCreator
{
    public partial class IgnoreListFrm : Form
    {
        string mutations = "";
        public IgnoreListFrm(string[] mt)
        {
            mutations = string.Join(", ", mt);
            InitializeComponent();
        }

        private void IgnoreListFrm_Load(object sender, EventArgs e)
        {
            ignore_list.Text=mutations;
            ignore_list.SelectionStart = 0;
            ignore_list.SelectionLength = 0;
        }

        public string[] getMutations()
        {
            return Regex.Replace(ignore_list.Text.Trim(), "[\\s]", "").Split(new char[] { ',' });
        }
    }
}
