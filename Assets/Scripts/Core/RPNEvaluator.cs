using System;
using System.Collections.Generic;
using UnityEngine;

public static class RPNEvaluator
{
    /// <summary>
    /// Evaluates a space‐delimited RPN (Reverse Polish Notation) expression.
    /// Supports integers, variables, and the operators +, -, *, /, %.
    /// </summary>
    /// <param name="expression">Tokens separated by spaces, e.g. "base 5 wave * +"</param>
    /// <param name="variables">Dictionary mapping variable names to integer values</param>
    /// <returns>The integer result of the expression</returns>
    public static int Evaluate(string expression, Dictionary<string,int> variables)
    {
        if (string.IsNullOrWhiteSpace(expression))
            throw new ArgumentException("Expression is null or empty", nameof(expression));

        var stack = new Stack<int>();
        var tokens = expression.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var token in tokens)
        {
            // 1) variable?
            if (variables != null && variables.TryGetValue(token, out var varVal))
            {
                stack.Push(varVal);
            }
            // 2) literal integer?
            else if (int.TryParse(token, out var intVal))
            {
                stack.Push(intVal);
            }
            // 3) operator
            else
            {
                if (stack.Count < 2)
                    throw new InvalidOperationException($"Not enough operands for operator '{token}'");

                int b = stack.Pop();
                int a = stack.Pop();
                int res;

                switch (token)
                {
                    case "+": res = a + b; break;
                    case "-": res = a - b; break;
                    case "*": res = a * b; break;
                    case "/":
                        if (b == 0) throw new DivideByZeroException("Division by zero in RPN expression");
                        res = a / b;
                        break;
                    case "%":
                        if (b == 0) throw new DivideByZeroException("Modulus by zero in RPN expression");
                        res = a % b;
                        break;
                    default:
                        throw new InvalidOperationException($"Unrecognized token '{token}' in RPN expression");
                }

                stack.Push(res);
            }
        }

        if (stack.Count != 1)
            throw new InvalidOperationException("RPN expression did not reduce to a single value");

        return stack.Pop();
    }

    /// <summary>
    /// Safe wrapper for Evaluate. Returns fallback if evaluation fails.
    /// </summary>
    public static int SafeEvaluate(string expression, Dictionary<string, int> variables, int fallback)
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
}
