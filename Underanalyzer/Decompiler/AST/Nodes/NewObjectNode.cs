﻿using System;
using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents the "new" keyword being used to instantiate an object in the AST.
/// </summary>
public class NewObjectNode : IExpressionNode, IStatementNode
{
    /// <summary>
    /// The function (constructor) being used.
    /// </summary>
    public IExpressionNode Function { get; private set; }

    /// <summary>
    /// The arguments passed into the function (constructor).
    /// </summary>
    public List<IExpressionNode> Arguments { get; private set; }

    public bool Duplicated { get; set; }
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;

    public NewObjectNode(IExpressionNode function, List<IExpressionNode> arguments)
    {
        Function = function;
        Arguments = arguments;
    }

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        Function = Function.Clean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        Function = Function.Clean(cleaner);
        for (int i = 0; i < Arguments.Count; i++)
        {
            Arguments[i] = Arguments[i].Clean(cleaner);
        }
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        throw new NotImplementedException();
    }
}