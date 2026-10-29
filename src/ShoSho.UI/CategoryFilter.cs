using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ShoSho.UI.ViewModels;

public partial class CategoryFilter : ObservableObject
{
    public CategoryFilter(string name, string icon, string? typeValue, bool isCustom = false)
    {
        Name = name;
        Icon = icon;
        TypeValue = typeValue;
        IsCustom = isCustom;
    }

    public string Name { get; }
    public string Icon { get; }

    public string? TypeValue { get; }
    public bool IsCustom { get; }

    [ObservableProperty] private bool _isSelected;
    [ObservableProperty] private int _count;

    public bool Matches(string? type) =>
        TypeValue == null ||
        string.Equals(type?.Trim(), TypeValue, StringComparison.OrdinalIgnoreCase);
}

public partial class TagOption : ObservableObject
{
    public TagOption(string name) => Name = name;

    public string Name { get; }

    [ObservableProperty] private bool _isSelected;
}