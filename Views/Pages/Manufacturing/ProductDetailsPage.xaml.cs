using Poplar.ViewModels.Pages.Manufacturing;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace Poplar.Views.Pages.Manufacturing;

public partial class ProductDetailsPage : INavigationAware
{
    public ProductDetailsViewModel ViewModel { get; }

    public ProductDetailsPage(ProductDetailsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    public async Task OnNavigatedToAsync()
    {
        // The productId should be passed from the navigation service somehow.
        // For now, if we get here, we might need to read it from a global state or
        // pass it during navigation. WPF UI navigation doesn't directly pass parameters to OnNavigatedToAsync.
        // We will handle passing the ID in the ProductsViewModel.
    }

    public Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    private void OnStepPalettePreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem != null)
        {
            DragDrop.DoDragDrop(listBox, listBox.SelectedItem, DragDropEffects.Copy);
        }
    }

    private void OnNodifyEditorDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(typeof(uniffi.stump.StepMetadata)) is uniffi.stump.StepMetadata meta)
        {
            var editor = (Nodify.NodifyEditor)sender;
            var pos = e.GetPosition(editor);
            
            // Call the command on the ViewModel
            if (ViewModel.DagEditor.AddNodeCommand.CanExecute(meta))
            {
                ViewModel.DagEditor.AddNodeCommand.Execute(meta);
                
                // Update the location of the newly added node
                if (ViewModel.DagEditor.Nodes.LastOrDefault() is FlowNodeViewModel newNode)
                {
                    newNode.Location = pos;
                }
            }
        }
    }
}
