using UnityEngine;

public static class MathExtensions
{
    public static float Tanh(float x)
    {
        float exp2x = Mathf.Exp(2f * x);
        return (exp2x - 1f) / (exp2x + 1f);
    }
}

