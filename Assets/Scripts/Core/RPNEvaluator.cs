// File: Assets/Scripts/RPNEvaluator.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public static class RPNEvaluator
{
    public static int Evaluate(string expression, Dictionary<string,int> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression empty", nameof(expression));

        var stack = new Stack<int>();
        foreach (var tok in expression.Split(' '))
        {
            if (variables != null && variables.TryGetValue(tok, out int v))
            {
                stack.Push(v);
            }
            else if (int.TryParse(tok, out int i))
            {
                stack.Push(i);
            }
            else
            {
                int b = stack.Pop(), a = stack.Pop(), r;
                switch (tok)
                {
                    case "+": r = a + b; break;
                    case "-": r = a - b; break;
                    case "*": r = a * b; break;
                    case "/": r = a / b; break;
                    case "%": r = a % b; break;
                    default:  throw new InvalidOperationException($"Unknown op {tok}");
                }
                stack.Push(r);
            }
        }

        return stack.Pop();
    }

    public static int SafeEvaluate(string expression, Dictionary<string,int> variables, int fallback)
    {
        try
        {
            return Evaluate(expression, variables);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"RPN SafeEvaluate failed for '{expression}': {ex.Message}");
            return fallback;
        }
    }

    public static float EvaluateFloat(string expression, Dictionary<string,float> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression empty", nameof(expression));

        var stack = new Stack<float>();
        foreach (var tok in expression.Split(' '))
        {
            if (variables != null && variables.TryGetValue(tok, out float v))
            {
                stack.Push(v);
            }
            else if (float.TryParse(tok, out float f))
            {
                stack.Push(f);
            }
            else
            {
                float b = stack.Pop(), a = stack.Pop(), r;
                switch (tok)
                {
                    case "+": r = a + b; break;
                    case "-": r = a - b; break;
                    case "*": r = a * b; break;
                    case "/": r = a / b; break;
                    default:  throw new InvalidOperationException($"Unknown op {tok}");
                }
                stack.Push(r);
            }
        }

        return stack.Pop();
    }

    public static float SafeEvaluateFloat(string expression, Dictionary<string, float> variables, float fallback = 0f)
    {
        try
        {
            return EvaluateFloat(expression, variables);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"RPN SafeEvaluateFloat failed for '{expression}': {ex.Message}");
            return fallback;
        }
    }
}
