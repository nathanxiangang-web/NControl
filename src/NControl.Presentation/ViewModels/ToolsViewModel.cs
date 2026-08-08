using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>实用工具页:即时工具,点击即运行(产品文档 §5.3、§6.7)。</summary>
public partial class ToolsViewModel : ObservableObject
{
    public ToolsViewModel(IFunctionCatalog catalog, NavigationService nav)
    {
        foreach (var item in catalog.ByModule(ModuleKind.Tools))
            Tools.Add(new ToolEntryViewModel(item, nav));
    }

    public ObservableCollection<ToolEntryViewModel> Tools { get; } = new();
}

public partial class ToolEntryViewModel : ObservableObject
{
    private readonly NavigationService _nav;

    public ToolEntryViewModel(FunctionItem item, NavigationService nav)
    {
        Item = item;
        _nav = nav;
        Title = item.Name;
        Description = item.Description;
        MetaText = item.RequiresAdmin ? "需要管理员权限" : "即时运行";
    }

    public FunctionItem Item { get; }
    public string Title { get; }
    public string Description { get; }
    public string MetaText { get; }

    [RelayCommand]
    private async Task RunAsync()
    {
        await _nav.RunSingleAsync(Item);
    }
}
