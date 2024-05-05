﻿using System.Collections.Generic;

namespace Underanalyzer.Decompiler.ControlFlow;

/// <summary>
/// Represents a continue statement in the control flow graph.
/// </summary>
internal class ContinueNode(int address) : IControlFlowNode
{
    public int StartAddress { get; set; } = address;

    public int EndAddress { get; set; } = address;

    public List<IControlFlowNode> Predecessors { get; } = new();

    public List<IControlFlowNode> Successors { get; } = new();

    public IControlFlowNode Parent { get; set; } = null;

    public List<IControlFlowNode> Children { get; } = new();

    public bool Unreachable { get; set; } = false;

    public override string ToString()
    {
        return $"{nameof(ContinueNode)} (address {StartAddress}, {Predecessors.Count} predecessors, {Successors.Count} successors)";
    }
}