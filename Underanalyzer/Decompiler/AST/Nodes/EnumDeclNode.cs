﻿using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents an enum declaration node in the AST. Only appears during AST cleanup.
/// </summary>
public class EnumDeclNode : IStatementNode
{
    /// <summary>
    /// The enum being declared.
    /// </summary>
    public GMEnum Enum { get; }

    public bool SemicolonAfter => false;
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public EnumDeclNode(GMEnum gmEnum)
    {
        Enum = gmEnum;
    }

    public IStatementNode Clean(ASTCleaner cleaner)
    {
        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundEnums;
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        printer.Write("enum ");
        printer.Write(Enum.Name);
        printer.OpenBlock();

        // Sort values of enum by value
        List<GMEnumValue> sorted = new(Enum.Values);
        sorted.Sort((a, b) => Math.Sign(a.Value - b.Value));

        // Print values of this enum
        bool first = true;
        long expectedValue = 0;
        foreach (GMEnumValue value in sorted)
        {
            // Print comma and newline if not the first value (workaround for enumeration)
            if (first)
            {
                first = false;
            }
            else
            {
                printer.Write(',');
                printer.EndLine();
            }

            printer.StartLine();
            printer.Write(value.Name);

            if (value.Value == expectedValue)
            {
                // Our enum value matches the expected auto-generated value, so don't write it out
                if (expectedValue != long.MaxValue)
                {
                    // Increment to next auto-generated value (without overflow)
                    expectedValue++;
                }
            }
            else
            {
                // Our enum value does NOT match the expected value, so manually write it out
                printer.Write(" = ");
                printer.Write(value.Value);
                if (value.Value != long.MaxValue)
                {
                    // Adjust next expected value (without overflow)
                    expectedValue = value.Value + 1;
                }
                else
                {
                    // Avoid overflow
                    expectedValue = value.Value;
                }
            }
        }
        printer.EndLine();

        printer.CloseBlock();
    }

    /// <summary>
    /// Generates enum declarations for the given context, returning a modified AST.
    /// </summary>
    internal static void GenerateDeclarations(ASTCleaner cleaner, IStatementNode ast)
    {
        if (ast is not BlockNode block)
        {
            throw new DecompilerException($"Expected final AST to be a {nameof(BlockNode)}");
        }

        // Create nodes
        List<EnumDeclNode> enums = new(cleaner.Context.EnumDeclarations.Count);
        foreach (GMEnum gmEnum in cleaner.Context.EnumDeclarations)
        {
            enums.Add(new EnumDeclNode(gmEnum));
        }

        // Insert nodes
        if (cleaner.Context.Settings.MacroDeclarationsAtTop)
        {
            block.Children.InsertRange(0, enums);
        }
        else
        {
            block.Children.AddRange(enums);
        }

        // Perform cleanup on the nodes
        foreach (EnumDeclNode enumDecl in enums)
        {
            enumDecl.Clean(cleaner);
        }
    }
}
