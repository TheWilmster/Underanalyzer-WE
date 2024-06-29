﻿using System.Collections.Generic;
using Underanalyzer.Decompiler.ControlFlow;

namespace Underanalyzer.Decompiler.AST;

/// <summary>
/// Represents a single fragment context within the AST.
/// </summary>
public class ASTFragmentContext
{
    /// <summary>
    /// The fragment we belong to.
    /// </summary>
    private Fragment Fragment { get; }

    /// <summary>
    /// The name of the code entry this fragment belongs to.
    /// </summary>
    public string CodeEntryName { get => Fragment.CodeEntry.Name?.Content; }

    /// <summary>
    /// The name of the function this fragment belongs to, or null if none.
    /// </summary>
    public string FunctionName { get; internal set; } = null;

    /// <summary>
    /// Children of this fragment, e.g. sub-functions.
    /// </summary>
    internal List<ASTFragmentContext> Children { get; } = new();
    
    /// <summary>
    /// Current working VM expression stack.
    /// </summary>
    internal Stack<IExpressionNode> ExpressionStack { get; } = new();

    /// <summary>
    /// If true, the current AST is within a static variable initialization block (when printing).
    /// </summary>
    internal bool InStaticInitialization { get; set; } = false;

    /// <summary>
    /// If not null, represents the list of arguments getting passed into this fragment (which is a struct).
    /// </summary>
    public List<IExpressionNode> StructArguments { get; internal set; } = null;

    /// <summary>
    /// Function call to the parent constructor function, if this is a constructor function that inherits
    /// another constructor function, or null otherwise.
    /// </summary>
    internal IExpressionNode BaseParentCall { get; set; } = null;

    /// <summary>
    /// Contains all local variables referenced from within this fragment.
    /// </summary>
    public HashSet<string> LocalVariableNames { get; } = new();

    /// <summary>
    /// Contains all local variables referenced from within this fragment, in order of occurrence.
    /// </summary>
    public List<string> LocalVariableNamesList { get; } = new();

    /// <summary>
    /// Map of code entry names to function names, for all children fragments/sub-functions of this context.
    /// </summary>
    public Dictionary<string, string> SubFunctionNames { get; } = new();

    /// <summary>
    /// The loop surrounding the currently-building position in the AST.
    /// </summary>
    internal Loop SurroundingLoop { get; set; } = null;

    /// <summary>
    /// Contains local variable names that should be entirely removed from the fragment. 
    /// (For removing compiler-generated code.)
    /// </summary>
    internal HashSet<string> LocalVariablesToPurge { get; } = new();

    /// <summary>
    /// Stack of the number of statements contained in all enveloping try finally blocks.
    /// </summary>
    internal Stack<int> FinallyStatementCount { get; set; } = new();

    /// <summary>
    /// The maximum argument variable referenced within this context, if applicable. -1 means none are referenced.
    /// </summary>
    internal int MaxReferencedArgument { get; set; } = -1;

    /// <summary>
    /// Contains all named argument variables referenced from within this fragment.
    /// </summary>
    internal HashSet<string> NamedArguments { get; set; } = new();

    /// <summary>
    /// Lookup of argument index to argument name, for GMLv2 named arguments.
    /// </summary>
    private Dictionary<int, string> NamedArgumentByIndex { get; set; } = new();

    internal ASTFragmentContext(Fragment fragment)
    {
        Fragment = fragment;

        // Update max referenced argument, if we have an argument count greater than 0
        if (fragment.CodeEntry.ArgumentCount > 0)
        {
            MaxReferencedArgument = fragment.CodeEntry.ArgumentCount - 1;
        }
    }

    /// <summary>
    /// Removes a local variable's declaration from this fragment.
    /// </summary>
    internal void RemoveLocal(string name)
    {
        if (LocalVariableNames.Contains(name))
        {
            LocalVariableNames.Remove(name);
            LocalVariableNamesList.Remove(name);
        }
    }

    /// <summary>
    /// Generates and returns the named argument name that the given index should have.
    /// By default, resorts to formatting string from settings.
    /// Returns null if prior to GMLv2 (and no named argument should be used).
    /// </summary>
    internal string GetNamedArgumentName(DecompileContext context, int index)
    {
        // GMLv2 introduced named arguments
        if (!context.GMLv2)
        {
            return null;
        }

        // Look up existing name, and use that, if it exists already
        if (NamedArgumentByIndex.TryGetValue(index, out string existingName))
        {
            return existingName;
        }

        // Resolve name from registry
        string name = context.GameContext.GameSpecificRegistry.NamedArgumentResolver.ResolveArgument(CodeEntryName, index);

        // If no name exists in the registry, auto-generate one from settings
        name ??= string.Format(context.Settings.UnknownArgumentNamePattern, index);

        // Resolve conflicts with local variable names
        while (LocalVariableNames.Contains(name) || NamedArguments.Contains(name))
        {
            name += "_";
        }

        // Add new named argument, and return it
        NamedArguments.Add(name);
        NamedArgumentByIndex[index] = name;
        return name;
    }
}
