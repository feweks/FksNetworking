
namespace FksNetworking;

public class FksNetworkReader : IDisposable
{
    private byte[] data;
    private MemoryStream stream;
    private BinaryReader reader;

    public FksNetworkReader(byte[] data)
    {
        this.data = data;

        stream = new MemoryStream(this.data);
        reader = new BinaryReader(stream);
    }

    public int ReadInt()
    {
        return reader.ReadInt32();
    }

    public string ReadString()
    {
        return reader.ReadString();
    }

    public short ReadShort()
    {
        return reader.ReadInt16();
    }

    public float ReadFloat()
    {
        return reader.ReadSingle();
    }

    public bool ReadBool()
    {
        return reader.ReadBoolean();
    }

    public byte ReadByte()
    {
        return reader.ReadByte();
    }

    public long ReadLong()
    {
        return reader.ReadInt64();
    }

    public uint ReadUInt()
    {
        return reader.ReadUInt32();
    }

    public ushort ReadUShort()
    {
        return reader.ReadUInt16();
    }

    public ulong ReadULong()
    {
        return reader.ReadUInt64();
    }

    public char ReadChar()
    {
        return reader.ReadChar();
    }

    public void Dispose()
    {
        stream.Dispose();
        reader.Dispose();
    }

    public int GetLength()
    {
        return data.Length;
    }

    public static implicit operator byte[](FksNetworkReader reader)
    {
        return reader.data;
    }
}

