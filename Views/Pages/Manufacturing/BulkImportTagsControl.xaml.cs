using System.Windows.Controls;

namespace Poplar.Views.Pages.Manufacturing;

public partial class BulkImportTagsControl : UserControl
{
    public BulkImportTagsControl()
    {
        InitializeComponent();
    }

    public string GetImportText()
    {
        return ImportTextBox.Text;
    }
}
