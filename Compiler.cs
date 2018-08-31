using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using mwtc.Tpk;

namespace mwtc
{

	public class TpkTextureInfo
	{
		private int _null1;
		private int _null2;
		private int _null3;
		public string TextureName;  // 0x18
		public uint Hash;
		public int Usage;
		private int _null4;
		public uint MemoryOffset;
		public uint MemoryPaletteOffset;
		public uint TextureLength;
		public uint PaletteLength;
		public int PitchOrLinearSize;
		public ushort Width;
		public ushort Height;
		public int D1;
		public int D2;
		public int D3;
		public int D4;
		public int D5;
		public int D6;
		public int D7;
		public int D8;
		private byte[] _padding; // 0x14
		private int _null5;
		private int _null6;
		public int Alpha;
		public int D9;
		public int D10;
		public int D3DFormat;
		private int _null7;
		private int _null8;

		protected static string ReadString(BinaryReader br, int length)
		{
			byte[] strBytes = br.ReadBytes(length);
			
			int i=0;
			for(i=0; i<length; i++) 
				if (strBytes[i]==0) break;

			string strString = Encoding.ASCII.GetString(strBytes, 0, i);

			return strString;
		}

		protected static void WriteString(BinaryWriter bw, string str, int length)
		{
			string strWrite = str.PadRight(length, (char)0);
			byte[] strBytes = Encoding.ASCII.GetBytes(strWrite);
			bw.Write(strBytes);
		}

		public void Read(BinaryReader br)
		{
			_null1 = br.ReadInt32();
			_null2 = br.ReadInt32();
			_null3 = br.ReadInt32();
			TextureName = ReadString(br, 0x18);
			Hash = br.ReadUInt32();
			Usage = br.ReadInt32();
			_null4 = br.ReadInt32();
			MemoryOffset = br.ReadUInt32();
			MemoryPaletteOffset = br.ReadUInt32();
			TextureLength = br.ReadUInt32();
			PaletteLength = br.ReadUInt32();
			PitchOrLinearSize = br.ReadInt32();
			Width = br.ReadUInt16();
			Height = br.ReadUInt16();
			D1 = br.ReadInt32();
			D2 = br.ReadInt32();
			D3 = br.ReadInt32();
			D4 = br.ReadInt32();
			D5 = br.ReadInt32();
			D6 = br.ReadInt32();
			D7 = br.ReadInt32();
			D8 = br.ReadInt32();
			_padding = br.ReadBytes(0x14);
			_null5 = br.ReadInt32();
			_null6 = br.ReadInt32();
			Alpha = br.ReadInt32();
			D9 = br.ReadInt32();
			D10 = br.ReadInt32();
			D3DFormat = br.ReadInt32();
			_null7 = br.ReadInt32();
			_null8 = br.ReadInt32();
		}

		public void Write(BinaryWriter bw)
		{
			if (_padding == null)
				_padding = new byte[0x14];

			bw.Write(_null1);
			bw.Write(_null2);
			bw.Write(_null3);
			WriteString(bw, TextureName, 0x18);
			bw.Write(Hash);
			bw.Write(Usage);
			bw.Write(_null4);
			bw.Write(MemoryOffset);
			bw.Write(MemoryPaletteOffset);
			bw.Write(TextureLength);
			bw.Write(PaletteLength);
			bw.Write(PitchOrLinearSize);
			bw.Write(Width);
			bw.Write(Height);
			bw.Write(D1);
			bw.Write(D2);
			bw.Write(D3);
			bw.Write(D4);
			bw.Write(D5);
			bw.Write(D6);
			bw.Write(D7);
			bw.Write(D8);
			bw.Write(_padding);
			bw.Write(_null5);
			bw.Write(_null6);
			bw.Write(Alpha);
			bw.Write(D9);
			bw.Write(D10);
			bw.Write(D3DFormat);
			bw.Write(_null7);
			bw.Write(_null8);
		}
	}


	class Compiler
	{

		class TextureInfo : IComparable
		{
			public uint Hash;
			public uint Offset;
			public uint LengthCompressed;
			public uint Length;
			public byte[] Data;

			#region IComparable Members

			public int CompareTo(object obj)
			{
				TextureInfo ti = obj as TextureInfo;
				return Hash.CompareTo(ti.Hash);
			}

			#endregion
		}

		static uint Hash(string str)
		{
			uint hash = 0xFFFFFFFF;
			for(int i=0; i<str.Length; i++)
			{
				hash *= 33;
				hash += str[i];
			}
			return hash;
		}

		static void PrintBanner()
		{
			Assembly asm = Assembly.GetExecutingAssembly();
			AssemblyName asmName = asm.GetName();
			
			Console.WriteLine("NFS:MW Texture Compiler (mwtc) " + asmName.Version.ToString());
			Console.WriteLine("Copyright(C) 2005 - 2006, AruTec Inc. (Arushan), All Rights Reserved.");
			Console.WriteLine("Contact: oneforaru at gmail dot com (bug reports only)");
			Console.WriteLine();
			Console.WriteLine("Disclaimer: This program is provided as is without any warranties of any kind.");
			//Console.WriteLine("            All reverse-engineering performed to develop this software was done");
			//Console.WriteLine("            for the sole purpose of accomplishing interoperability.");
			Console.WriteLine();
		}

		static void PrintUsage()
		{
			Console.WriteLine("Usage:   mwtc <texture-definition.txt>");
			Console.WriteLine();
		}

		[STAThread]
		static int Main(string[] args)
		{
			PrintBanner();

			if (args.Length < 1)
			{
				PrintUsage();
				return 1;
			}

			string configFile = args[0];
			ConfigFile config = new ConfigFile();
			config.LoadFile(configFile, new string[]{"texture"});

            FileInfo fileInfo = new FileInfo(configFile);
			Directory.SetCurrentDirectory(fileInfo.DirectoryName);

			string xName = config.GetValue("tpk", "xname");

			ArrayList textures = new ArrayList();
			int textureCount = config.GetCount("texture");
			uint memoryOffset = 0;
			for(int i=0; i<textureCount; i++)
			{
				DDS dds = new DDS();
				string ddsfile = config.GetValue("texture", i, "file");
				dds.Open(ddsfile);

				if ((dds.FormatFlags & DDS.DDSFormatFlags.DXT) == 0 ||
					!(dds.FormatFourCC == DDS.DDSFormatFourCC.DXT1 ||
					dds.FormatFourCC == DDS.DDSFormatFourCC.DXT3)) 
				{
					throw new Exception("Currently only DXT1 and DXT3 textures are supported.");
				}

				bool hasAlpha = false;
				if (dds.FormatFourCC == DDS.DDSFormatFourCC.DXT3)
					hasAlpha = true;
				
				TpkTextureInfo texInfo = new TpkTextureInfo();
				if (xName == null)
					texInfo.TextureName = config.GetValue("texture", i, "name");
				else
					texInfo.TextureName = xName + "_" + config.GetValue("texture", i, "name");
				texInfo.Hash = Hash(texInfo.TextureName);
				
				string usage = config.GetValue("texture", i, "usage");
				if (usage != null) 
					usage = usage.ToLower();
				if (usage == "type1")
					texInfo.Usage = 0x1B81E7B0;
				else if (usage == "type2")
					texInfo.Usage = 0x1DA6C8A6;
				else
					texInfo.Usage = 0x1A93CF;

				bool flagA = config.GetValue("texture", i, "flaga") == "1";

				texInfo.MemoryOffset = memoryOffset;
				texInfo.TextureLength = (uint)dds.Pitch;
				texInfo.MemoryPaletteOffset = texInfo.MemoryOffset + texInfo.TextureLength;
				texInfo.PaletteLength = 0;
				texInfo.PitchOrLinearSize = dds.Pitch;
				texInfo.Width = (ushort)dds.Width;
				texInfo.Height = (ushort)dds.Height;
				texInfo.D1 = 0x220000;
				texInfo.D1 += ((int)Math.Log((double)texInfo.Height, 2)) << 8;
				texInfo.D1 += ((int)Math.Log((double)texInfo.Width, 2));
				texInfo.D2 = 0x10000;
				texInfo.D3 = hasAlpha ? 0x500 : 0x0;
				texInfo.D4 = hasAlpha ? (0x10201 - (flagA ? 0x1 : 0x0)) : 
					(0x1000000 + (flagA ? 0x2000000 : 0x0));
				texInfo.D5 = 0x100;
				texInfo.D6 = 0x0;
				texInfo.D7 = 0x1000000;
				texInfo.D8 = 0x100;
				texInfo.Alpha = hasAlpha ? 0x1 : 0x0;
				texInfo.D9 = 5;
				texInfo.D10 = 6;
				texInfo.D3DFormat = (int)dds.FormatFourCC;
				
				MemoryStream msTex = new MemoryStream(dds.Pitch + 0x9c + 0x10);
				BinaryWriter bwTex = new BinaryWriter(msTex);
				bwTex.Write((int)0x57574152);
				bwTex.Write((int)0x1001);
				bwTex.Write((int)(dds.Pitch + 0x9c));
				bwTex.Write((int)(dds.Pitch + 0x9c + 0x10));
				bwTex.Write(dds.Texture[0]);
				texInfo.Write(bwTex);
				byte[] texData = msTex.GetBuffer();
				msTex.Close();

				TextureInfo textureInfo = new TextureInfo();
				textureInfo.Hash = texInfo.Hash;
				textureInfo.Length = (uint)texData.Length - 0x10;
				textureInfo.LengthCompressed = (uint)texData.Length;
				textureInfo.Data = texData;
				textures.Add(textureInfo);

				memoryOffset += texInfo.TextureLength + texInfo.PaletteLength;

			}
			textures.Sort();

			int initialLen = 0xDC + textures.Count * (0x8 + 0x18);
			int paddingToData = 0x80 - ((initialLen + 8) % 0x80);
			int textureOffsBase = initialLen + 8 + paddingToData + 0x100;

			uint offs = (uint)textureOffsBase;
			foreach(TextureInfo ti in textures)
			{
				ti.Offset = offs;
				offs += ti.LengthCompressed;
				if ((offs % 0x40) != 0)
					offs += 0x40 - (offs % 0x40);
			}
			uint maxOffs = offs;

			TpkFile tpk = new TpkFile();
			tpk.AddBlock(new TpkNull(0x30));
			
			TpkBaseBlock head = new TpkBaseBlock(TpkChunk.TpkHead);
			
			TpkHeadFileInfo fi = new TpkHeadFileInfo();
			fi.GlobalPath = config.GetValue("tpk", "pipelinepath");
			if (fi.GlobalPath == null)
				fi.GlobalPath = "";
			fi.FileHash = Hash(fi.GlobalPath);
			fi.TextureName = config.GetValue("tpk", "identifier");
			if (fi.TextureName == null)
				fi.TextureName = "";
			fi.Version = 5;
			head.AddBlock(fi);

			TpkHeadHash hashes = new TpkHeadHash();
			hashes.Count = textures.Count;
			for(int i=0; i<textures.Count; i++)
			{
				hashes[i] = (textures[i] as TextureInfo).Hash;
			}
			head.AddBlock(hashes);

			TpkHeadDataOffset tpkdo = new TpkHeadDataOffset();
			for(int i=0; i<textures.Count; i++)
			{
				TextureInfo ti = textures[i] as TextureInfo;
				TpkHeadDataOffset.DataOffsetStruct dos = new TpkHeadDataOffset.DataOffsetStruct();
				dos.Flags = 0x100;
				dos.Hash = ti.Hash;
				dos.Length = ti.LengthCompressed;
				dos.RealLength = ti.Length;
				dos.Offset = ti.Offset;
				tpkdo[ti.Hash] = dos;
			}
			head.AddBlock(tpkdo);

			tpk.AddBlock(head);

			tpk.AddBlock(new TpkNull(paddingToData));

			TpkBaseBlock data = new TpkBaseBlock(TpkChunk.TpkData);

			TpkDataHeadLink headlink = new TpkDataHeadLink();
			headlink.Unknown1 = 1;
			headlink.FileHash = fi.FileHash;
			data.AddBlock(headlink);

			data.AddBlock(new TpkNull(0x50));

			TpkDataRaw raw = new TpkDataRaw();
			MemoryStream ms = new MemoryStream((int)maxOffs - textureOffsBase + 0x78);
			BinaryWriter msbw = new BinaryWriter(ms);
			for(int i=0; i<textures.Count; i++)
			{
				TextureInfo ti = textures[i] as TextureInfo;
				msbw.Seek((int)ti.Offset - textureOffsBase + 0x78, SeekOrigin.Begin);
				msbw.Write(ti.Data);
			}
			/*
			if (((ms.Position + 8) % 0x40) != 0)
			{
				msbw.Seek((int)ms.Position + 0x40 - (((int)ms.Position + 8) % 0x40), SeekOrigin.Begin);
				msbw.Seek(-4, SeekOrigin.Current);
				msbw.Write((int)0xAAAAAA);
			}
			*/
			raw.SetDataRaw(ms.GetBuffer());
			ms.Close();
			data.AddBlock(raw);

			tpk.AddBlock(data);
			
			string save = config.GetValue("tpk", "output");
			if (save == null)
				save = "TEXTURES.BIN";
			tpk.Save(save);

			return 0;

		}
	}
}
