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
            (_ibxSaver as IDisposable)?.Dispose();
            (_lvhnSaver as IDisposable)?.Dispose();

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
            btnTest = new Button();
            btnParseLvhn = new Button();
            btnClearDatabase = new Button();
            saveExcelFileDialog = new SaveFileDialog();
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
            webView.Size = new Size(1389, 1106);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            // 
            // btnNavigateHome
            // 
            btnNavigateHome.Location = new Point(3, 3);
            btnNavigateHome.Name = "btnNavigateHome";
            btnNavigateHome.Size = new Size(189, 53);
            btnNavigateHome.TabIndex = 1;
            btnNavigateHome.Text = "Go Home";
            btnNavigateHome.UseVisualStyleBackColor = true;
            btnNavigateHome.Click += btnNavigateHome_Click;
            // 
            // btnParseListings
            // 
            btnParseListings.Location = new Point(198, 3);
            btnParseListings.Name = "btnParseListings";
            btnParseListings.Size = new Size(189, 53);
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
            flowLayoutPanel1.Controls.Add(btnParseLvhn);
            flowLayoutPanel1.Controls.Add(btnClearDatabase);
            flowLayoutPanel1.Dock = DockStyle.Top;
            flowLayoutPanel1.Location = new Point(0, 0);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(1389, 82);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // btnCopyUrl
            // 
            btnCopyUrl.Location = new Point(393, 3);
            btnCopyUrl.Name = "btnCopyUrl";
            btnCopyUrl.Size = new Size(191, 53);
            btnCopyUrl.TabIndex = 2;
            btnCopyUrl.Text = "Get/Set URL";
            btnCopyUrl.UseVisualStyleBackColor = true;
            btnCopyUrl.Click += btnCopyUrl_Click;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(590, 3);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(159, 53);
            btnTest.TabIndex = 3;
            btnTest.Text = "Test";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += btnTest_Click;
            // 
            // btnParseLvhn
            // 
            btnParseLvhn.Location = new Point(756, 5);
            btnParseLvhn.Margin = new Padding(4, 5, 4, 5);
            btnParseLvhn.Name = "btnParseLvhn";
            btnParseLvhn.Size = new Size(293, 52);
            btnParseLvhn.TabIndex = 3;
            btnParseLvhn.Text = "Parse lvhn.org doctor list";
            btnParseLvhn.UseVisualStyleBackColor = true;
            btnParseLvhn.Click += btnParseLvhn_Click;
            // 
            // btnClearDatabase
            // 
            btnClearDatabase.Location = new Point(1057, 5);
            btnClearDatabase.Margin = new Padding(4, 5, 4, 5);
            btnClearDatabase.Name = "btnClearDatabase";
            btnClearDatabase.Size = new Size(197, 52);
            btnClearDatabase.TabIndex = 4;
            btnClearDatabase.Text = "Clear Database";
            btnClearDatabase.UseVisualStyleBackColor = true;
            btnClearDatabase.Click += btnClearDatabase_Click;
            // 
            // saveExcelFileDialog
            // 
            saveExcelFileDialog.Filter = "Excel Spreadsheet|*.xlsx";
            // 
            // frmIbxDocParser
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1389, 1188);
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
        private SaveFileDialog saveExcelFileDialog;
        private Button btnTest;
        private Button btnParseLvhn;
        private Button btnClearDatabase;
    }
}