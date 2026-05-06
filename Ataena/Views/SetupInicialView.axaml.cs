using System;
using Avalonia.Controls;
using Ataena.ViewModels;

namespace Ataena.Views;

public partial class SetupInicialView : Window
{
    public SetupInicialView()
    {
        InitializeComponent();
        var vm = new SetupInicialViewModel(this);
        DataContext = vm;
        vm.SetupCompletado += OnSetupCompletado;
    }

    /// <summary>
    /// Se dispara cuando el usuario completa el setup.
    /// </summary>
    public event EventHandler? SetupCompletado;

    private void OnSetupCompletado(object? sender, EventArgs e)
    {
        SetupCompletado?.Invoke(this, EventArgs.Empty);
    }
}
