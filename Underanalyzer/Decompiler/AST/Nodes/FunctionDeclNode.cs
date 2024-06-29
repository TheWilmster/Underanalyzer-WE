﻿using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler.GameSpecific;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// A function declaration within the AST.
/// </summary>
public class FunctionDeclNode : IFragmentNode, IExpressionNode, IConditionalValueNode
{
    /// <summary>
    /// Name of the function, or null if anonymous.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// If true, this function is unnamed (anonymous).
    /// </summary>
    public bool IsAnonymous { get => Name is null; }

    /// <summary>
    /// If true, this function is a constructor function.
    /// </summary>
    public bool IsConstructor { get; }

    /// <summary>
    /// The body of the function.
    /// </summary>
    public BlockNode Body { get; }

    /// <summary>
    /// Mapping of argument index to default value, for a GMLv2 function declarations.
    /// </summary>
    internal Dictionary<int, IExpressionNode> ArgumentDefaultValues { get; set; } = new();

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Variable;
    public ASTFragmentContext FragmentContext { get; }
    public bool SemicolonAfter => false;
    public bool EmptyLineBefore { get; private set; }
    public bool EmptyLineAfter { get; private set; }

    public string ConditionalTypeName => "FunctionDecl";
    public string ConditionalValue => Name;

    public FunctionDeclNode(string name, bool isConstructor, BlockNode body, ASTFragmentContext fragmentContext)
    {
        Name = name;
        IsConstructor = isConstructor;
        Body = body;
        FragmentContext = fragmentContext;
    }

    private void CleanBody(ASTCleaner cleaner)
    {
        Body.Clean(cleaner);
        Body.UseBraces = true;
        if (Body.FragmentContext.BaseParentCall is not null)
        {
            cleaner.PushFragmentContext(Body.FragmentContext);
            Body.FragmentContext.BaseParentCall = Body.FragmentContext.BaseParentCall.Clean(cleaner);
            cleaner.PopFragmentContext();
        }
    }

    private void CleanEmptyLines(ASTCleaner cleaner)
    {
        EmptyLineAfter = EmptyLineBefore = cleaner.Context.Settings.EmptyLineAroundFunctionDeclarations;
    }

    private void CleanDefaultArgumentValues(ASTCleaner cleaner)
    {
        if (!cleaner.Context.Settings.CleanupDefaultArgumentValues)
        {
            return;
        }

        int firstIfIndex = 0;
        int childIndex = 0;
        int lastArgumentIndex = -1;
        while (childIndex < Body.Children.Count)
        {
            // Skip locals, if they exist
            if (Body.Children[childIndex] is BlockLocalVarDeclNode)
            {
                firstIfIndex++;
                childIndex++;
                continue;
            }

            // An if statement is expected
            if (Body.Children[childIndex] is not IfNode ifNode)
            {
                break;
            }

            // Verify the if condition is an == comparison of two simple variables
            if (ifNode.Condition is not BinaryNode 
                    { Instruction: { Kind: IGMInstruction.Opcode.Compare, ComparisonKind: IGMInstruction.ComparisonType.EqualTo },
                      Left: VariableNode argumentVariable, Right: VariableNode undefinedVariable })
            {
                break;
            }

            // Verify the left variable is an argument we have not yet provided a default value for,
            // and is strictly greater than the previous argument index
            int argIndex = argumentVariable.GetArgumentIndex();
            if (argIndex == -1 || ArgumentDefaultValues.ContainsKey(argIndex) || argIndex <= lastArgumentIndex)
            {
                break;
            }

            // Ensure the right variable is simply "undefined"
            if (!undefinedVariable.IsUndefinedVariable())
            {
                break;
            }

            // If statement should not have an else block
            if (ifNode.ElseBlock is not null)
            {
                break;
            }

            // If statement should have a single assignment statement within it
            if (ifNode.TrueBlock is not { Children: [AssignNode assign] })
            {
                break;
            }

            // Assignment's destination should be the same argument variable
            if (assign.Variable is not VariableNode assignDest || assignDest.GetArgumentIndex() != argIndex)
            {
                break;
            }

            // Successfully found a default argument assignment - store expression and move on
            ArgumentDefaultValues[argIndex] = assign.Value;
            lastArgumentIndex = argIndex;
            childIndex++;
        }

        // Remove all if statement nodes we just successfully processed
        Body.Children.RemoveRange(firstIfIndex, childIndex - firstIfIndex);
    }

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        CleanBody(cleaner);
        CleanEmptyLines(cleaner);
        CleanDefaultArgumentValues(cleaner);
        return this;
    }

    IStatementNode IASTNode<IStatementNode>.Clean(ASTCleaner cleaner)
    {
        CleanBody(cleaner);
        CleanEmptyLines(cleaner);
        CleanDefaultArgumentValues(cleaner);
        return this;
    }

    public void Print(ASTPrinter printer)
    {
        if (IsAnonymous)
        {
            printer.Write("function(");
        }
        else
        {
            printer.Write("function ");
            printer.Write(Name);
            printer.Write('(');
        }

        for (int i = 0; i <= Body.FragmentContext.MaxReferencedArgument; i++)
        {
            printer.Write(Body.FragmentContext.GetNamedArgumentName(printer.Context, i));
            if (ArgumentDefaultValues.TryGetValue(i, out IExpressionNode defaultValue))
            {
                printer.Write(" = ");
                printer.PushFragmentContext(Body.FragmentContext);
                defaultValue.Print(printer);
                printer.PopFragmentContext();
            }
            if (i != Body.FragmentContext.MaxReferencedArgument)
            {
                printer.Write(", ");
            }
        }

        printer.Write(')');

        if (Body.FragmentContext.BaseParentCall is not null)
        {
            printer.Write(" : ");
            printer.PushFragmentContext(Body.FragmentContext);
            Body.FragmentContext.BaseParentCall.Print(printer);
            printer.PopFragmentContext();
        }

        if (IsConstructor)
        {
            printer.Write(" constructor");
        }

        if (printer.Context.Settings.OpenBlockBraceOnSameLine)
        {
            printer.Write(' ');
        }
        Body.Print(printer);
    }

    public IExpressionNode ResolveMacroType(ASTCleaner cleaner, IMacroType type)
    {
        if (type is IMacroTypeConditional conditional)
        {
            return conditional.Resolve(cleaner, this);
        }
        return null;
    }
}
