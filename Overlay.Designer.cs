namespace ApplicationMuter;

partial class Overlay
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
        this.label1 = new Label();
        this.pictureBox1 = new PictureBox();
        ((System.ComponentModel.ISupportInitialize)this.pictureBox1).BeginInit();
        this.SuspendLayout();
        // 
        // label1
        // 
        this.label1.BackColor = Color.IndianRed;
        this.label1.Dock = DockStyle.Right;
        this.label1.Font = new Font("Segoe UI Emoji", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
        this.label1.Location = new Point(47, 0);
        this.label1.Name = "label1";
        this.label1.Size = new Size(198, 44);
        this.label1.TabIndex = 0;
        this.label1.Text = "notification 🔇🔇";
        this.label1.TextAlign = ContentAlignment.MiddleCenter;
        // 
        // pictureBox1
        // 
        this.pictureBox1.BackColor = Color.Brown;
        this.pictureBox1.Dock = DockStyle.Left;
        this.pictureBox1.Location = new Point(0, 0);
        this.pictureBox1.Name = "pictureBox1";
        this.pictureBox1.Size = new Size(44, 44);
        this.pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
        this.pictureBox1.TabIndex = 1;
        this.pictureBox1.TabStop = false;
        // 
        // Overlay
        // 
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.AutoSize = true;
        this.BackColor = Color.Black;
        this.ClientSize = new Size(245, 44);
        this.ControlBox = false;
        this.Controls.Add(this.label1);
        this.Controls.Add(this.pictureBox1);
        this.ForeColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.None;
        this.Name = "Overlay";
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Overlay";
        this.TopMost = true;
        this.Load += this.Overlay_Load;
        ((System.ComponentModel.ISupportInitialize)this.pictureBox1).EndInit();
        this.ResumeLayout(false);
    }

    #endregion

    private Label label1;
    private PictureBox pictureBox1;
}