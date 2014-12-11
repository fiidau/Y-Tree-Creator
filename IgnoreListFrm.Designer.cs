namespace YTreeCreator
{
    partial class IgnoreListFrm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.ignore_list = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ignore_list
            // 
            this.ignore_list.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ignore_list.Location = new System.Drawing.Point(0, 0);
            this.ignore_list.Multiline = true;
            this.ignore_list.Name = "ignore_list";
            this.ignore_list.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.ignore_list.Size = new System.Drawing.Size(309, 154);
            this.ignore_list.TabIndex = 0;
            // 
            // IgnoreListFrm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(309, 154);
            this.Controls.Add(this.ignore_list);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "IgnoreListFrm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Ignore List";
            this.Load += new System.EventHandler(this.IgnoreListFrm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox ignore_list;
    }
}