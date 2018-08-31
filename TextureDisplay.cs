using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

namespace mwtc
{
	/// <summary>
	/// Summary description for TextureDisplay.
	/// </summary>
	public class TextureDisplay : System.Windows.Forms.Form
	{
		private System.Windows.Forms.PictureBox picBox;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public TextureDisplay()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.picBox = new System.Windows.Forms.PictureBox();
			this.SuspendLayout();
			// 
			// picBox
			// 
			this.picBox.Location = new System.Drawing.Point(0, 0);
			this.picBox.Name = "picBox";
			this.picBox.Size = new System.Drawing.Size(120, 104);
			this.picBox.TabIndex = 0;
			this.picBox.TabStop = false;
			// 
			// TextureDisplay
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.AutoScroll = true;
			this.ClientSize = new System.Drawing.Size(480, 453);
			this.Controls.Add(this.picBox);
			this.Name = "TextureDisplay";
			this.Text = "TextureDisplay";
			this.ResumeLayout(false);

		}
		#endregion

		public void SetImage(Image image)
		{
			picBox.Image = image;
			picBox.Width = image.Width;
			picBox.Height = image.Height;
		}
	}
}
