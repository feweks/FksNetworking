
namespace FksNetworking;

public class FksNetworkWriter : IDisposable
{
    private MemoryStream stream;
    private BinaryWriter writer;

    public FksNetworkWriter()
    {
        stream = new MemoryStream();
        writer = new BinaryWriter(stream);
    }

    public void WriteInt(int value)
    {
        writer.Write(value);
    }

    public void WriteString(string value)
    {
        writer.Write(value);
    }

    public void WriteShort(short value)
    {
        writer.Write(value);
    }

    public void WriteFloat(float value)
    {
        writer.Write(value);
    }

    public void WriteBool(bool value)
    {
        writer.Write(value);
    }

    public void WriteByte(byte value)
    {
        writer.Write(value);
    }

    public void WriteLong(long value)
    {
        writer.Write(value);
    }

    public void WriteUInt(uint value)
    {
        writer.Write(value);
    }

    public void WriteUShort(ushort value)
    {
        writer.Write(value);
    }

    public void WriteULong(ulong value)
    {
        writer.Write(value);
    }

    public void WriteChar(char value)
    {
        writer.Write(value);
    }

    public void Append(FksNetworkWriter writer)
    {
        stream.Position = 0;
        writer.stream.Position = 0;

        MemoryStream final = new MemoryStream();
        stream.CopyTo(final);
        writer.stream.CopyTo(final);

        stream = final;
    }

    public int GetLength()
    {
        return stream.ToArray().Length;
    }

    public void Dispose()
    {
        stream.Dispose();
        writer.Dispose();
    }

    public static implicit operator byte[](FksNetworkWriter writer)
    {
        return writer.stream.ToArray();
    }
}
