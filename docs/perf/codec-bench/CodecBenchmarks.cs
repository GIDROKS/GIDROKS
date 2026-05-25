using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using K4os.Compression.LZ4;
using MessagePack;
using MessagePack.Resolvers;

namespace CodecBench;

/// <summary>
/// LZ4 vs Brotli on MessagePack-encoded snapshots — the workload we actually ship.
/// Random bytes would be incompressible and invalidate the comparison, so the
/// corpus is generated deterministically with a fixed seed and realistic field shape.
/// </summary>
[MemoryDiagnoser]
[ThreadingDiagnoser]
[Config(typeof(Config))]
public class CodecBenchmarks
{
    private class Config : ManualConfig
    {
        public Config()
        {
            AddDiagnoser(MemoryDiagnoser.Default);
            AddDiagnoser(ThreadingDiagnoser.Default);
        }
    }

    [Params(50, 500, 5_000, 50_000)]
    public int PayloadBytes;

    private byte[] _plain = Array.Empty<byte>();
    private byte[] _lz4 = Array.Empty<byte>();
    private byte[] _brotli = Array.Empty<byte>();
    private byte[] _scratch = Array.Empty<byte>();

    private static readonly MessagePackSerializerOptions Options =
        MessagePackSerializerOptions.Standard.WithResolver(ContractlessStandardResolver.Instance);

    // Brotli quality 4 is the interactive default the production service ships with.
    // BrotliStream exposes only Fastest (~q1) and Optimal (q11) via CompressionLevel,
    // so the benchmark uses BrotliEncoder/Decoder directly to pin quality exactly.
    private const int BrotliQuality = 4;
    private const int BrotliWindow = 22;

    [GlobalSetup]
    public void Setup()
    {
        _plain = MakeSnapshotCorpus(PayloadBytes, seed: 1337);

        // Pre-allocate scratch buffers sized to the worst case so we never measure GC of the output buffer.
        var maxOut = Math.Max(
            LZ4Codec.MaximumOutputSize(_plain.Length),
            BrotliEncoder.GetMaxCompressedLength(_plain.Length));
        _scratch = new byte[maxOut];

        var lz4Len = LZ4Codec.Encode(_plain, 0, _plain.Length, _scratch, 0, _scratch.Length);
        _lz4 = _scratch.AsSpan(0, lz4Len).ToArray();

        BrotliEncoder.TryCompress(_plain, _scratch, out var brLen, BrotliQuality, BrotliWindow);
        _brotli = _scratch.AsSpan(0, brLen).ToArray();
    }

    // ---------- compression ----------

    [Benchmark(Baseline = true), BenchmarkCategory("Compress")]
    public int Compress_LZ4()
    {
        return LZ4Codec.Encode(_plain, 0, _plain.Length, _scratch, 0, _scratch.Length);
    }

    [Benchmark, BenchmarkCategory("Compress")]
    public int Compress_Brotli_Q4()
    {
        BrotliEncoder.TryCompress(_plain, _scratch, out var written, BrotliQuality, BrotliWindow);
        return written;
    }

    // ---------- decompression ----------

    [Benchmark, BenchmarkCategory("Decompress")]
    public int Decompress_LZ4()
    {
        return LZ4Codec.Decode(_lz4, 0, _lz4.Length, _scratch, 0, _scratch.Length);
    }

    [Benchmark, BenchmarkCategory("Decompress")]
    public int Decompress_Brotli()
    {
        BrotliDecoder.TryDecompress(_brotli, _scratch, out var written);
        return written;
    }

    // ---------- corpus ----------

    /// <summary>
    /// Deterministic, MessagePack-encoded "snapshot" corpus with realistic repetition
    /// (entity ids, recurring strings, float clusters). Reproduces compressibility close
    /// to what we see on gameplay traffic without shipping a real capture.
    /// Exact bucket sizes use a <c>pad</c> field inside the document — never raw trimming.
    /// </summary>
    private static byte[] MakeSnapshotCorpus(int targetBytes, int seed)
    {
        var rng = new Random(seed);
        var entities = new List<object>();
        var template = new[] { "vehicle", "pedestrian", "prop", "trigger", "vfx" };

        while (SerializeSnapshotCorpus(entities, padSize: 0).Length < targetBytes)
        {
            entities.Add(MakeSnapshotEntity(rng, template));

            if (SerializeSnapshotCorpus(entities, padSize: 0).Length > targetBytes)
            {
                entities.RemoveAt(entities.Count - 1);
                break;
            }
        }

        var padSize = 0;
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var blob = SerializeSnapshotCorpus(entities, padSize);
            var delta = targetBytes - blob.Length;
            if (delta == 0)
            {
                MessagePackSerializer.Deserialize<object>(blob, Options);
                return blob;
            }

            padSize += delta;
            if (padSize < 0)
            {
                throw new InvalidOperationException(
                    $"Snapshot corpus for {targetBytes} B cannot fit after {entities.Count} entities.");
            }
        }

        throw new InvalidOperationException(
            $"Could not converge on a valid {targetBytes} B MessagePack snapshot corpus.");
    }

    private static object MakeSnapshotEntity(Random rng, string[] template) => new
    {
        id = rng.Next(1, 5_000),
        kind = template[rng.Next(template.Length)],
        x = (float)(rng.NextDouble() * 1024 - 512),
        y = (float)(rng.NextDouble() * 8),
        z = (float)(rng.NextDouble() * 1024 - 512),
        vx = (float)(rng.NextDouble() * 4 - 2),
        vy = 0f,
        vz = (float)(rng.NextDouble() * 4 - 2),
        state = rng.Next(0, 8),
    };

    private static byte[] SerializeSnapshotCorpus(IReadOnlyList<object> entities, int padSize) =>
        MessagePackSerializer.Serialize(
            new { entities, pad = padSize == 0 ? Array.Empty<byte>() : new byte[padSize] },
            Options);
}
