using NyaAI.Decision;
using NyaAI.Numpy;

namespace NyaAI.Jev;

/// <summary>
/// FP32 decision-head Jev-Omni: линейный слой над стандартизованным последним
/// hidden-состоянием (3840). Соответствует decision-head-f32.npz.
/// </summary>
internal sealed class JevOmniHead
{
    public const int HiddenSize = 3840;
    public const int MaxOptions = 256;

    private readonly float[] _weight; // [MaxOptions * HiddenSize], row-major
    private readonly float[] _bias;   // [MaxOptions]
    private readonly float[] _mu;     // [HiddenSize]
    private readonly float[] _sd;     // [HiddenSize]

    private JevOmniHead(float[] weight, float[] bias, float[] mu, float[] sd)
    {
        _weight = weight;
        _bias = bias;
        _mu = mu;
        _sd = sd;
    }

    public static JevOmniHead Load(string path)
    {
        if (!File.Exists(path))
            throw new NyaAIException($"Не найден файл decision-head: {path}");

        var npz = NpzArchive.Open(path);

        var weight = npz["linear.weight"];
        var bias = npz["linear.bias"];
        var mu = npz["mu"];
        var sd = npz["sd"];

        if (weight.Shape.Length != 2 || weight.Shape[1] != HiddenSize)
            throw new NyaAIException($"Ожидался linear.weight (N, {HiddenSize}), получено ({string.Join(",", weight.Shape)}).");
        if (weight.Shape[0] != MaxOptions)
            throw new NyaAIException($"Ожидалось {MaxOptions} вариантов в голове, получено {weight.Shape[0]}.");
        if (mu.Data.Length != HiddenSize || sd.Data.Length != HiddenSize)
            throw new NyaAIException("Ожидались mu/sd размера 3840.");

        return new JevOmniHead(weight.Data, bias.Data, mu.Data, sd.Data);
    }

    /// <summary>Логиты вариантов 0..count-1 над последним hidden-состоянием.</summary>
    public double[] Logits(ReadOnlySpan<float> hidden, int count)
    {
        if (hidden.Length != HiddenSize)
            throw new NyaAIException($"Ожидалось {HiddenSize} скрытых измерений, получено {hidden.Length}.");
        if (count < 2 || count > MaxOptions)
            throw new NyaAIException($"Число вариантов должно быть 2..{MaxOptions}.");

        var z = new double[count];
        for (int i = 0; i < count; i++)
        {
            var offset = i * HiddenSize;
            double sum = 0;
            for (int j = 0; j < HiddenSize; j++)
            {
                var norm = (hidden[j] - _mu[j]) / _sd[j];
                sum += _weight[offset + j] * norm;
            }
            z[i] = sum + _bias[i];
        }
        return z;
    }
}
