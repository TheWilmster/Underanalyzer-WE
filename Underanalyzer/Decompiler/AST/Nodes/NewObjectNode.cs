﻿using System;
using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents the "new" keyword being used to instantiate an object in the AST.
/// </summary>
public class NewObjectNode : IExpressionNode
{
    /// <summary>
    /// The function (constructor) being used.
    /// </summary>
    public IExpressionNode Function { get; }

    /// <summary>
    /// The arguments passed into the function (constructor).
    /// </summary>
    public List<IExpressionNode> Arguments { get; }

    public bool Duplicated { get; set; }
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    public NewObjectNode(IExpressionNode function, List<IExpressionNode> arguments)
    {
        Function = function;
        Arguments = arguments;
    }

    public void Print(ASTPrinter printer)
    {
        throw new NotImplementedException();
    }
}
