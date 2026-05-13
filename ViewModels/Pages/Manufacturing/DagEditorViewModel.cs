using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;
using System.Linq;
using System.Collections.Generic;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DagEditorViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<FlowNodeViewModel> _nodes = new();

    [ObservableProperty]
    private ObservableCollection<FlowConnectionViewModel> _connections = new();

    public void LoadFromDagFlow(DagFlow? dag)
    {
        Nodes.Clear();
        Connections.Clear();

        if (dag == null || dag.nodes == null || dag.nodes.Length == 0)
        {
            return;
        }

        // 1. Create Nodes
        var nodeDict = new Dictionary<string, FlowNodeViewModel>();
        foreach (var record in dag.nodes)
        {
            var vm = new FlowNodeViewModel(record);
            Nodes.Add(vm);
            nodeDict[record.id] = vm;
        }

        // 2. Create Connections
        foreach (var record in dag.nodes)
        {
            if (record.edges == null || !nodeDict.TryGetValue(record.id, out var sourceVm)) continue;

            foreach (var edge in record.edges)
            {
                if (nodeDict.TryGetValue(edge.targetNodeId, out var targetVm))
                {
                    Connections.Add(new FlowConnectionViewModel(sourceVm, targetVm, edge.condition));
                }
            }
        }

        // 3. Auto-Layout
        ApplyAutoLayout(dag.entryNode);
    }

    private void ApplyAutoLayout(string entryNodeId)
    {
        if (Nodes.Count == 0) return;

        var nodeDict = Nodes.ToDictionary(n => n.Id);
        
        // Find entry node or just pick the first one
        FlowNodeViewModel? startNode = null;
        if (!string.IsNullOrEmpty(entryNodeId) && nodeDict.TryGetValue(entryNodeId, out var found))
        {
            startNode = found;
        }
        else
        {
            startNode = Nodes.First();
        }

        // BFS to assign layers
        var layers = new Dictionary<int, List<FlowNodeViewModel>>();
        var visited = new HashSet<string>();
        var queue = new Queue<(FlowNodeViewModel node, int layer)>();

        queue.Enqueue((startNode, 0));
        visited.Add(startNode.Id);

        while (queue.Count > 0)
        {
            var (current, layer) = queue.Dequeue();

            if (!layers.ContainsKey(layer))
            {
                layers[layer] = new List<FlowNodeViewModel>();
            }
            layers[layer].Add(current);

            // Find outgoing edges
            var outgoingEdges = Connections.Where(c => c.Source?.Id == current.Id).ToList();
            foreach (var edge in outgoingEdges)
            {
                if (edge.Target != null && !visited.Contains(edge.Target.Id))
                {
                    visited.Add(edge.Target.Id);
                    queue.Enqueue((edge.Target, layer + 1));
                }
            }
        }

        // For any disconnected nodes, put them in layer 0 at the bottom
        var disconnected = Nodes.Where(n => !visited.Contains(n.Id)).ToList();
        if (disconnected.Count > 0)
        {
            if (!layers.ContainsKey(0)) layers[0] = new List<FlowNodeViewModel>();
            layers[0].AddRange(disconnected);
        }

        // Apply coordinates based on layers
        double startX = 100;
        double startY = 100;
        double xSpacing = 350;
        double ySpacing = 200;

        foreach (var kvp in layers.OrderBy(l => l.Key))
        {
            int layerIndex = kvp.Key;
            var nodesInLayer = kvp.Value;

            double currentX = startX + (layerIndex * xSpacing);
            double currentY = startY;

            // Center vertically if we want, but simple stacking is fine
            foreach (var node in nodesInLayer)
            {
                node.Location = new Point(currentX, currentY);
                currentY += ySpacing;
            }
        }
    }

    public DagFlow? ExportToDagFlow(int originalDagId, string dagName, string entryNode)
    {
        if (Nodes.Count == 0) return null;

        var dagNodes = new List<DagNode>();

        foreach (var nodeVm in Nodes)
        {
            // Reconstruct the node, keeping original params/mappings but updating edges
            var outgoingConnections = Connections.Where(c => c.Source?.Id == nodeVm.Id).ToList();
            var newEdges = new List<DagEdge>();

            foreach (var conn in outgoingConnections)
            {
                if (conn.Target != null)
                {
                    newEdges.Add(new DagEdge(conn.Target.Id, conn.Condition));
                }
            }

            var record = nodeVm.OriginalRecord;
            var updatedNode = new DagNode(
                record.id,
                record.stepType,
                record.staticParams,
                record.inputMappings,
                record.outputToContext,
                record.outputToTrace,
                newEdges.ToArray(),
                record.retryPolicy
            );

            dagNodes.Add(updatedNode);
        }

        // If the entry node no longer exists, default to the first node
        var actualEntryNode = dagNodes.Any(n => n.id == entryNode) ? entryNode : dagNodes.First().id;

        return new DagFlow(originalDagId, dagName, actualEntryNode, dagNodes.ToArray());
    }
}
