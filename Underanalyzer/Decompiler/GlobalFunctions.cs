﻿/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using System.Threading.Tasks;
using Underanalyzer.Decompiler.ControlFlow;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Decompiler;

/// <summary>
/// Interface that can be used to supply global function information to the decompiler,
/// particularly for GMLv2 and above.
/// </summary>
public interface IGlobalFunctions
{
    /// <summary>
    /// Lookup of function reference to name. Should be the same references that are supplied to the decompiler.
    /// </summary>
    public Dictionary<IGMFunction, string> FunctionToName { get; }

    /// <summary>
    /// Lookup of function name to reference. Should be the same references that are supplied to the decompiler.
    /// </summary>
    public Dictionary<string, IGMFunction> NameToFunction { get; }
}

/// <summary>
/// Provided way to find all global functions in a game, using some components of the decompiler.
/// </summary>
public class GlobalFunctions : IGlobalFunctions
{
    public Dictionary<IGMFunction, string> FunctionToName { get; }

    public Dictionary<string, IGMFunction> NameToFunction { get; }

    /// <summary>
    /// Initializes an empty instance of this class. Useful for pre-GMLv2.
    /// </summary>
    public GlobalFunctions()
    {
        FunctionToName = new();
        NameToFunction = new();
    }

    /// <summary>
    /// Given a list of global scripts, initializes this class with all global function information.
    /// Optionally, <see cref="ParallelOptions"/> can be passed in to configure parallelization.
    /// By default, the default settings are used (which has no limits).
    /// </summary>
    public GlobalFunctions(IEnumerable<IGMCode> globalScripts, ParallelOptions parallelOptions = null)
    {
        Dictionary<IGMFunction, string> functionToName = new();
        Dictionary<string, IGMFunction> nameToFunction = new();
        object _lock = new();

        Parallel.ForEach(globalScripts, parallelOptions ?? new(), script =>
        {
            // Find all fragments in the code entry
            List<Block> blocks = Block.FindBlocks(script, out _);
            List<Fragment> fragments = Fragment.FindFragments(script, blocks);

            // Find names of functions after each fragment
            for (int i = 1; i < fragments.Count; i++)
            {
                Fragment fragment = fragments[i];
                if (fragment.Successors.Count == 0)
                {
                    // If no successors, assume code is corrupt and don't consider it
                    continue;
                }
                Block after = fragment.Successors[0] as Block;
                if (after is null)
                {
                    // If block after isn't a block, assume code is corrupt as well
                    continue;
                }

                string name = GetFunctionNameAfterFragment(after, out IGMFunction function);
                if (name is not null)
                {
                    lock (_lock)
                    {
                        functionToName[function] = name;
                        nameToFunction[name] = function;
                    }
                }
            }
        });

        FunctionToName = functionToName;
        NameToFunction = nameToFunction;
    }

    /// <summary>
    /// Gets the name of a global function based on the instructions after a code fragment.
    /// Returns null if there is none, or the code is corrupt.
    /// </summary>
    private string GetFunctionNameAfterFragment(Block block, out IGMFunction foundFunction)
    {
        foundFunction = null;

        // Ensure enough instructions exist
        if (block.Instructions.Count < 3)
        {
            return null;
        }

        // Get function reference for fragment
        if (block.Instructions[0] is not { Kind: Opcode.Push, Type1: DataType.Int32, Function: IGMFunction function } || function is null)
        {
            return null;
        }
        foundFunction = function;

        // Ensure conv instruction exists
        if (block.Instructions[1] is not { Kind: Opcode.Convert, Type1: DataType.Int32, Type2: DataType.Variable })
        {
            return null;
        }

        switch (block.Instructions[2].Kind)
        {
            case Opcode.PushImmediate:
                {
                    // Normal function. Skip past basic instructions.
                    if (block.Instructions is not
                        [
                            _, _,
                        { ValueShort: -1 or -16 },
                        { Kind: Opcode.Convert, Type1: DataType.Int32, Type2: DataType.Variable },
                        { Kind: Opcode.Call, Function.Name.Content: VMConstants.MethodFunction },
                            ..
                        ])
                    {
                        // Failed to match instructions
                        return null;
                    }

                    // Check if we have a name
                    if (block   .Instructions is
                        [
                            _, _, _, _, _,
                        { Kind: Opcode.Duplicate, DuplicationSize2: 0 },
                        { Kind: Opcode.PushImmediate },
                        { Kind: Opcode.Pop, Variable.Name.Content: string funcName },
                            ..
                        ])
                    {
                        // We have a name!
                        return funcName;
                    }
                    break;
                }
            case Opcode.Call:
                {
                    // This is a struct or constructor function
                    if (block.Instructions is not
                        [
                            _, _,
                        { Kind: Opcode.Call, Function.Name.Content: VMConstants.NullObjectFunction },
                        { Kind: Opcode.Call, Function.Name.Content: VMConstants.MethodFunction },
                            ..
                        ])
                    {
                        // Failed to match instructions
                        return null;
                    }

                    // Check if we're a struct or function constructor (named)
                    if (block.Instructions is
                        [
                            _, _, _, _,
                        { Kind: Opcode.Duplicate, DuplicationSize2: 0 },
                        { Kind: Opcode.PushImmediate, ValueShort: short pushVal },
                        { Kind: Opcode.Pop, Variable.Name.Content: string funcName },
                            ..
                        ])
                    {
                        // Check if struct or constructor
                        if (pushVal != -16 && pushVal != -5)
                        {
                            // We're a constructor!
                            return funcName;
                        }
                    }
                    break;
                }
        }

        return null;
    }
}
