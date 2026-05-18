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

    private object? _selectedItem;
    public object? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (_selectedItem is FlowConnectionViewModel oldConn)
            {
                oldConn.IsSelected = false;
            }

            SetProperty(ref _selectedItem, value);
            SelectedNode = value as FlowNodeViewModel;
            SelectedConnection = value as FlowConnectionViewModel;

            if (SelectedConnection != null)
            {
                SelectedConnection.IsSelected = true;
            }
        }
    }

    // For manual connections
    [ObservableProperty] private Point _pendingConnectionSource;
    [ObservableProperty] private bool _isConnecting;

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
        if (SelectedItem is FlowNodeViewModel node)
        {
            var toRemove = Connections.Where(c => c.Source == node || c.Target == node).ToList();
            foreach (var c in toRemove) Connections.Remove(c);
            Nodes.Remove(node);
            SelectedItem = null;
        }
        else if (SelectedItem is FlowConnectionViewModel conn)
        {
            Connections.Remove(conn);
            SelectedItem = null;
        }
    }

    [RelayCommand]
    private void DisconnectConnector(FlowNodeViewModel node)
    {
        var toRemove = Connections.Where(c => c.Source == node || c.Target == node).ToList();
        foreach (var c in toRemove) Connections.Remove(c);
    }

    [RelayCommand]
    private void CreateConnection(object? parameter)
    {
        string logPath = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "debug_log.txt");
        try
        {
            string msg = $"[CreateConnection] Called at {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}. Parameter type: {parameter?.GetType()?.FullName ?? "null"}";
            System.Diagnostics.Debug.WriteLine(msg);
            System.IO.File.AppendAllText(logPath, msg + "\r\n");
            
            if (parameter == null)
            {
                System.Diagnostics.Debug.WriteLine("[CreateConnection] Parameter is null");
                System.IO.File.AppendAllText(logPath, "[CreateConnection] Parameter is null\r\n");
                return;
            }
            
            object? sourceObj = null;
            object? targetObj = null;

            // Nodify passes a ValueTuple containing (Source, Target)
            if (parameter is System.Runtime.CompilerServices.ITuple tuple && tuple.Length >= 2)
            {
                sourceObj = tuple[0];
                targetObj = tuple[1];
                string parseMsg = $"[CreateConnection] Parsed as ITuple. Length: {tuple.Length}, Type0: {sourceObj?.GetType()?.FullName}, Type1: {targetObj?.GetType()?.FullName}";
                System.Diagnostics.Debug.WriteLine(parseMsg);
                System.IO.File.AppendAllText(logPath, parseMsg + "\r\n");
            }
            else
            {
                // Fallback for other potential payload structures
                dynamic args = parameter;
                try 
                { 
                    sourceObj = args.Item1; 
                    System.Diagnostics.Debug.WriteLine("[CreateConnection] Found Item1");
                    System.IO.File.AppendAllText(logPath, "[CreateConnection] Found Item1\r\n"); 
                } 
                catch 
                { 
                    try 
                    { 
                        sourceObj = args.Source; 
                        System.Diagnostics.Debug.WriteLine("[CreateConnection] Found Source");
                        System.IO.File.AppendAllText(logPath, "[CreateConnection] Found Source\r\n"); 
                    } 
                    catch { } 
                }
                
                try 
                { 
                    targetObj = args.Item2; 
                    System.Diagnostics.Debug.WriteLine("[CreateConnection] Found Item2");
                    System.IO.File.AppendAllText(logPath, "[CreateConnection] Found Item2\r\n"); 
                } 
                catch 
                { 
                    try 
                    { 
                        targetObj = args.Target; 
                        System.Diagnostics.Debug.WriteLine("[CreateConnection] Found Target");
                        System.IO.File.AppendAllText(logPath, "[CreateConnection] Found Target\r\n"); 
                    } 
                    catch { } 
                }
            }

            var source = sourceObj as FlowNodeViewModel;
            var target = targetObj as FlowNodeViewModel;

            string castMsg = $"[CreateConnection] Source cast: {source?.Id ?? "null"}, Target cast: {target?.Id ?? "null"}";
            System.Diagnostics.Debug.WriteLine(castMsg);
            System.IO.File.AppendAllText(logPath, castMsg + "\r\n");

            if (source != null && target != null && source != target)
            {
                if (!Connections.Any(c => c.Source == source && c.Target == target))
                {
                    Connections.Add(new FlowConnectionViewModel(source, target, new EdgeCondition.Fallback()));
                    string successMsg = $"[CreateConnection] Connection added successfully. Total connections: {Connections.Count}";
                    System.Diagnostics.Debug.WriteLine(successMsg);
                    System.IO.File.AppendAllText(logPath, successMsg + "\r\n");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("[CreateConnection] Connection already exists");
                    System.IO.File.AppendAllText(logPath, "[CreateConnection] Connection already exists\r\n");
                }
            }
            else
            {
                string failMsg = $"[CreateConnection] Validation failed. source == target? {source == target}";
                System.Diagnostics.Debug.WriteLine(failMsg);
                System.IO.File.AppendAllText(logPath, failMsg + "\r\n");
            }
        }
        catch (System.Exception ex)
        {
            string errMsg = $"[CreateConnection] Exception: {ex.Message}\r\n{ex.StackTrace}";
            System.Diagnostics.Debug.WriteLine(errMsg);
            System.IO.File.AppendAllText(logPath, errMsg + "\r\n");
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
            var edges = outgoingConnections.Select(c => new DagEdge(c.Target!.Id, c.ToEdgeCondition())).ToArray();
            
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
