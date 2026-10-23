namespace Sample.Forms
{
    partial class MainForm
    {
        private System.Windows.Forms.TableLayoutPanel panel1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label1;

        private void InitializeComponent()
        {
            this.panel1 = new System.Windows.Forms.TableLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            this.button1.Location = new System.Drawing.Point(10, 11);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.Text = "Click me";
            this.button1.Click += new System.EventHandler(this.button1_Click);
            this.label1.Name = "label1";
            this.label1.Text = "Status";
            this.panel1.Controls.Add(this.button1, 0, 0);
            this.panel1.Controls.AddRange(new System.Windows.Forms.Control[] { this.label1 });
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ClientSize = new System.Drawing.Size(320, 200);
            this.Name = "MainForm";
            this.Text = "Sample";
            this.panel1.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
