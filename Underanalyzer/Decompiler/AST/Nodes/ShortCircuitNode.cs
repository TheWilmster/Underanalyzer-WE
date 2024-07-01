﻿/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.ControlFlow;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents short circuit && and || in the AST.
/// </summary>
public class ShortCircuitNode : IMultiExpressionNode
{
    /// <summary>
    /// List of conditions in this short circuit chain.
    /// </summary>
    public List<IExpressionNode> Conditions { get; private set; }

    /// <summary>
    /// Type of logic (And or Or) being used.
    /// </summary>
    public ShortCircuitType LogicType { get; }

    public bool Duplicated { get; set; } = false;
    public bool Group { get; set; } = false;
    public IGMInstruction.DataType StackType { get; set; } = IGMInstruction.DataType.Boolean;

    public ShortCircuitNode(List<IExpressionNode> conditions, ShortCircuitType logicType)
    {
        Conditions = conditions;
        LogicType = logicType;
    }

    public IExpressionNode Clean(ASTCleaner cleaner)
    {
        for (int i = 0; i < Conditions.Count; i++)
        {
            Conditions[i] = Conditions[i].Clean(cleaner);

            // Group inner short circuits, so they don't clash order of operations
            if (Conditions[i] is ShortCircuitNode sc)
            {
                sc.Group = true;
            }
        }

        return this;
    }

    public void Print(ASTPrinter printer)
    {
        if (Group)
        {
            printer.Write('(');
        }

        string op = (LogicType == ShortCircuitType.And) ? " && " : " || ";
        for (int i = 0; i < Conditions.Count; i++)
        {
            Conditions[i].Print(printer);
            if (i != Conditions.Count - 1)
            {
                printer.Write(op);
            }
        }

        if (Group)
        {
            printer.Write(')');
        }
    }

    public bool RequiresMultipleLines(ASTPrinter printer)
    {
        foreach (IExpressionNode condition in Conditions)
        {
            if (condition.RequiresMultipleLines(printer))
            {
                return true;
            }
        }
        return false;
    }
}
