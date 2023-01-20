using System;
using System.Collections.Generic;
using System.Linq;

namespace NFA_Convertor.Classes;
public static class MachineConvertor
{
    private static List<Node> LClosure(params Node[] nodes)
    {
        var list = new List<Node>();
        var seenTransitions = new List<Transition>();

        for (var i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            LClosureAction(seenTransitions, list, node);
        }
        list = list.Distinct() /*remove duplicates*/.OrderBy(o => o.Index).ToList(); /*sort list*/
        return list;
    }

    private static void LClosureAction(List<Transition> seen, List<Node> resultList, Node input)
    {
        resultList.Add(input);
        foreach (var transition in input.Transitions)
        {
            if (seen.Contains(transition)) continue;
            if (!transition.Letters.Contains(Machine.Lambda)) continue;

            seen.Add(transition);
            LClosureAction(seen, resultList, transition.To);
        }
    }

    private static List<Node> Move(string str, List<Node> nodes)
    {
        var list = new List<Node>();
        foreach (var node in nodes)
        {
            foreach (var transition in node.Transitions)
            {
                if (transition.From.Equals(node) && transition.Letters.Contains(str))
                {
                    list.Add(transition.To);
                }
            }
        }

        return list;
    }

    public static Machine ConvertNfa(Machine machine)
    {
        var newMachine = new Machine();
        var alphabet = newMachine.Alphabet = machine.Alphabet;
            
        var newNodes = newMachine.Nodes;
            
        var dTrans = new List<Node>[alphabet.Count];
        for (var i = 0; i < dTrans.Length; i++)
        {
            dTrans[i] = new List<Node>();
        }
            
        var collection = LClosure(machine.StartNode);
            
        var node = newMachine.AddNode(collection, true, CheckFinal(collection));
        newMachine.StartNode = node;

        for (var i = 0; i < newNodes.Count; i++)
        {
            for (var j = 0; j < alphabet.Count; j++)
            {
                collection = LClosure(Move(alphabet[j], newNodes[i].SubNodes).ToArray());

                var set = new HashSet<Node>(collection);
                //check if collection is in dTrans
                var exists = false;
                foreach (var t in newNodes)
                {
                    if (set.SetEquals(t.SubNodes))
                    {
                        dTrans[j].Add(t);
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {
                    node = newMachine.AddNode(collection, false, CheckFinal(collection));
                    dTrans[j].Add(node);
                }


            }
        }

        foreach (var from in newNodes)
        {
            for (var j = 0; j < alphabet.Count; j++)
            {
                var to = dTrans[j][from.Index];

                newMachine.AddTransition(from, to, alphabet[j]);
            }
        }

        return newMachine;
    }

    public static string ConvertMachineAsText(Machine machine,bool isDfa)
    {
        var result = !isDfa ? ConvertNfa(machine) : DfaOptimization(machine);
        string InputGen()
        {
            var alphaTxt = "Alphabet: {" + string.Join(", ", machine.Alphabet) + "}";

            var nodesTxt = "Nodes: {";
            var lastNodeIndex = machine.Nodes.Count - 1;
            for (var i = 0; i <= lastNodeIndex; i++)
            {
                nodesTxt += "i" + machine.Nodes[i].Index;
                if (i == lastNodeIndex) nodesTxt += "}";
                else nodesTxt += ", ";
            }

            var startNodeTxt = "Start Node: i" + machine.StartNode.Index;

            var acceptNodesTxt = "Accept Nodes: {";
            for (var i = 0; i <= lastNodeIndex; i++)
            {
                var node = machine.Nodes[i];

                if (node.IsFinal)
                {
                    acceptNodesTxt += "i" + node.Index;

                    if (i != lastNodeIndex) acceptNodesTxt += ", ";
                }

                if (i == lastNodeIndex) acceptNodesTxt += "}";
            }

            var transitionsTxt = "Transitions:\n";
            foreach (var node in machine.Nodes)
            {
                foreach (var transition in node.Transitions)
                {
                    if (transition.From.Equals(node))
                    {
                        foreach (var letter in transition.Letters)
                        {
                            transitionsTxt += " δ(i" + node.Index + ", " + letter + ") = i" +
                                              transition.To.Index + "\n";
                        }
                    }
                }
            }
            return $"[INPUT]\n\n{alphaTxt}\n\n{nodesTxt}\n\n{startNodeTxt}\n\n{acceptNodesTxt}\n\n{transitionsTxt}";
        }

        string OutputGen()
        {
            var alphaTxt = "Alphabet: {" + string.Join(", ", machine.Alphabet) + "}";

            var nodesTxt = "Nodes: {";
            var lastNodeIndex = result.Nodes.Count - 1;
            for (var i = 0; i <= lastNodeIndex; i++)
            {
                nodesTxt += "o" + result.Nodes[i].Index;
                if (i == lastNodeIndex) nodesTxt += "}";
                else nodesTxt += ", ";
            }

            var subNodesTxt = "";
            foreach (var node in result.Nodes)
            {
                subNodesTxt += "o" + node.Index + ": {";

                var subNodes = node.SubNodes;
                if (subNodes.Count == 0)
                {
                    subNodesTxt += "}\n";
                    continue;
                }

                var lastSubNodeIndex = subNodes.Count - 1;
                for (var i = 0; i <= lastSubNodeIndex; i++)
                {
                    subNodesTxt += "i" + subNodes[i].Index;
                    if (i == lastSubNodeIndex) subNodesTxt += "}\n";
                    else subNodesTxt += ", ";
                }
            }

            var startNodeTxt = "Start Node: o" + result.StartNode.Index;

            var acceptNodesTxt = "Accept Nodes: {";
            for (var i = 0; i <= lastNodeIndex; i++)
            {
                var node = result.Nodes[i];
                if (node.IsFinal)
                {
                    acceptNodesTxt += "o" + node.Index;

                    if (i != lastNodeIndex) acceptNodesTxt += ", ";
                }

                if (i == lastNodeIndex) acceptNodesTxt += "}";
            }

            var transitionsTxt = "Transitions:\n";
            foreach (var node in result.Nodes)
            {
                foreach (var transition in node.Transitions)
                {
                    if (transition.From.Equals(node))
                    {
                        foreach (var letter in transition.Letters)
                        {
                            transitionsTxt += " δ(o" + node.Index + ", " + letter + ") = o" +
                                              transition.To.Index + "\n";
                        }
                    }
                }
            }

            return $"[OUTPUT]\n\n{alphaTxt}\n\n{nodesTxt}\n\n{subNodesTxt}\n{startNodeTxt}\n\n{acceptNodesTxt}\n\n{transitionsTxt}";
        }


        return $"{InputGen()}\n\n{OutputGen()}";
    }

    private static bool CheckStarter(List<Node> nodes)
    {
        return nodes.Any(node => node.IsStarter);
    }
    
    private static bool CheckFinal(List<Node> nodes)
    {
        return nodes.Any(node => node.IsFinal);
    }

    public static Machine LoadFromStruct(MachineStruct mStruct)
    {
        var machine = new Machine();

        machine.Alphabet.AddRange(mStruct.Alphabet);

        //nodes

        var nodes = machine.Nodes = new List<Node>();

        foreach (var nodeStruct in mStruct.Nodes)
        {
            var node = new Node()
            {
                Index = nodeStruct.Index, IsFinal = nodeStruct.IsFinal, IsStarter = nodeStruct.IsStarter,
                HasSubNodes = nodeStruct.HasSubNodes
            };

            if (node.HasSubNodes)
            {
                foreach (var index in nodeStruct.SubNodesIndexes)
                {
                    node.SubNodes.Add(new Node() {Index = index});
                }
            }

            nodes.Add(node);
        }

        //transitions
        foreach (var nodeStruct in mStruct.Nodes)
        {
            var node = nodes.FirstOrDefault(node1 => node1.Index == nodeStruct.Index);
            foreach (var transitionStruct in nodeStruct.Transitions)
            {
                var from = nodes.FirstOrDefault(node1 => node1.Index == transitionStruct.FromIndex);
                var to = nodes.FirstOrDefault(node1 => node1.Index == transitionStruct.ToIndex);
                var transition = new Transition() {From = from, To = to};
                transition.Letters.AddRange(transitionStruct.Letters);
                node?.Transitions.Add(transition);
            }
        }

        machine.StartNode = mStruct.StartNodeIndex == -1? null : nodes.FirstOrDefault(node => node.Index == mStruct.StartNodeIndex);
        Console.Write("");
        return machine;
    }

    public static MachineStruct SaveToStruct(Machine machine)
    {
        var machineStruct = new MachineStruct
        {
            Alphabet = machine.Alphabet.ToArray()
        };

        var nodeStructs = new List<NodeStruct>();
        foreach (var node in machine.Nodes)
        {
            var nodeStruct = new NodeStruct()
            {
                Index = node.Index, IsFinal = node.IsFinal, IsStarter = node.IsStarter,
                HasSubNodes = node.HasSubNodes
            };

            if (node.HasSubNodes)
            {
                var subNodesIndexes = new List<int>();

                foreach (var subNode in node.SubNodes)
                {
                    subNodesIndexes.Add(subNode.Index);
                }

                nodeStruct.SubNodesIndexes = subNodesIndexes.ToArray();
            }

            var transitionsStructs = new List<TransitionStruct>();

            foreach (var transition in node.Transitions)
            {
                var transitionStruct = new TransitionStruct()
                {
                    FromIndex = transition.From.Index, ToIndex = transition.To.Index,
                    Letters = transition.Letters.ToArray()
                };
                transitionsStructs.Add(transitionStruct);
            }

            nodeStruct.Transitions = transitionsStructs.ToArray();
            nodeStructs.Add(nodeStruct);
        }

        machineStruct.Nodes = nodeStructs.ToArray();

        machineStruct.StartNodeIndex = machine.StartNode?.Index ?? -1;

        return machineStruct;

    }

    
    //DFA Optimization

    public static Machine DfaOptimization(Machine machine)
    {
        var newMachine = new Machine(){Alphabet = machine.Alphabet};
        
        var availableNodes = new List<Node>();//to store reachable Nodes
        
        void SetReachableNodes(Node node)
        {
            if (availableNodes.Contains(node)) return;
            availableNodes.Add(node);
            foreach (var transition in node.Transitions.Where(transition => Equals(transition.From, node))) 
                SetReachableNodes(transition.To);

        }
        SetReachableNodes(machine.StartNode);
        
        var lastIterationSets = new List<HashSet<Node>>();//stores sets for previous iteration
        var nonFinals = new HashSet<Node>(); 
        var finals = new HashSet<Node>();
        lastIterationSets.Add(nonFinals); 
        lastIterationSets.Add(finals);
        foreach (var node in availableNodes)
        {
            if (node.IsFinal)finals.Add(node);
            else nonFinals.Add(node);
        }
        
        
        
        while (true)
        {
            var sets = new List<HashSet<Node>>(); //stores sets for current iteration
            foreach (var set in lastIterationSets)
            {
                if (set.Count < 2)
                {
                    sets.Add(set);
                    continue;
                }
                
                NewSet(set.ToArray()[0], sets);

                var i = 0;
                var j = 1;
                while (j < set.Count)
                {
                    if (IsEquivalent(set.ToArray()[i], set.ToArray()[j]))
                    {
                        GetSet(set.ToArray()[i],sets).Add(set.ToArray()[j]);
                    }
                    else if (j-1>i)
                    {
                        i++;
                        continue;
                    }
                    else if (j-1 == i)
                    {
                        NewSet(set.ToArray()[j],sets);
                    }

                    j++;
                    i = 0;
                }
            }

            var isIterationsEqual = true;
            foreach (var set in sets)
            {
                isIterationsEqual = CheckEquality(set);

                if (!isIterationsEqual) break;
            }

            if (isIterationsEqual) break;
            lastIterationSets = sets;
        }

        bool CheckEquality(HashSet<Node> set)
        {
            return lastIterationSets.Any(set.SetEquals);
        }
        
        
        //creating new nodes

        foreach (var set in lastIterationSets)
        {
            var node = newMachine.AddNode(set.ToList(), CheckStarter(set.ToList()), CheckFinal(set.ToList()));
            if (node.IsStarter) newMachine.StartNode = node;
        }
        
        //creating new transitions

        foreach (var node in newMachine.Nodes)
        {
            foreach (var alphabet in newMachine.Alphabet)
            {
                foreach (var subNode in node.SubNodes)
                {
                    var toSubNode = machine.SearchTransition(subNode, alphabet)?.To;
                    foreach (var toNode in newMachine.Nodes)
                    {
                        if (toNode.SubNodes.Contains(toSubNode))
                        {
                            newMachine.AddTransition(node, toNode, alphabet);
                            break;
                        }
                    }
                }
            }
        }
        //-------------------------------------------------------------------------------------------------------

        HashSet<Node> GetSet(Node node, List<HashSet<Node>> sets)
        {
            return sets.First(set => set.Contains(node));
        }

        bool IsEquivalent(Node node1, Node node2)
        {
            foreach (var alphabet in machine.Alphabet)
            {
                var set1 = lastIterationSets.FirstOrDefault(set => set.Contains(machine.SearchTransition(node1, alphabet)?.To));
                var set2 = lastIterationSets.FirstOrDefault(set => set.Contains(machine.SearchTransition(node2, alphabet)?.To));       
                if (set1 != null && !set1.Equals(set2)) return false;
            }

            return true;
        }

        void NewSet(Node node, List<HashSet<Node>> sets)
        {
            foreach (var set in sets)
            {
                if (set.Contains(node))
                {
                    return;
                }
            }

            var newSet = new HashSet<Node>();
            newSet.Add(node);
            sets.Add(newSet);
        }

        return newMachine;
    }
}

[Serializable]
public struct MachineStruct
{
    public string[] Alphabet;
    public NodeStruct[] Nodes;
    public int StartNodeIndex;
}

[Serializable]
public struct NodeStruct
{
    public int Index;
    public TransitionStruct[] Transitions;
    public bool IsFinal, IsStarter, HasSubNodes;
    public int[] SubNodesIndexes;
}

[Serializable]
public struct TransitionStruct
{
    public int FromIndex, ToIndex;
    public string[] Letters;
}