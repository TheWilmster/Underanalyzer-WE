﻿using System;
using System.Collections.Generic;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a single block of code in the AST.
/// Blocks can have an arbitrary number of child nodes.
/// </summary>
public class BlockNode : IFragmentNode
{
    /// <summary>
    /// Whether or not curly braces are required for this block.
    /// </summary>
    public bool UseBraces { get; set; } = true;

    /// <summary>
    /// All children contained within this block.
    /// </summary>
    public List<IStatementNode> Children { get; internal set; } = new();

    public ASTFragmentContext FragmentContext { get; }

    public BlockNode(ASTFragmentContext fragmentContext)
    {
        FragmentContext = fragmentContext;
    }

    public IFragmentNode Clean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].Clean(cleaner);
        }
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Children.Count; i++)
        {
            Children[i] = Children[i].Clean(cleaner);
        }
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        if (UseBraces)
        {
            printer.OpenBlock();
        }

        for (int i = 0; i < Children.Count; i++)
        {
            printer.StartLine();
            Children[i].Print(printer);
            printer.EndLine();
        }

        if (UseBraces)
        {
            printer.CloseBlock();
        }
    }
}