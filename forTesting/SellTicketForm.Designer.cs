namespace TicketCSharp.Forms;

partial class SellTicketForm
{

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

        /// <summary>
        /// Required method for Designer support - do not modify
        /// using code generator.
        /// </summary>
        private void InitializeComponent()
        {

            components = new System.ComponentModel.Container();
            adultCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
            childCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
            oldCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
            label1.Font = boldFont;
            label2.Font = boldFont;
            label3.Font = boldFont;
            label4.Font = boldFont;
            groupTypeBox.Font = boldFont;
            label5.Font = boldFont;
            groupNumBox.Font = boldFont;
            /*      label6.Font = boldFont;
            regionBox.Font = boldFont;
            */
            // label8.Font = boldFont;
            // label8.Location = new Point(310, 180);
            // label8.Name = "label8";
            // label8.Text = "Payment Mode";
            // label8.AutoSize = true;
            flowLayoutPanel.Font = boldFont;
            btnConfirm.Font = boldFont;
            btnCash.Font = boldFont;
            /*         btnStuff.Font = boldFont;
            menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            toolsToolStripMenuItem
            toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            receivedCashToolStripMenuItem,
            cashReportToolStripMenuItem,
            openDaysToolStripMenuItem
            statusStrip.Items.AddRange(new ToolStripItem[] {
            connectionStatusLabel,
            statusSeparator1,
            userStatusLabel,
            statusSeparator2,
            salesStatusLabel,
            statusSeparator3,
            appStatusLabel
            FormBorderStyle = FormBorderStyle.FixedDialog;
            return;
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.receivedCashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cashReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openDaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.connectionStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSeparator1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.userStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSeparator2 = new System.Windows.Forms.ToolStripStatusLabel();
            this.salesStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSeparator3 = new System.Windows.Forms.ToolStripStatusLabel();
            this.appStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            statusRefreshTimer = new System.Windows.Forms.Timer(components);
            var boldFont = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
            this.btnCashReport = new System.Windows.Forms.Button();
            btnCashReport.Font = boldFont;
            this.btnReceiveCash = new System.Windows.Forms.Button();
            btnReceiveCash.Font = boldFont;
            this.btnCash = new System.Windows.Forms.Button();
            btnStuff = new Button();
            this.btnConfirm = new System.Windows.Forms.Button();
            this.flowLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.label7 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.regionBox = new System.Windows.Forms.ComboBox();
            this.visitorTypeBox = new System.Windows.Forms.ComboBox();
            this.groupNumBox = new System.Windows.Forms.TextBox();
            this.groupTypeBox = new System.Windows.Forms.ComboBox();
            this.payModeBox = new System.Windows.Forms.ComboBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.oldCount = new System.Windows.Forms.TextBox();
            this.childCount = new System.Windows.Forms.TextBox();
            this.adultCount = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.statusStrip.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.fileToolStripMenuItem,
                this.toolsToolStripMenuItem,
            });
            fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            exitToolStripMenuItem
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            btnStuff.Name = "btnStuff";
            btnStuff.Name = "btnStuff";
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(200, 24);
            btnStuff.Text = "Sell Stuff";
            btnStuff.Click += btnStuff_Click; */
            btnStuff.Text = "Sell Stuff";
            btnStuff.Click += btnStuff_Click; */
            //Controls.Add(btnCashReport);
            //Controls.Add(btnReceiveCash);
            //Controls.Add(btnCash);
            this.menuStrip1.TabIndex = 22;
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.exitToolStripMenuItem,
            });
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Text = "&File";
            // 
            // exitToolStripMenuItem
            // 
            this.exitToolStripMenuItem.Name = "exitToolStripMenuItem";
            this.exitToolStripMenuItem.Text = "E&xit";
            exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;
            this.exitToolStripMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.receivedCashToolStripMenuItem,
                this.cashReportToolStripMenuItem,
                this.openDaysToolStripMenuItem,
            });
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // receivedCashToolStripMenuItem
            // 
            this.receivedCashToolStripMenuItem.Name = "receivedCashToolStripMenuItem";
            this.receivedCashToolStripMenuItem.Text = "Receive Cash...";
            receivedCashToolStripMenuItem.Click += receivedCashToolStripMenuItem_Click;
            this.receivedCashToolStripMenuItem.Click += new System.EventHandler(this.receivedCashToolStripMenuItem_Click);
            // 
            // cashReportToolStripMenuItem
            // 
            this.cashReportToolStripMenuItem.Name = "cashReportToolStripMenuItem";
            this.cashReportToolStripMenuItem.Text = "Cash Report...";
            cashReportToolStripMenuItem.Click += cashReportToolStripMenuItem_Click;
            this.cashReportToolStripMenuItem.Click += new System.EventHandler(this.cashReportToolStripMenuItem_Click);
            // 
            // openDaysToolStripMenuItem
            // 
            this.openDaysToolStripMenuItem.Name = "openDaysToolStripMenuItem";
            this.openDaysToolStripMenuItem.Text = "Open Days...";
            openDaysToolStripMenuItem.Click += openDaysToolStripMenuItem_Click;
            this.openDaysToolStripMenuItem.Click += new System.EventHandler(this.openDaysToolStripMenuItem_Click);
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
                this.connectionStatusLabel,
                this.statusSeparator1,
                this.userStatusLabel,
                this.statusSeparator2,
                this.salesStatusLabel,
                this.statusSeparator3,
                this.appStatusLabel,
            });
            this.statusStrip.Location = new System.Drawing.Point(0, 0);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(200, 22);
            this.statusStrip.SizingGrip = false;
            statusStrip.Dock = DockStyle.Bottom;
            this.statusStrip.TabIndex = 21;
            // 
            // connectionStatusLabel
            // 
            this.connectionStatusLabel.Name = "connectionStatusLabel";
            this.connectionStatusLabel.Text = "ODBC: Checking...";
            // 
            // statusSeparator1
            // 
            this.statusSeparator1.Name = "statusSeparator1";
            this.statusSeparator1.Text = " | ";
            // 
            // userStatusLabel
            // 
            this.userStatusLabel.Name = "userStatusLabel";
            this.userStatusLabel.Text = "User: -";
            // 
            // statusSeparator2
            // 
            this.statusSeparator2.Name = "statusSeparator2";
            this.statusSeparator2.Text = " | ";
            // 
            // salesStatusLabel
            // 
            this.salesStatusLabel.Name = "salesStatusLabel";
            this.salesStatusLabel.Text = "Today cash: 0.00 MOP";
            // 
            // statusSeparator3
            // 
            this.statusSeparator3.Name = "statusSeparator3";
            this.statusSeparator3.Text = " | ";
            // 
            // appStatusLabel
            // 
            this.appStatusLabel.Name = "appStatusLabel";
            this.appStatusLabel.Spring = true;
            this.appStatusLabel.Text = "Ready";
            this.appStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // btnCashReport
            // 
            this.btnCashReport.Location = new System.Drawing.Point(474, 320);
            this.btnCashReport.Name = "btnCashReport";
            this.btnCashReport.Size = new System.Drawing.Size(140, 48);
            this.btnCashReport.TabIndex = 12;
            this.btnCashReport.Text = "Cash Report";
            btnCashReport.Click += cashReportToolStripMenuItem_Click;
            this.btnCashReport.Click += new System.EventHandler(this.cashReportToolStripMenuItem_Click);
            // 
            // btnReceiveCash
            // 
            this.btnReceiveCash.Location = new System.Drawing.Point(324, 320);
            this.btnReceiveCash.Name = "btnReceiveCash";
            this.btnReceiveCash.Size = new System.Drawing.Size(140, 48);
            this.btnReceiveCash.TabIndex = 11;
            this.btnReceiveCash.Text = "Receive Cash";
            btnReceiveCash.Click += receivedCashToolStripMenuItem_Click;
            this.btnReceiveCash.Click += new System.EventHandler(this.receivedCashToolStripMenuItem_Click);
            // 
            // btnCash
            // 
            this.btnCash.Location = new System.Drawing.Point(46, 368);
            this.btnCash.Name = "btnCash";
            this.btnCash.Size = new System.Drawing.Size(140, 48);
            this.btnCash.TabIndex = 10;
            this.btnCash.Text = "Cash";
            btnCash.Click += btnCash_Click;
            this.btnCash.Click += new System.EventHandler(this.btnCash_Click);
            // 
            // btnConfirm
            // 
            this.btnConfirm.Location = new System.Drawing.Point(319, 368);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Size = new System.Drawing.Size(140, 48);
            this.btnConfirm.TabIndex = 9;
            this.btnConfirm.Text = "Pay";
            btnConfirm.Click += btnConfirm_Click;
            this.btnConfirm.Click += new System.EventHandler(this.btnConfirm_Click);
            // 
            // flowLayoutPanel
            // 
            this.flowLayoutPanel.Location = new System.Drawing.Point(24, 245);
            this.flowLayoutPanel.Name = "flowLayoutPanel";
            this.flowLayoutPanel.Size = new System.Drawing.Size(590, 55);
            this.flowLayoutPanel.TabIndex = 8;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(310, 240);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(100, 23);
            this.label7.TabIndex = 15;
            this.label7.Text = "Region";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            label7.Font = boldFont;
            this.label6.Location = new System.Drawing.Point(310, 180);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(100, 23);
            this.label6.TabIndex = 14;
            this.label6.Text = "Visitor Type";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(310, 120);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(100, 23);
            this.label5.TabIndex = 13;
            this.label5.Text = "Group Number";
            // 
            // regionBox
            // 
            this.regionBox.Location = new System.Drawing.Point(475, 235);
            this.regionBox.Name = "regionBox";
            this.regionBox.Size = new System.Drawing.Size(140, 38);
            this.regionBox.TabIndex = 6;
            // 
            // visitorTypeBox
            // 
            this.visitorTypeBox.Location = new System.Drawing.Point(0, 0);
            this.visitorTypeBox.Name = "visitorTypeBox";
            this.visitorTypeBox.Size = new System.Drawing.Size(121, 28);
            this.visitorTypeBox.TabIndex = 11;
            // 
            // groupNumBox
            // 
            this.groupNumBox.Location = new System.Drawing.Point(475, 115);
            this.groupNumBox.Name = "groupNumBox";
            this.groupNumBox.Size = new System.Drawing.Size(140, 38);
            this.groupNumBox.TabIndex = 4;
            this.groupNumBox.Text = "0";
            // 
            // groupTypeBox
            // 
            this.groupTypeBox.Location = new System.Drawing.Point(475, 55);
            this.groupTypeBox.Name = "groupTypeBox";
            this.groupTypeBox.Size = new System.Drawing.Size(140, 38);
            this.groupTypeBox.TabIndex = 3;
            // 
            // payModeBox
            // 
            this.payModeBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            payModeBox.Font = boldFont;
            this.payModeBox.Location = new System.Drawing.Point(475, 175);
            this.payModeBox.Name = "payModeBox";
            this.payModeBox.Size = new System.Drawing.Size(140, 38);
            this.payModeBox.TabIndex = 5;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(310, 180);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(100, 23);
            this.label8.TabIndex = 7;
            this.label8.Text = "Payment Mode";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(310, 60);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(100, 23);
            this.label4.TabIndex = 6;
            this.label4.Text = "Group Type";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(24, 180);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 23);
            this.label3.TabIndex = 5;
            this.label3.Text = "Old";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(24, 120);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 23);
            this.label2.TabIndex = 4;
            this.label2.Text = "Child";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(24, 60);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 23);
            this.label1.TabIndex = 3;
            this.label1.Text = "Adult";
            // 
            // oldCount
            // 
            this.oldCount.Location = new System.Drawing.Point(150, 175);
            this.oldCount.MaxLength = 3;
            this.oldCount.Name = "oldCount";
            this.oldCount.Size = new System.Drawing.Size(140, 43);
            this.oldCount.TabIndex = 2;
            this.oldCount.Text = "0";
            // 
            // childCount
            // 
            this.childCount.Location = new System.Drawing.Point(150, 115);
            this.childCount.MaxLength = 3;
            this.childCount.Name = "childCount";
            this.childCount.Size = new System.Drawing.Size(140, 43);
            this.childCount.TabIndex = 1;
            this.childCount.Text = "0";
            // 
            // adultCount
            // 
            this.adultCount.Location = new System.Drawing.Point(150, 55);
            this.adultCount.MaxLength = 3;
            this.adultCount.Name = "adultCount";
            this.adultCount.Size = new System.Drawing.Size(140, 43);
            this.adultCount.TabIndex = 0;
            this.adultCount.Text = "0";
            // 
            // SellTicketForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            Font = boldFont;
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.statusStrip);
            /*         btnStuff.Font = boldFont;
            this.Controls.Add(this.btnCashReport);
            this.Controls.Add(this.btnReceiveCash);
            this.Controls.Add(this.btnCash);
            this.Controls.Add(this.btnConfirm);
            this.Controls.Add(this.flowLayoutPanel);
            //Controls.Add(label8);
            //Controls.Add(label7);
            //Controls.Add(label6);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            /*      Controls.Add(payModeBox);
            this.Controls.Add(this.regionBox);
            this.Controls.Add(this.visitorTypeBox);
            */
            this.Controls.Add(this.groupNumBox);
            this.Controls.Add(this.groupTypeBox);
            //Controls.Add(payModeBox);
            this.Controls.Add(this.payModeBox);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.oldCount);
            this.Controls.Add(this.childCount);
            this.Controls.Add(this.adultCount);
            FormClosing += SellTicketForm_FormClosing;
            MainMenuStrip = menuStrip1;
            Text = "Macau Museum Ticket System - Sell Ticket";
            StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "SellTicketForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Macau Museum Ticket System - Sell Ticket";
            this.statusStrip.ResumeLayout(false);
            this.menuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        
}

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem exitToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem receivedCashToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem cashReportToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openDaysToolStripMenuItem;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel connectionStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel statusSeparator1;
        private System.Windows.Forms.ToolStripStatusLabel userStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel statusSeparator2;
        private System.Windows.Forms.ToolStripStatusLabel salesStatusLabel;
        private System.Windows.Forms.ToolStripStatusLabel statusSeparator3;
        private System.Windows.Forms.ToolStripStatusLabel appStatusLabel;
        private System.Windows.Forms.Button btnCashReport;
        private System.Windows.Forms.Button btnReceiveCash;
        private System.Windows.Forms.Button btnCash;
        private System.Windows.Forms.Button btnConfirm;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox regionBox;
        private System.Windows.Forms.ComboBox visitorTypeBox;
        private System.Windows.Forms.TextBox groupNumBox;
        private System.Windows.Forms.ComboBox groupTypeBox;
        private System.Windows.Forms.ComboBox payModeBox;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox oldCount;
        private System.Windows.Forms.TextBox childCount;
        private System.Windows.Forms.TextBox adultCount;
    


}

