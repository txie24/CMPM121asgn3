// File: Assets/Scripts/RPNEvaluator.cs

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Utility for evaluating Reverse‑Polish Notation expressions,
/// both integer and floating‑point, with optional variables.
/// </summary>
public static class RPNEvaluator
{
    /// <summary>
    /// Evaluates a space‑delimited RPN expression as an integer.
    /// Supports variables and the operators +, -, *, /, %.
    /// </summary>
    /// <param name="expression">E.g. "base 5 wave * +"</param>
    /// <param name="variables">Map from variable names to int values.</param>
    /// <returns>The computed integer.</returns>
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
                // operator
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

    /// <summary>
    /// Same as Evaluate, but catches errors and returns a fallback.
    /// </summary>
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

    /// <summary>
    /// Evaluates a space‑delimited RPN expression as a float.
    /// Supports the operators +, -, *, /.
    /// </summary>
    /// <param name="expression">E.g. "power 1.5 *"</param>
    /// <param name="variables">Map from variable names to float values.</param>
    /// <returns>The computed float.</returns>
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
                // operator
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
}
