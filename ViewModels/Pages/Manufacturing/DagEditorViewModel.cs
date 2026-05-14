using System.Windows;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using uniffi.stump;
using System.Linq;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.Input;

namespace Poplar.ViewModels.Pages.Manufacturing;

public partial class DagEditorViewModel : ObservableObject
{
    private StepMetadata[] _stepMetadata = Array.Empty<StepMetadata>();
    
    [ObservableProperty] private ObservableCollection<FlowNodeViewModel> _nodes = new();
    [ObservableProperty] private ObservableCollection<FlowConnectionViewModel> _connections = new();
    [ObservableProperty] private ObservableCollection<StepMetadata> _stepPalette = new();
    
    [ObservableProperty] private FlowNodeViewModel? _selectedNode;
    [ObservableProperty] private FlowConnectionViewModel? _selectedConnection;

    public void Initialize(StepMetadata[] metadata)
    {
        _stepMetadata = metadata;
        StepPalette.Clear();
        foreach (var m in metadata) StepPalette.Add(m);
    }

    public void LoadFromDagFlow(DagFlow? dag)
    {
        Nodes.Clear();
        Connections.Clear();
        SelectedNode = null;
        SelectedConnection = null;

        if (dag == null || dag.nodes == null || dag.nodes.Length == 0)
        {
            return;
        }

        // 1. Create Nodes
        var nodeDict = new Dictionary<string, FlowNodeViewModel>();
        foreach (var record in dag.nodes)
        {
            var meta = _stepMetadata.FirstOrDefault(m => m.stepType == record.stepType);
            var vm = new FlowNodeViewModel(record, meta, record.id == dag.entryNode);
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

    [RelayCommand]
    private void AutoLayout()
    {
        var entryNodeId = Nodes.FirstOrDefault(n => n.IsEntry)?.Id ?? Nodes.FirstOrDefault()?.Id;
        if (entryNodeId != null) ApplyAutoLayout(entryNodeId);
    }

    private void ApplyAutoLayout(string entryNodeId)
    {
        if (Nodes.Count == 0) return;

        var nodeDict = Nodes.ToDictionary(n => n.Id);
        FlowNodeViewModel? startNode = nodeDict.TryGetValue(entryNodeId, out var found) ? found : Nodes.First();

        var layers = new Dictionary<int, List<FlowNodeViewModel>>();
        var visited = new HashSet<string>();
        var queue = new Queue<(FlowNodeViewModel node, int layer)>();

        queue.Enqueue((startNode, 0));
        visited.Add(startNode.Id);

        while (queue.Count > 0)
        {
            var (current, layer) = queue.Dequeue();
            if (!layers.ContainsKey(layer)) layers[layer] = new List<FlowNodeViewModel>();
            layers[layer].Add(current);

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

        var disconnected = Nodes.Where(n => !visited.Contains(n.Id)).ToList();
        if (disconnected.Count > 0)
        {
            if (!layers.ContainsKey(0)) layers[0] = new List<FlowNodeViewModel>();
            layers[0].AddRange(disconnected);
        }

        double startX = 50, startY = 50, xSpacing = 300, ySpacing = 150;
        foreach (var kvp in layers.OrderBy(l => l.Key))
        {
            double currentX = startX + (kvp.Key * xSpacing);
            double currentY = startY;
            foreach (var node in kvp.Value)
            {
                node.Location = new Point(currentX, currentY);
                currentY += ySpacing;
            }
        }
    }

    [RelayCommand]
    private void AddNode(StepMetadata metadata)
    {
        var id = $"node_{DateTime.Now.Ticks}";
        // Create a dummy record to start with
        var record = new DagNode(id, metadata.stepType, new Dictionary<string, GenericValue>(), new Dictionary<string, string>(), new DataMapping[0], new DataMapping[0], new DagEdge[0], null);
        var vm = new FlowNodeViewModel(record, metadata);
        vm.Location = new Point(100, 100);
        Nodes.Add(vm);
        SelectedNode = vm;
    }

    [RelayCommand]
    private void DeleteSelected()
    {
        if (SelectedNode != null)
        {
            var toRemove = Connections.Where(c => c.Source == SelectedNode || c.Target == SelectedNode).ToList();
            foreach (var c in toRemove) Connections.Remove(c);
            Nodes.Remove(SelectedNode);
            SelectedNode = null;
        }
        else if (SelectedConnection != null)
        {
            Connections.Remove(SelectedConnection);
            SelectedConnection = null;
        }
    }

    public DagFlow? ExportToDagFlow(int originalDagId, string dagName)
    {
        if (Nodes.Count == 0) return null;

        var dagNodes = new List<DagNode>();
        var entryNodeId = Nodes.FirstOrDefault(n => n.IsEntry)?.Id ?? Nodes.First().Id;

        foreach (var nodeVm in Nodes)
        {
            var nodeRecord = nodeVm.ToRecord();
            var outgoingConnections = Connections.Where(c => c.Source?.Id == nodeVm.Id).ToList();
            var edges = outgoingConnections.Select(c => new DagEdge(c.Target!.Id, c.Condition)).ToArray();
            
            // Create updated record with edges
            var updatedNode = new DagNode(
                nodeRecord.id,
                nodeRecord.stepType,
                nodeRecord.staticParams,
                nodeRecord.inputMappings,
                nodeRecord.outputToContext,
                nodeRecord.outputToTrace,
                edges,
                nodeRecord.retryPolicy
            );
            dagNodes.Add(updatedNode);
        }

        return new DagFlow(originalDagId, dagName, entryNodeId, dagNodes.ToArray());
    }
}
