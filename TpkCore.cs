using System;
using System.Collections;
using System.IO;
using System.Text;

namespace mwtc.Tpk
{
	#region "TPK Core"
	public enum TpkChunk : uint
	{
		TpkModelRoot = 0x80134000,
		TpkTextureRoot = 0xB3300000,
		TpkAnimationRoot = 0x00E34010,

		TpkHead = 0xB3310000,
		TpkHeadFileInfo = 0x33310001,
		TpkHeadHash = 0x33310002,
		TpkHeadDataOffset = 0x33310003,
		TpkHeadDesc = 0x33310004,
		TpkHeadFormat = 0x33310005,

		TpkData = 0xB3320000,
		TpkDataHeadLink = 0x33320001,
		TpkDataRaw = 0x33320002,

		TpkNull = 0x0,
	}

	public struct TpkHeader
	{
		public TpkChunk Id;
		public uint Length;

		public void Read(BinaryReader br)
		{
			Id = (TpkChunk)br.ReadUInt32();
			Length = br.ReadUInt32();
		}

		public void Write(BinaryWriter bw)
		{
			bw.Write((uint)Id);
			bw.Write(Length);
		}
	}

	public abstract class TpkBase
	{
		protected TpkBaseBlock _root;
		protected TpkHeader _header;
		public TpkHeader Header
		{
			get { return _header; }
			set { _header = value; }
		}
		public abstract void Read(BinaryReader br);
		protected abstract void WriteData(BinaryWriter bw);
		
		public TpkBase()
		{
		}

		public TpkBase(TpkChunk id)
		{
			_header.Id = id;
		}

		public void Write(BinaryWriter bw)
		{
			_header.Write(bw);
			long position = bw.BaseStream.Position;
			WriteData(bw);
			uint length = (uint)(bw.BaseStream.Position - position);
			bw.BaseStream.Seek(position-0x4, SeekOrigin.Begin);
			bw.Write(length);
			bw.Seek((int)length, SeekOrigin.Current);
		}

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
	}

	public class TpkBaseBlock : TpkBase, IEnumerable
	{
		protected ArrayList _blocks;

		public TpkBaseBlock()
		{
			_blocks = new ArrayList();
		}

		public TpkBaseBlock(TpkChunk id)
		{
			_header.Id = id;
			_blocks = new ArrayList();
		}

		public TpkBase FindByChunk(TpkChunk id)
		{
			foreach(TpkBase block in _blocks)
			{
				if (block is TpkBaseBlock) 
				{
					TpkBase ret = (block as TpkBaseBlock).FindByChunk(id);
					if (ret != null)
						return ret;
				} 
				else
				{
					if (block.Header.Id == id)
						return block;
				}
			}
			return null;
		}

		public TpkBase this[int index]
		{
			get { return _blocks[index] as TpkBase; }
			set { _blocks[index] = value; }
		}

		public TpkBase this[TpkChunk id]
		{
			get
			{
				foreach(TpkBase block in _blocks)
				{
					if (block.Header.Id == id)
						return block;
				}
				return null;
			}
		}

		public int Count
		{
			get
			{
				return _blocks.Count;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return _blocks.GetEnumerator();
		}

		public void AddBlock(TpkBase block)
		{
			_blocks.Add(block);
		}

		public override void Read(BinaryReader br)
		{
			const uint IsParentBlock = 0x80000000;
			
			_blocks = new ArrayList();
			long position = br.BaseStream.Position;
			while((br.BaseStream.Position - position) < _header.Length)
			{
				TpkBase block = null;
				TpkHeader header = new TpkHeader();
				header.Read(br);
				long positionLocal = br.BaseStream.Position;
				if (((uint)header.Id & IsParentBlock) != 0)
				{
					block = new TpkBaseBlock();
				} 
				else
				{
					switch(header.Id)
					{
						case TpkChunk.TpkHeadFileInfo:
							block = new TpkHeadFileInfo();
							break;
						case TpkChunk.TpkHeadHash:
							block = new TpkHeadHash();
							break;
						case TpkChunk.TpkHeadDataOffset:
							block = new TpkHeadDataOffset();
							break;
						case TpkChunk.TpkDataHeadLink:
							block = new TpkDataHeadLink();
							break;
						case TpkChunk.TpkDataRaw:
							block = new TpkDataRaw();
							break;
						case TpkChunk.TpkNull:
							block = new TpkNull();
							break;
						default:
							block = new TpkUnknown();
							break;
					}
				}
				block.Header = header;
				block.Read(br);
				_blocks.Add(block);
				br.BaseStream.Seek(positionLocal+header.Length, SeekOrigin.Begin);
			}
		}

		protected override void WriteData(BinaryWriter bw)
		{
			foreach(TpkBase block in _blocks)
			{
				block.Write(bw);
			}
		}

	}	

	#endregion

	#region "TPK Data Classes"
	public class TpkHeadFileInfo : TpkBase
	{
		public int Version;
		public string TextureName;
		public string GlobalPath;
		public uint FileHash;
		private byte[] _padding;

		public TpkHeadFileInfo() : base(TpkChunk.TpkHeadFileInfo)
		{
		}

		public override void Read(BinaryReader br)
		{
			Version = br.ReadInt32();
			TextureName = ReadString(br, 0x1C);
			GlobalPath = ReadString(br, 0x40);
			FileHash = br.ReadUInt32();
			_padding = br.ReadBytes(0x18);
		}

		protected override void WriteData(BinaryWriter bw)
		{
			bw.Write(Version);
			WriteString(bw, TextureName, 0x1C);
			WriteString(bw, GlobalPath, 0x40);
			bw.Write(FileHash);
			if (_padding == null) 
				_padding = new byte[0x18];
			bw.Write(_padding);
		}

	}

	public class TpkHeadHash : TpkBase, IEnumerable
	{
		private uint[] _hash;
		private int[] _padding;

		public TpkHeadHash() : base(TpkChunk.TpkHeadHash)
		{
		}

		public int Count
		{
			get { return _hash.Length; }
			set
			{
				_hash = new uint[value];
				_padding = new int[value];
			}
		}
		public uint this[int index]
		{
			get { return _hash[index]; }
			set { _hash[index] = value; }
		}
		public override void Read(BinaryReader br)
		{
			int length = (int)(_header.Length / 0x8);
			_hash = new uint[length];
			_padding = new int[length];
			for(int i=0; i<length; i++)
			{
				_hash[i] = br.ReadUInt32();
				_padding[i] = br.ReadInt32();
			}
		}

		protected override void WriteData(BinaryWriter bw)
		{
			for(int i=0; i<_hash.Length; i++)
			{
				bw.Write(_hash[i]);
				bw.Write(_padding[i]);
			}
		}
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _hash.GetEnumerator();
		}

		#endregion
	}

	public class TpkHeadDataOffset : TpkBase, IEnumerable
	{
		public class DataOffsetStruct
		{
			public uint Hash;
			public uint Offset;
			public uint Length;
			public uint RealLength;
			public int Flags;
			public int Padding;
		}

		private Hashtable _data;

		public TpkHeadDataOffset() : base(TpkChunk.TpkHeadDataOffset)
		{
			_data = new Hashtable();
		}

		public DataOffsetStruct this[uint hash]
		{
			get { return (DataOffsetStruct)_data[hash]; }
			set { _data[hash] = value; }
		}

		public override void Read(BinaryReader br)
		{
			int length = (int)(_header.Length / 0x18);
			for(int i=0; i<length; i++)
			{
				DataOffsetStruct dos = new DataOffsetStruct();
				dos.Hash = br.ReadUInt32();
				dos.Offset = br.ReadUInt32();
				dos.Length = br.ReadUInt32();
				dos.RealLength = br.ReadUInt32();
				dos.Flags = br.ReadInt32();
				dos.Padding = br.ReadInt32();
				_data[dos.Hash] = dos;
			}
		}

		protected override void WriteData(BinaryWriter bw)
		{
			ArrayList list = new ArrayList(_data.Count);
			foreach(DictionaryEntry de in _data)
			{
				list.Add((de.Value as DataOffsetStruct).Hash);
			}
			list.Sort();

			foreach(uint hash in list)
			{
				DataOffsetStruct dos = _data[hash] as DataOffsetStruct;
				bw.Write(dos.Hash);
				bw.Write(dos.Offset);
				bw.Write(dos.Length);
				bw.Write(dos.RealLength);
				bw.Write(dos.Flags);
				bw.Write(dos.Padding);
			}
		}
		#region IEnumerable Members

		public IEnumerator GetEnumerator()
		{
			return _data.GetEnumerator();
		}

		#endregion
	}
	public class TpkDataHeadLink : TpkBase
	{
		public int Null1, Null2;
		public int Unknown1;
		public uint FileHash;
		public int Null3, Null4;

		public TpkDataHeadLink() : base(TpkChunk.TpkDataHeadLink)
		{
			
		}

		public override void Read(BinaryReader br)
		{
			Null1 = br.ReadInt32();
			Null2 = br.ReadInt32();
			Unknown1 = br.ReadInt32();
			FileHash = br.ReadUInt32();
			Null3 = br.ReadInt32();
			Null4 = br.ReadInt32();
		}

		protected override void WriteData(BinaryWriter bw)
		{
			bw.Write(Null1);
			bw.Write(Null2);
			bw.Write(Unknown1);
			bw.Write(FileHash);
			bw.Write(Null3);
			bw.Write(Null4);

		}

	}

	public class TpkDataRaw : TpkBase
	{
		private byte[] _data;
		private long _position;

        public TpkDataRaw() : base(TpkChunk.TpkDataRaw)
        {
        	
        }

		public void SetDataRaw(byte[] data)
		{
			_header.Length = (uint)data.Length;
			_data = data;
		}

		public byte[] GetData(long offset, uint length)
		{
			long relOffset = offset - _position;
			byte[] data = new byte[length];
			Array.Copy(_data, relOffset, data, 0, length);
			return data;
		}

		public void SetData(long offset, uint length, byte[] data)
		{
			long relOffset = offset - _position;
			if (data.Length == length)
			{
				Array.Copy(data, 0, _data, relOffset, length);
			} 
			else
			{
				long newSize = relOffset + data.Length + (_data.Length - relOffset - length);
				byte[] newData = new byte[newSize];
				Array.Copy(_data, 0, newData, 0, relOffset);
				Array.Copy(data, 0, newData, relOffset, data.Length);
				Array.Copy(_data, relOffset+length, newData, relOffset+data.Length, _data.Length-relOffset-length);
				_data = newData;
				GC.Collect();
			}
		}
		
		public override void Read(BinaryReader br)
		{
			_position = br.BaseStream.Position;
			if (_header.Length > 0)
				_data = br.ReadBytes((int)_header.Length);
			else
				_data = null;
		}

		protected override void WriteData(BinaryWriter bw)
		{
			_position = bw.BaseStream.Position;
			if (_header.Length > 0)
				bw.Write(_data);
		}

	}

	public class TpkNull : TpkBase
	{
		private byte[] _data;

		public TpkNull() : base(TpkChunk.TpkNull)
		{
			
		}
		
		public TpkNull(int length) : base(TpkChunk.TpkNull)
		{
			_data = new byte[length];
			_header.Length = (uint)length;
		}

		public override void Read(BinaryReader br)
		{
			if (_header.Length > 0)
				_data = br.ReadBytes((int)_header.Length);
			else
				_data = null;
		}

		protected override void WriteData(BinaryWriter bw)
		{
			if (_header.Length > 0)
				bw.Write(_data);
		}

	}

	public class TpkUnknown : TpkNull {}

	#endregion

	public class TpkFile : TpkBaseBlock
	{
		private string _lastFile;
		
		public TpkFile() : base(TpkChunk.TpkTextureRoot)
		{
		}

		public void Open(string filename)
		{
			try
			{
				FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
				_lastFile = filename;
				BinaryReader br = new BinaryReader(fs);
				Read(br);
				fs.Close();
			} 
			catch (Exception e)
			{
				throw new Exception("Could not open texture file. " + e.Message);
			}
		}
		public void Save()
		{
			Save(_lastFile);
		}
		public void Save(string filename)
		{
			try
			{
				FileStream fs = new FileStream(filename, FileMode.Create, FileAccess.Write);
				BinaryWriter bw = new BinaryWriter(fs);
				Write(bw);
				fs.Close();
			} 
			catch (Exception e)
			{
				throw new Exception("Could not save texture file. " + e.Message);
			}
		}
		protected new void Read(BinaryReader br)
		{
			br.BaseStream.Seek(0, SeekOrigin.Begin);
			_header = new TpkHeader();
			_header.Length = (uint)br.BaseStream.Length;
			base.Read(br);
		}
		protected new void Write(BinaryWriter bw)
		{
			base.Write(bw);
		}
	}
}
