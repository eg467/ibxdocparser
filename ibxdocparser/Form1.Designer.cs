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
            saveExcelFileDialog = new SaveFileDialog();
            btnClearDatabase = new Button();
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
            webView.Location = new Point(0, 49);
            webView.Margin = new Padding(2);
            webView.Name = "webView";
            webView.Size = new Size(972, 664);
            webView.TabIndex = 0;
            webView.ZoomFactor = 1D;
            // 
            // btnNavigateHome
            // 
            btnNavigateHome.Location = new Point(2, 2);
            btnNavigateHome.Margin = new Padding(2);
            btnNavigateHome.Name = "btnNavigateHome";
            btnNavigateHome.Size = new Size(132, 32);
            btnNavigateHome.TabIndex = 1;
            btnNavigateHome.Text = "Go Home";
            btnNavigateHome.UseVisualStyleBackColor = true;
            btnNavigateHome.Click += btnNavigateHome_Click;
            // 
            // btnParseListings
            // 
            btnParseListings.Location = new Point(138, 2);
            btnParseListings.Margin = new Padding(2);
            btnParseListings.Name = "btnParseListings";
            btnParseListings.Size = new Size(132, 32);
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
            flowLayoutPanel1.Margin = new Padding(2);
            flowLayoutPanel1.Name = "flowLayoutPanel1";
            flowLayoutPanel1.Size = new Size(972, 49);
            flowLayoutPanel1.TabIndex = 2;
            // 
            // btnCopyUrl
            // 
            btnCopyUrl.Location = new Point(274, 2);
            btnCopyUrl.Margin = new Padding(2);
            btnCopyUrl.Name = "btnCopyUrl";
            btnCopyUrl.Size = new Size(134, 32);
            btnCopyUrl.TabIndex = 2;
            btnCopyUrl.Text = "Get/Set URL";
            btnCopyUrl.UseVisualStyleBackColor = true;
            btnCopyUrl.Click += btnCopyUrl_Click;
            // 
            // btnTest
            // 
            btnTest.Location = new Point(412, 2);
            btnTest.Margin = new Padding(2);
            btnTest.Name = "btnTest";
            btnTest.Size = new Size(111, 32);
            btnTest.TabIndex = 3;
            btnTest.Text = "Test";
            btnTest.UseVisualStyleBackColor = true;
            btnTest.Click += btnTest_Click;
            // 
            // btnParseLvhn
            // 
            btnParseLvhn.Location = new Point(528, 3);
            btnParseLvhn.Name = "btnParseLvhn";
            btnParseLvhn.Size = new Size(205, 31);
            btnParseLvhn.TabIndex = 3;
            btnParseLvhn.Text = "Parse lvhn.org doctor list";
            btnParseLvhn.UseVisualStyleBackColor = true;
            btnParseLvhn.Click += btnParseLvhn_Click;
            // 
            // saveExcelFileDialog
            // 
            saveExcelFileDialog.Filter = "Excel Spreadsheet|*.xlsx";
            // 
            // btnClearDatabase
            // 
            btnClearDatabase.Location = new Point(739, 3);
            btnClearDatabase.Name = "btnClearDatabase";
            btnClearDatabase.Size = new Size(96, 31);
            btnClearDatabase.TabIndex = 4;
            btnClearDatabase.Text = "Clear Database";
            btnClearDatabase.UseVisualStyleBackColor = true;
            btnClearDatabase.Click += btnClearDatabase_Click;
            // 
            // frmIbxDocParser
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(972, 713);
            Controls.Add(webView);
            Controls.Add(flowLayoutPanel1);
            Margin = new Padding(2);
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