using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using NyaAI.Decision;

namespace NyaAI.Numpy;

/// <summary>Массив из .npy/.npz, приведённый к float и C-порядку.</summary>
public sealed class NpArray
{
    public int[] Shape { get; }
    public float[] Data { get; }

    internal NpArray(int[] shape, float[] data)
    {
        Shape = shape;
        Data = data;
    }

    /// <summary>Первый размер (число строк), либо 1 для одномерного.</summary>
    public int Rows => Shape.Length >= 2 ? Shape[0] : 1;

    /// <summary>Второй размер (число столбцов), либо длина для одномерного.</summary>
    public int Cols => Shape.Length >= 2 ? Shape[1] : (Shape.Length == 1 ? Shape[0] : 1);

    public float this[int i] => Data[i];
}

/// <summary>Минимальный читатель numpy .npz (zip из .npy) — только для нужных типов.</summary>
public sealed class NpzArchive
{
    private readonly Dictionary<string, NpArray> _arrays;

    private NpzArchive(Dictionary<string, NpArray> arrays) => _arrays = arrays;

    public IReadOnlyDictionary<string, NpArray> Arrays => _arrays;

    public NpArray this[string name] =>
        _arrays.TryGetValue(name, out var a)
            ? a
            : throw new NyaAIException($"В .npz нет массива '{name}'. Есть: {string.Join(", ", _arrays.Keys)}");

    public static NpzArchive Open(string path)
    {
        using var fs = File.OpenRead(path);
        return Read(fs);
    }

    public static NpzArchive Read(Stream stream)
    {
        var arrays = new Dictionary<string, NpArray>(StringComparer.Ordinal);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        foreach (var entry in zip.Entries)
        {
            if (entry.Name.Length == 0) continue;
            if (!entry.Name.EndsWith(".npy", StringComparison.OrdinalIgnoreCase)) continue;

            using var es = entry.Open();
            var array = ReadNpy(es);
            var key = entry.Name[..^4]; // без ".npy"
            arrays[key] = array;
        }

        return new NpzArchive(arrays);
    }

    private static NpArray ReadNpy(Stream stream)
    {
        // magic: \x93 N U M P Y
        var magic = new byte[6];
        ReadExact(stream, magic);
        if (magic[0] != 0x93 || magic[1] != (byte)'N' || magic[2] != (byte)'U' ||
            magic[3] != (byte)'M' || magic[4] != (byte)'P' || magic[5] != (byte)'Y')
        {
            throw new NyaAIException("Некорректный .npy (magic).");
        }

        int major = stream.ReadByte();
        stream.ReadByte(); // minor

        int headerLen;
        if (major == 1)
        {
            var b = new byte[2];
            ReadExact(stream, b);
            headerLen = BitConverter.ToUInt16(b, 0);
        }
        else
        {
            var b = new byte[4];
            ReadExact(stream, b);
            headerLen = (int)BitConverter.ToUInt32(b, 0);
        }

        var headerBytes = new byte[headerLen];
        ReadExact(stream, headerBytes);
        var header = Encoding.UTF8.GetString(headerBytes);

        var descr = Regex.Match(header, @"'descr'\s*:\s*'([^']+)'").Groups[1].Value;
        var fortran = header.Contains("'fortran_order': True");
        var shapeStr = Regex.Match(header, @"'shape'\s*:\s*\(([^)]*)\)").Groups[1].Value.Trim();

        var shape = shapeStr.Length == 0
            ? Array.Empty<int>()
            : shapeStr.Split(',', StringSplitOptions.RemoveEmptyEntries)
                     .Select(s => int.Parse(s.Trim())).ToArray();

        if (fortran)
            throw new NyaAIException("fortran_order=True не поддерживается.");

        long count = 1;
        foreach (var d in shape) count *= d;
        if (shape.Length == 0) count = 1;

        var data = ReadData(stream, descr, count);
        return new NpArray(shape, data);
    }

    private static float[] ReadData(Stream stream, string descr, long count)
    {
        var littleEndian = !descr.StartsWith('>');
        var size = descr.Length >= 3 ? int.Parse(descr.AsSpan(2).ToString()) : 1;
        var kind = descr.Length >= 2 ? descr[1] : 'f';

        var raw = new byte[count * size];
        ReadExact(stream, raw);

        var result = new float[count];
        for (long i = 0; i < count; i++)
        {
            var span = raw.AsSpan((int)(i * size), size);
            result[i] = kind switch
            {
                'f' when size == 4 => ToFloat(span, littleEndian),
                'f' when size == 8 => (float)ToDouble(span, littleEndian),
                'i' when size == 1 => (sbyte)span[0],
                'i' when size == 2 => ToInt16(span, littleEndian),
                'i' when size == 4 => ToInt32(span, littleEndian),
                'i' when size == 8 => ToInt64(span, littleEndian),
                'u' when size == 1 => span[0],
                _ => throw new NyaAIException($"Тип '{descr}' не поддерживается.")
            };
        }
        return result;
    }

    private static float ToFloat(ReadOnlySpan<byte> b, bool le)
    {
        if (!BitConverter.IsLittleEndian) le = !le;
        if (le) return BitConverter.ToSingle(b);
        Span<byte> tmp = stackalloc byte[4];
        b.CopyTo(tmp);
        tmp.Reverse();
        return BitConverter.ToSingle(tmp);
    }

    private static double ToDouble(ReadOnlySpan<byte> b, bool le)
    {
        if (!BitConverter.IsLittleEndian) le = !le;
        if (le) return BitConverter.ToDouble(b);
        Span<byte> tmp = stackalloc byte[8];
        b.CopyTo(tmp);
        tmp.Reverse();
        return BitConverter.ToDouble(tmp);
    }

    private static short ToInt16(ReadOnlySpan<byte> b, bool le)
    {
        if (!BitConverter.IsLittleEndian) le = !le;
        return le ? BitConverter.ToInt16(b) : System.Buffers.Binary.BinaryPrimitives.ReadInt16BigEndian(b);
    }

    private static int ToInt32(ReadOnlySpan<byte> b, bool le)
    {
        if (!BitConverter.IsLittleEndian) le = !le;
        return le ? BitConverter.ToInt32(b) : System.Buffers.Binary.BinaryPrimitives.ReadInt32BigEndian(b);
    }

    private static long ToInt64(ReadOnlySpan<byte> b, bool le)
    {
        if (!BitConverter.IsLittleEndian) le = !le;
        return le ? BitConverter.ToInt64(b) : System.Buffers.Binary.BinaryPrimitives.ReadInt64BigEndian(b);
    }

    private static void ReadExact(Stream stream, Span<byte> buffer)
    {
        var total = 0;
        while (total < buffer.Length)
        {
            var read = stream.Read(buffer[total..]);
            if (read <= 0) throw new NyaAIException("Неожиданный конец файла .npy.");
            total += read;
        }
    }
}
