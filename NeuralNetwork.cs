using System;
using UnityEngine;

[Serializable]
public class NeuralNetwork
{
    private int inputSize;
    private int hiddenSize;
    private int outputSize;

    public float[] weights;

    private float[] hidden;
    private float[] output;

    public NeuralNetwork(int inputs, int hidden, int outputs)
    {
        inputSize = inputs;
        hiddenSize = hidden;
        outputSize = outputs;

        int weightCount = (inputs * hidden) + (hidden * outputs);
        weights = new float[weightCount];
        this.hidden = new float[hidden];
        output = new float[outputs];

        Randomize();
    }

    public void Randomize()
    {
        for (int i = 0; i < weights.Length; i++)
            weights[i] = UnityEngine.Random.Range(-1f, 1f);
    }

    public float[] Forward(float[] inputs)
    {
        int w = 0;
        for (int h = 0; h < hiddenSize; h++)
        {
            float sum = 0f;
            for (int i = 0; i < inputSize; i++)
                sum += inputs[i] * weights[w++];
            hidden[h] = MathExtensions.Tanh(sum);
        }

        for (int o = 0; o < outputSize; o++)
        {
            float sum = 0f;
            for (int h = 0; h < hiddenSize; h++)
                sum += hidden[h] * weights[w++];
            output[o] = MathExtensions.Tanh(sum);
        }

        return output;
    }

    public NeuralNetwork Clone()
    {
        var copy = new NeuralNetwork(inputSize, hiddenSize, outputSize);
        Array.Copy(weights, copy.weights, weights.Length);
        return copy;
    }

    public void Mutate(float rate = 0.1f, float strength = 0.3f)
    {
        for (int i = 0; i < weights.Length; i++)
            if (UnityEngine.Random.value < rate)
                weights[i] += UnityEngine.Random.Range(-strength, strength);
    }

    public byte[] ToBytes()
    {
        using var ms = new System.IO.MemoryStream();
        using var bw = new System.IO.BinaryWriter(ms);

        bw.Write(inputSize);
        bw.Write(hiddenSize);
        bw.Write(outputSize);
        bw.Write(weights != null ? weights.Length : 0);

        if (weights != null)
        {
            for (int i = 0; i < weights.Length; i++)
                bw.Write(weights[i]);
        }

        bw.Flush();
        return ms.ToArray();
    }

    public static NeuralNetwork FromBytes(byte[] data)
    {
        if (data == null || data.Length < 16) return null;

        try
        {
            using var ms = new System.IO.MemoryStream(data);
            using var br = new System.IO.BinaryReader(ms);

            int input = br.ReadInt32();
            int hidden = br.ReadInt32();
            int output = br.ReadInt32();
            int weightCount = br.ReadInt32();

            var nn = new NeuralNetwork(input, hidden, output);

            if (nn.weights == null || nn.weights.Length != weightCount)
            {
                nn.weights = new float[weightCount];
            }

            for (int i = 0; i < weightCount; i++)
                nn.weights[i] = br.ReadSingle();

            return nn;
        }
        catch
        {
            return null;
        }
    }
}



