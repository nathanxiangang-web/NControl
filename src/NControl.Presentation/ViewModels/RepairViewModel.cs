using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using NControl.Core;
using NControl.Presentation.Services;

namespace NControl.Presentation.ViewModels;

/// <summary>系统修复页:按问题组织修复入口(产品文档 §6.6)。</summary>
public partial class RepairViewModel : ObservableObject
{
    public RepairViewModel(IFunctionCatalog catalog, NavigationService nav)
    {
        AddCard(catalog, nav, "repair.system-integrity", "S");
        AddCard(catalog, nav, "repair.update-reset", "U");
        AddCard(catalog, nav, "repair.store-reregister", "M");

        AddNetwork(catalog, nav, "repair.network-dns-flush");
        AddNetwork(catalog, nav, "repair.network-winsock");
        AddNetwork(catalog, nav, "repair.network-tcpip");
        AddNetwork(catalog, nav, "repair.network-ip-renew");
    }

    public ObservableCollection<RepairCardViewModel> Cards { get; } = new();
    public ObservableCollection<RepairCardViewModel> NetworkActions { get; } = new();

    private void AddCard(IFunctionCatalog catalog, NavigationService nav, string id, string glyph)
    {
        var item = catalog.Find(id);
        if (item is not null) Cards.Add(new RepairCardViewModel(item, glyph, nav));
    }

    private void AddNetwork(IFunctionCatalog catalog, NavigationService nav, string id)
    {
        var item = catalog.Find(id);
        if (item is not null) NetworkActions.Add(new RepairCardViewModel(item, "▶", nav));
    }
}
