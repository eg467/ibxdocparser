namespace ibxdocparser
{
    partial class frmIbxDocParser
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            btnNavigateHome = new Button();
            btnParseListings = new Button();
            flowLayoutPanel1 = new FlowLayoutPanel();
            btnCopyUrl = new Button();
            saveFileDialog1 = new SaveFileDialog();
            btnTest = new Button();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            flowLayoutPanel1.SuspendLayout();
            SuspendLayout();
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Dock = DockStyle.Fill;
            webView.Location = new Point(0, 82);
            webView.Name = "webView";
            webView.Size = new Size(1388, 1107);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            // 
            // btnNavigateHome
            // 
            btnNavigateHome.Location = new Point(3, 3);
            btnNavigateHome.Name = "btnNavigateHome";
            btnNavigateHome.Size = new Size(189, 54);
            btnNavigateHome.TabIndex = 1;
            btnNavigateHome.Text = "Go Home";
            btnNavigateHome.UseVisualStyleBackColor = true;
            btnNavigateHome.Click += btnNavigateHome_Click;
            // 
            // btnParseListings
            // 
            btnParseListings.Location = new Point(198, 3);
            btnParseListings.Name = "btnParseListings";
            btnParseListings.Size = new Size(189, 54);
            btnParseListings.TabIndex = 1;
            btnParseListings.Text = "Parse Listings";
            btnParseListings.UseVisualStyleBackColor = true;
            btnParseListings.Click += btnParseListings_Click;
            // 
            // flowLayoutPanel1
            // 
            flowLayoutPanel1.Controls.Add(btnNavigateHome);
            flowLayoutPanel1.Controls.Add(btnParseListings);
            flowLayoutPanel1.Controls.Add(btnCopyUrl);
            flowLayoutPanel1.Controls.Add(btnTest);
            flowLayoutPanel1.Dock = DockStyle.Top;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1388, 82);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // btnCopyUrl
            // 
            btnCopyUrl.Location = new Point(393, 3);
            btnCopyUrl.Name = "btnCopyUrl";
            btnCopyUrl.Size = new Size(191, 54);
            btnCopyUrl.TabIndex = 2;
            btnCopyUrl.Text = "Get/Set URL";
            btnCopyUrl.UseVisualStyleBackColor = true;
            btnCopyUrl.Click += btnCopyUrl_Click;
            // 
            // saveFileDialog1
            // 
            saveFileDialog1.Filter = "Excel Spreadsheet|*.xlsx";
            // 
            // btnTest
            // 
            btnTest.Location = new Point(590, 3);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(158, 54);
            btnTest.TabIndex = 3;
            btnTest.Text = "Test";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += btnTest_Click;
            // 
            // frmIbxDocParser
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1388, 1189);
            Controls.Add(webView);
            Controls.Add(flowLayoutPanel1);
            Name = "frmIbxDocParser";
            Text = "IBX Doctor Parser";
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            flowLayoutPanel1.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button btnNavigateHome;
        private Button btnParseListings;
        private FlowLayoutPanel flowLayoutPanel1;
        private Button btnCopyUrl;
        private SaveFileDialog saveFileDialog1;
        private Button btnTest;
    }
}