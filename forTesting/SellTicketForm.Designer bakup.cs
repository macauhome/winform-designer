namespace TicketCSharp.Forms;

partial class SellTicketForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        adultCount = new TextBox();
        childCount = new TextBox();
        oldCount = new TextBox();
        label1 = new Label();
        label2 = new Label();
        label3 = new Label();
        label4 = new Label();
        label5 = new Label();
        label6 = new Label();
        label7 = new Label();
        label8 = new Label();
        groupTypeBox = new ComboBox();
        groupNumBox = new TextBox();
        visitorTypeBox = new ComboBox();
        regionBox = new ComboBox();
        payModeBox = new ComboBox();
        btnConfirm = new Button();
        btnCash = new Button();
        btnStuff = new Button();
        flowLayoutPanel = new FlowLayoutPanel();
        menuStrip1 = new System.Windows.Forms.MenuStrip();
        fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        exitToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        receivedCashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        cashReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        openDaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        statusStrip = new StatusStrip();
        connectionStatusLabel = new ToolStripStatusLabel();
        statusSeparator1 = new ToolStripStatusLabel();
        userStatusLabel = new ToolStripStatusLabel();
        statusSeparator2 = new ToolStripStatusLabel();
        salesStatusLabel = new ToolStripStatusLabel();
        statusSeparator3 = new ToolStripStatusLabel();
        appStatusLabel = new ToolStripStatusLabel();
        statusRefreshTimer = new System.Windows.Forms.Timer(components);
        var boldFont = new Font("Segoe UI", 16F, FontStyle.Bold, GraphicsUnit.Point);
        SuspendLayout();

        adultCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
        adultCount.Location = new Point(150, 55);
        adultCount.MaxLength = 3;
        adultCount.Name = "adultCount";
        adultCount.Size = new Size(140, 43);
        adultCount.TabIndex = 0;
        adultCount.Text = "0";

        childCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
        childCount.Location = new Point(150, 115);
        childCount.MaxLength = 3;
        childCount.Name = "childCount";
        childCount.Size = new Size(140, 43);
        childCount.TabIndex = 1;
        childCount.Text = "0";

        oldCount.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
        oldCount.Location = new Point(150, 175);
        oldCount.MaxLength = 3;
        oldCount.Name = "oldCount";
        oldCount.Size = new Size(140, 43);
        oldCount.TabIndex = 2;
        oldCount.Text = "0";

        label1.Font = boldFont;
        label1.Location = new Point(24, 60);
        label1.Name = "label1";
        label1.Text = "Adult";
        label1.AutoSize = true;

        label2.Font = boldFont;
        label2.Location = new Point(24, 120);
        label2.Name = "label2";
        label2.Text = "Child";
        label2.AutoSize = true;

        label3.Font = boldFont;
        label3.Location = new Point(24, 180);
        label3.Name = "label3";
        label3.Text = "Old";
        label3.AutoSize = true;

        label4.Font = boldFont;
        label4.Location = new Point(310, 60);
        label4.Name = "label4";
        label4.Text = "Group Type";
        label4.AutoSize = true;

        groupTypeBox.Font = boldFont;
        groupTypeBox.Location = new Point(475, 55);
        groupTypeBox.Name = "groupTypeBox";
        groupTypeBox.Size = new Size(140, 38);
        groupTypeBox.TabIndex = 3;

        label5.Font = boldFont;
        label5.Location = new Point(310, 120);
        label5.Name = "label5";
        label5.Text = "Group Number";
        label5.AutoSize = true;

        receivedCashToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
        cashReportToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();

        groupNumBox.Font = boldFont;
        groupNumBox.Location = new Point(475, 115);
        groupNumBox.Name = "groupNumBox";
        groupNumBox.Size = new Size(140, 38);
        groupNumBox.TabIndex = 4;
        groupNumBox.Text = "0";

        /*      label6.Font = boldFont;
            label6.Location = new Point(310, 180);
             label6.Name = "label6";
             label6.Text = "Visitor Type";
             label6.AutoSize = true;
             label7.Font = boldFont;
            label7.Location = new Point(310, 240);
             label7.Name = "label7";
             label7.Text = "Region";
             label7.AutoSize = true;

             regionBox.Font = boldFont;
            regionBox.Location = new Point(475, 235);
             regionBox.Name = "regionBox";
            regionBox.Size = new Size(140, 38);
             regionBox.TabIndex = 6;

             */

        // label8.Font = boldFont;
        // label8.Location = new Point(310, 180);
        // label8.Name = "label8";
        // label8.Text = "Payment Mode";
        // label8.AutoSize = true;

        payModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
        payModeBox.Font = boldFont;
        payModeBox.Location = new Point(475, 175);
        payModeBox.Name = "payModeBox";
        payModeBox.Size = new Size(140, 38);
        payModeBox.TabIndex = 5;

        flowLayoutPanel.Font = boldFont;
        flowLayoutPanel.Location = new Point(24, 245);
        flowLayoutPanel.Name = "flowLayoutPanel";
        flowLayoutPanel.Size = new Size(590, 55);
        flowLayoutPanel.TabIndex = 8;

        btnConfirm.Font = boldFont;
        btnConfirm.Location = new Point(200, 320);
        btnConfirm.Name = "btnConfirm";
        btnConfirm.Size = new Size(140, 48);
        btnConfirm.TabIndex = 9;
        btnConfirm.Text = "Pay";
        btnConfirm.Click += btnConfirm_Click;

        btnCash.Font = boldFont;
        btnCash.Location = new Point(174, 320);
        btnCash.Name = "btnCash";
        btnCash.Size = new Size(140, 48);
        btnCash.TabIndex = 10;
        btnCash.Text = "Cash";
        btnCash.Click += btnCash_Click;

        btnReceiveCash = new Button();
        btnReceiveCash.Font = boldFont;
        btnReceiveCash.Location = new Point(324, 320);
        btnReceiveCash.Name = "btnReceiveCash";
        btnReceiveCash.Size = new Size(140, 48);
        btnReceiveCash.TabIndex = 11;
        btnReceiveCash.Text = "Receive Cash";
        btnReceiveCash.Click += receivedCashToolStripMenuItem_Click;

        btnCashReport = new Button();
        btnCashReport.Font = boldFont;
        btnCashReport.Location = new Point(474, 320);
        btnCashReport.Name = "btnCashReport";
        btnCashReport.Size = new Size(140, 48);
        btnCashReport.TabIndex = 12;
        btnCashReport.Text = "Cash Report";
        btnCashReport.Click += cashReportToolStripMenuItem_Click;

        /*         btnStuff.Font = boldFont;
                btnStuff.Location = new Point(30, 300);
                btnStuff.Name = "btnStuff";
                btnStuff.Size = new Size(170, 52);
                btnStuff.Text = "Sell Stuff";
                btnStuff.Click += btnStuff_Click; */

        menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            fileToolStripMenuItem,
            toolsToolStripMenuItem
        });
        fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            exitToolStripMenuItem
        });
        fileToolStripMenuItem.Name = "fileToolStripMenuItem";
        fileToolStripMenuItem.Size = new Size(37, 20);
        fileToolStripMenuItem.Text = "&File";

        exitToolStripMenuItem.Name = "exitToolStripMenuItem";
        exitToolStripMenuItem.Size = new Size(180, 22);
        exitToolStripMenuItem.Text = "E&xit";
        exitToolStripMenuItem.Click += exitToolStripMenuItem_Click;

        toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            receivedCashToolStripMenuItem,
            cashReportToolStripMenuItem,
            openDaysToolStripMenuItem
        });
        toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
        toolsToolStripMenuItem.Size = new Size(46, 20);
        toolsToolStripMenuItem.Text = "Tools";

        receivedCashToolStripMenuItem.Name = "receivedCashToolStripMenuItem";
        receivedCashToolStripMenuItem.Size = new Size(180, 22);
        receivedCashToolStripMenuItem.Text = "Receive Cash...";
        receivedCashToolStripMenuItem.Click += receivedCashToolStripMenuItem_Click;

        cashReportToolStripMenuItem.Name = "cashReportToolStripMenuItem";
        cashReportToolStripMenuItem.Size = new Size(180, 22);
        cashReportToolStripMenuItem.Text = "Cash Report...";
        cashReportToolStripMenuItem.Click += cashReportToolStripMenuItem_Click;

        openDaysToolStripMenuItem.Name = "openDaysToolStripMenuItem";
        openDaysToolStripMenuItem.Size = new Size(180, 22);
        openDaysToolStripMenuItem.Text = "Open Days...";
        openDaysToolStripMenuItem.Click += openDaysToolStripMenuItem_Click;

        statusStrip.Items.AddRange(new ToolStripItem[] {
            connectionStatusLabel,
            statusSeparator1,
            userStatusLabel,
            statusSeparator2,
            salesStatusLabel,
            statusSeparator3,
            appStatusLabel
        });
        statusStrip.Name = "statusStrip";
        statusStrip.SizingGrip = false;
        statusStrip.Dock = DockStyle.Bottom;
        connectionStatusLabel.Name = "connectionStatusLabel";
        connectionStatusLabel.Text = "ODBC: Checking...";
        statusSeparator1.Name = "statusSeparator1";
        statusSeparator1.Text = " | ";
        userStatusLabel.Name = "userStatusLabel";
        userStatusLabel.Text = "User: -";
        statusSeparator2.Name = "statusSeparator2";
        statusSeparator2.Text = " | ";
        salesStatusLabel.Name = "salesStatusLabel";
        salesStatusLabel.Text = "Today cash: 0.00 MOP";
        statusSeparator3.Name = "statusSeparator3";
        statusSeparator3.Text = " | ";
        appStatusLabel.Name = "appStatusLabel";
        appStatusLabel.Spring = true;
        appStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        appStatusLabel.Text = "Ready";

        FormBorderStyle = FormBorderStyle.FixedDialog;
        AutoScaleDimensions = new SizeF(9F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(640, 480);
        Font = boldFont;
        Controls.Add(menuStrip1);
        Controls.Add(statusStrip);
        /*         btnStuff.Font = boldFont;
        btnStuff.Location = new Point(30, 300);
        btnStuff.Name = "btnStuff";
        btnStuff.Size = new Size(170, 52);
        btnStuff.Text = "Sell Stuff";
        btnStuff.Click += btnStuff_Click; */
        //Controls.Add(btnCashReport);
        //Controls.Add(btnReceiveCash);
        //Controls.Add(btnCash);
        Controls.Add(btnConfirm);
        Controls.Add(flowLayoutPanel);
        //Controls.Add(label8);
        //Controls.Add(label7);
        //Controls.Add(label6);
        Controls.Add(label5);
        /*      Controls.Add(payModeBox);
                Controls.Add(regionBox);
                Controls.Add(visitorTypeBox); 
        */
        Controls.Add(groupNumBox);
        Controls.Add(groupTypeBox);
        //Controls.Add(payModeBox);
        Controls.Add(label8);
        Controls.Add(label4);
        Controls.Add(label3);
        Controls.Add(label2);
        Controls.Add(label1);
        Controls.Add(oldCount);
        Controls.Add(childCount);
        Controls.Add(adultCount);
        FormClosing += SellTicketForm_FormClosing;
        MainMenuStrip = menuStrip1;
        Text = "Macau Museum Ticket System - Sell Ticket";
        StartPosition = FormStartPosition.CenterScreen;
        ResumeLayout(false);
        PerformLayout();

        return;
    }

    private TextBox adultCount;
    private TextBox childCount;
    private TextBox oldCount;
    private Label label1;
    private Label label2;
    private Label label3;
    private Label label4;
    private Label label5;
    private Label label6;
    private Label label7;
    private Label label8;
    private ComboBox groupTypeBox;
    private TextBox groupNumBox;
    private ComboBox visitorTypeBox;
    private ComboBox regionBox;
    private ComboBox payModeBox;
    private Button btnConfirm;
    private Button btnCash;
    private Button btnReceiveCash;
    private Button btnCashReport;
    private Button btnStuff;
    private FlowLayoutPanel flowLayoutPanel;
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
    private System.Windows.Forms.Timer statusRefreshTimer;
}
