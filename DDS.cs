using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace mwtc
{
	/// <summary>
	/// Summary description for DDS.
	/// </summary>
	public class DDS
	{
		public enum DDSFormatFlags
		{
			DXT = 4,
			Palette = 32,
			RGB = 64,
		}

		public enum DDSFormatFourCC
		{
			DXT1 = 0x31545844,
			DXT3 = 0x33545844,
			A8R8G8B8 = 21,
			P8 = 41,
		}

		public int Height;
		public int Width;
		public int Pitch;
		public int VolDepth;
		public int MipLevels;
		public int FormatSize;
		public DDSFormatFlags FormatFlags;
		public DDSFormatFourCC FormatFourCC;
		public int BitsPerPixel;
		public int MaskRed;
		public int MaskGreen;
		public int MaskBlue;
		public int MaskAlpha;
		public int DWCaps1;
		public int DWCaps2;
		public int DWCapsReserved1;
		public int DWCapsReserved2;

		public byte[] Palette;
		public byte[][] Texture;


		public class DDSException : Exception
		{
			public DDSException(string reason) : base(reason) {}
		}

		public DDS()
		{
		}
		
		public void Open(string filename)
		{
			FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
			try
			{
				Open(fs);
			} 
			catch (Exception ex)
			{
				fs.Close();
				throw ex;
			}
		}
		
		public void Open(Stream stream)
		{
			BinaryReader br = new BinaryReader(stream);
			if (br.ReadInt32() != 542327876) // DDS
			{
				throw new DDSException("Not a valid DDS file.");
			}
			stream.Seek(8, SeekOrigin.Current);
			Height = br.ReadInt32();
			Width = br.ReadInt32();
			Pitch = br.ReadInt32();
			VolDepth = br.ReadInt32();
			MipLevels = br.ReadInt32();
			
			// Reserved
			stream.Seek(11*4, SeekOrigin.Current);

			// Pixel Format
			FormatSize = br.ReadInt32();
			FormatFlags = (DDSFormatFlags)br.ReadInt32();
			FormatFourCC = (DDSFormatFourCC)br.ReadInt32();
			BitsPerPixel = br.ReadInt32();
			MaskRed = br.ReadInt32();
			MaskGreen = br.ReadInt32();
			MaskBlue = br.ReadInt32();
			MaskAlpha = br.ReadInt32();

			// DWCaps
			DWCaps1 = br.ReadInt32();
			DWCaps2 = br.ReadInt32();
			DWCapsReserved1 = br.ReadInt32();
			DWCapsReserved2 = br.ReadInt32();

			// Reserved
			br.ReadInt32();

			// Read palette if present
			if ((FormatFlags & DDSFormatFlags.Palette) != 0)
			{
				Palette = br.ReadBytes((1<<BitsPerPixel)*4);
			}

			// Read the texture data (only the first mipmap)
			if (MipLevels == 0)
				MipLevels = 1;
			Texture = new byte[MipLevels][];
			int pitch = Pitch;
			for(int i=0; i<MipLevels; i++)
			{
				Texture[i] = br.ReadBytes(pitch);
				pitch /= 2;
			}

		}

		public Image Decode()
		{
			if ((this.FormatFlags & DDSFormatFlags.DXT) != 0)
			{
				byte[] data = Texture[0];
				if (this.FormatFourCC == DDSFormatFourCC.DXT1)
				{
					data = DecodeDXT1(data, Width, Height);
				}
				else if (this.FormatFourCC == DDSFormatFourCC.DXT3)
				{
					data = DecodeDXT3(data, Width, Height);
				}
				else
				{
					return null;
				}
				Bitmap image = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
				Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
				BitmapData bmd = image.LockBits(rect, ImageLockMode.ReadWrite, image.PixelFormat);
				Marshal.Copy(data, 0, bmd.Scan0, Width*Height*4);
				image.UnlockBits(bmd);
				return image;
			} 
			else
			{
				return null;	
			}
		} 

		private byte[] DecodeDXT3(byte[] data, int width, int height)
		{
			byte[] pixData = new byte[width * height * 4];
			int xBlocks = width / 4;
			int yBlocks = height / 4;
			for (int y=0; y<yBlocks; y++)
			{
				for (int x=0; x<xBlocks; x++)
				{
					int blockDataStart = ((y*xBlocks)+x)*16;
					ushort[] alphaData = new ushort[4];

					alphaData[0] = BitConverter.ToUInt16(data, blockDataStart+0);
					alphaData[1] = BitConverter.ToUInt16(data, blockDataStart+2);
					alphaData[2] = BitConverter.ToUInt16(data, blockDataStart+4);
					alphaData[3] = BitConverter.ToUInt16(data, blockDataStart+6);

					byte[,] alpha = new byte[4,4];
					for(int j=0; j<4; j++)
					{
						for (int i=0; i<4; i++)
						{
							alpha[i,j] = (byte)((alphaData[j] & 0xF) * 16);
							alphaData[j] >>= 4;
						}
					}

					ushort color0 = BitConverter.ToUInt16(data, blockDataStart+8);
					ushort color1 = BitConverter.ToUInt16(data, blockDataStart+8+2);
					uint code = BitConverter.ToUInt32(data, blockDataStart+8+4);

					ushort r0=0, g0=0, b0=0, r1=0, g1=0, b1=0;
					r0=(ushort)(8*(color0&31));
					g0=(ushort)(4*((color0>>5)&63));
					b0=(ushort)(8*((color0>>11)&31));
					
					r1=(ushort)(8*(color1&31));
					g1=(ushort)(4*((color1>>5)&63));
					b1=(ushort)(8*((color1>>11)&31));

					for(int j=0; j<4; j++)
					{
						for (int i=0; i<4; i++)
						{
							int pixDataStart = (width*(y*4+j)*4)+((x*4+i)*4);
							uint codeDec = code & 0x3;

							pixData[pixDataStart+3] = alpha[i,j];

							switch(codeDec)
							{
								case 0:
									pixData[pixDataStart+0] = (byte)r0;
									pixData[pixDataStart+1] = (byte)g0;
									pixData[pixDataStart+2] = (byte)b0;
									break;
								case 1:
									pixData[pixDataStart+0] = (byte)r1;
									pixData[pixDataStart+1] = (byte)g1;
									pixData[pixDataStart+2] = (byte)b1;
									break;
								case 2:
									if (color0>color1)
									{
										pixData[pixDataStart+0] = (byte)((2*r0+r1)/3);
										pixData[pixDataStart+1] = (byte)((2*g0+g1)/3);
										pixData[pixDataStart+2] = (byte)((2*b0+b1)/3);
									} 
									else
									{
										pixData[pixDataStart+0] = (byte)((r0+r1)/2);
										pixData[pixDataStart+1] = (byte)((g0+g1)/2);
										pixData[pixDataStart+2] = (byte)((b0+b1)/2);
									}
									break;
								case 3:
									if (color0>color1)
									{
										pixData[pixDataStart+0] = (byte)((r0+2*r1)/3);
										pixData[pixDataStart+1] = (byte)((g0+2*g1)/3);
										pixData[pixDataStart+2] = (byte)((b0+2*b1)/3);
									} 
									else
									{
										pixData[pixDataStart+0] = 0;
										pixData[pixDataStart+1] = 0;
										pixData[pixDataStart+2] = 0;
									}
									break;
							}

							code >>= 2;
						}
					}
						

				}
			}
			return pixData;
		}


		private byte[] DecodeDXT1(byte[] data, int width, int height)
		{
			byte[] pixData = new byte[width * height * 4];
			int xBlocks = width / 4;
			int yBlocks = height / 4;
			for (int y=0; y<yBlocks; y++)
			{
				for (int x=0; x<xBlocks; x++)
				{
					int blockDataStart = ((y*xBlocks)+x)*8;
					uint color0 = BitConverter.ToUInt16(data, blockDataStart);
					uint color1 = BitConverter.ToUInt16(data, blockDataStart+2);
					uint code = BitConverter.ToUInt32(data, blockDataStart+4);

					ushort r0=0, g0=0, b0=0, r1=0, g1=0, b1=0;
					r0=(ushort)(8*(color0&31));
					g0=(ushort)(4*((color0>>5)&63));
					b0=(ushort)(8*((color0>>11)&31));
					
					r1=(ushort)(8*(color1&31));
					g1=(ushort)(4*((color1>>5)&63));
					b1=(ushort)(8*((color1>>11)&31));

					for(int j=0; j<4; j++)
					{
						for (int i=0; i<4; i++)
						{
							int pixDataStart = (width*(y*4+j)*4)+((x*4+i)*4);
							uint codeDec = code & 0x3;

							switch(codeDec)
							{
								case 0:
									pixData[pixDataStart+0] = (byte)r0;
									pixData[pixDataStart+1] = (byte)g0;
									pixData[pixDataStart+2] = (byte)b0;
									pixData[pixDataStart+3] = 255;
									break;
								case 1:
									pixData[pixDataStart+0] = (byte)r1;
									pixData[pixDataStart+1] = (byte)g1;
									pixData[pixDataStart+2] = (byte)b1;
									pixData[pixDataStart+3] = 255;
									break;
								case 2:
									pixData[pixDataStart+3] = 255;
									if (color0>color1)
									{
										pixData[pixDataStart+0] = (byte)((2*r0+r1)/3);
										pixData[pixDataStart+1] = (byte)((2*g0+g1)/3);
										pixData[pixDataStart+2] = (byte)((2*b0+b1)/3);
									} 
									else
									{
										pixData[pixDataStart+0] = (byte)((r0+r1)/2);
										pixData[pixDataStart+1] = (byte)((g0+g1)/2);
										pixData[pixDataStart+2] = (byte)((b0+b1)/2);
									}
									break;
								case 3:
									if (color0>color1)
									{
										pixData[pixDataStart+0] = (byte)((r0+2*r1)/3);
										pixData[pixDataStart+1] = (byte)((g0+2*g1)/3);
										pixData[pixDataStart+2] = (byte)((b0+2*b1)/3);
										pixData[pixDataStart+3] = 255;
									} 
									else
									{
										pixData[pixDataStart+0] = 0;
										pixData[pixDataStart+1] = 0;
										pixData[pixDataStart+2] = 0;
										pixData[pixDataStart+3] = 0;
									}
									break;
							}

							code >>= 2;
						}
					}
						

				}
			}
			return pixData;
		}


	}
}
