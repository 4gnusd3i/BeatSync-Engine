using System.ComponentModel;
using System.IO;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Windows.Media.Core;
using Windows.UI.Text;
using BeatSync.Desktop.Services;
using BeatSync.Desktop.ViewModels;

namespace BeatSync.Desktop;

public sealed partial class MainWindow : Window
{
    private static readonly FontWeight SemiBoldWeight = new() { Weight = 600 };
    private readonly PickerService _pickerService = new();
    private readonly MediaPlayerElement _previewPlayer = new()
    {
        AreTransportControlsEnabled = true,
        AutoPlay = false,
        HorizontalAlignment = HorizontalAlignment.Stretch,
        VerticalAlignment = VerticalAlignment.Stretch,
    };

    private bool _initialized;

    public MainViewModel ViewModel { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        Closed += OnWindowClosed;
        Title = "BeatSync Desktop";
        Content = BuildShell();
    }

    private UIElement BuildShell()
    {
        var scrollViewer = new ScrollViewer();
        scrollViewer.DataContext = ViewModel;
        scrollViewer.Loaded += RootScrollViewer_Loaded;

        var stack = new StackPanel
        {
            Padding = new Thickness(24),
            Spacing = 18,
        };
        scrollViewer.Content = stack;

        var header = new StackPanel { Spacing = 4 };
        header.Children.Add(CreateText("BeatSync Desktop", 32, SemiBoldWeight));
        header.Children.Add(CreateWrappedText("Native WinUI 3 shell backed by the portable Python engine in this repository."));
        var progressRing = new ProgressRing { Width = 28, Height = 28 };
        Bind(progressRing, ProgressRing.IsActiveProperty, nameof(MainViewModel.IsBusy));
        header.Children.Add(progressRing);
        var busyState = new TextBlock();
        Bind(busyState, TextBlock.TextProperty, nameof(MainViewModel.BusyStateText));
        header.Children.Add(busyState);
        stack.Children.Add(CreateCard(header));

        var runtimeGrid = CreateTwoColumnGrid();
        runtimeGrid.Children.Add(CreateInfoSection("Runtime", nameof(MainViewModel.RuntimeSummaryText)));
        var sourceInfo = CreateInfoSection("Sources", nameof(MainViewModel.SourceSummaryText));
        Grid.SetColumn(sourceInfo, 1);
        runtimeGrid.Children.Add(sourceInfo);
        stack.Children.Add(CreateCard(runtimeGrid));

        stack.Children.Add(CreateSourceSection());

        var analysisExportGrid = CreateTwoColumnGrid();
        analysisExportGrid.Children.Add(CreateAnalysisSection());
        FrameworkElement exportSection = CreateExportSection();
        Grid.SetColumn(exportSection, 1);
        analysisExportGrid.Children.Add(exportSection);
        stack.Children.Add(CreateCard(analysisExportGrid));

        var playbackRenderGrid = CreateTwoColumnGrid();
        playbackRenderGrid.Children.Add(CreatePlaybackSection());
        FrameworkElement renderSection = CreateRenderSection();
        Grid.SetColumn(renderSection, 1);
        playbackRenderGrid.Children.Add(renderSection);
        stack.Children.Add(CreateCard(playbackRenderGrid));

        var statusPanel = new StackPanel { Spacing = 8 };
        statusPanel.Children.Add(CreateText("Render log", 20, SemiBoldWeight));
        var statusBox = new TextBox
        {
            AcceptsReturn = true,
            IsReadOnly = true,
            MinHeight = 220,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = VerticalAlignment.Top,
        };
        Bind(statusBox, TextBox.TextProperty, nameof(MainViewModel.StatusText));
        statusPanel.Children.Add(statusBox);
        stack.Children.Add(CreateCard(statusPanel));

        var previewPanel = new StackPanel { Spacing = 8 };
        previewPanel.Children.Add(CreateText("Latest output", 20, SemiBoldWeight));
        previewPanel.Children.Add(CreateWrappedText("The Python backend returns a preview copy when the master render is not directly playable."));
        var previewHost = new Grid { MinHeight = 280 };
        var placeholder = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.WrapWholeWords,
        };
        Bind(placeholder, TextBlock.TextProperty, nameof(MainViewModel.PreviewPlaceholderText));
        Bind(placeholder, TextBlock.VisibilityProperty, nameof(MainViewModel.PreviewPlaceholderVisibility));
        Bind(_previewPlayer, UIElement.VisibilityProperty, nameof(MainViewModel.PreviewPlayerVisibility));
        previewHost.Children.Add(placeholder);
        previewHost.Children.Add(_previewPlayer);
        previewPanel.Children.Add(previewHost);
        stack.Children.Add(CreateCard(previewPanel));

        return scrollViewer;
    }

    private Border CreateSourceSection()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(CreateText("Source media", 20, SemiBoldWeight));

        var audioGrid = CreateBrowseGrid();
        var audioBox = new TextBox
        {
            Header = "Audio file",
            PlaceholderText = @"C:\Music\song.mp3",
        };
        Bind(audioBox, TextBox.TextProperty, nameof(MainViewModel.AudioPath), BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
        audioGrid.Children.Add(audioBox);
        var audioButton = new Button
        {
            Content = "Browse audio",
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        audioButton.Click += BrowseAudioButton_Click;
        Grid.SetColumn(audioButton, 1);
        audioGrid.Children.Add(audioButton);
        panel.Children.Add(audioGrid);

        var videoGrid = CreateBrowseGrid();
        var videoBox = new TextBox
        {
            Header = "Video folder",
            PlaceholderText = @"D:\VideoClips",
        };
        Bind(videoBox, TextBox.TextProperty, nameof(MainViewModel.VideoFolder), BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
        videoGrid.Children.Add(videoBox);
        var videoButton = new Button
        {
            Content = "Browse folder",
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        videoButton.Click += BrowseVideoButton_Click;
        Grid.SetColumn(videoButton, 1);
        videoGrid.Children.Add(videoButton);
        panel.Children.Add(videoGrid);

        return CreateCard(panel);
    }

    private StackPanel CreateAnalysisSection()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(CreateText("Analysis", 20, SemiBoldWeight));

        var generationMode = new ComboBox
        {
            Header = "Generation mode",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(generationMode, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.GenerationModes));
        Bind(generationMode, Selector.SelectedValueProperty, nameof(MainViewModel.GenerationMode), BindingMode.TwoWay);
        panel.Children.Add(generationMode);

        var smartPresetPanel = new StackPanel();
        Bind(smartPresetPanel, UIElement.VisibilityProperty, nameof(MainViewModel.SmartPresetVisibility));
        var smartPreset = new ComboBox
        {
            Header = "Smart preset",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(smartPreset, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.SmartPresets));
        Bind(smartPreset, Selector.SelectedValueProperty, nameof(MainViewModel.SmartPreset), BindingMode.TwoWay);
        smartPresetPanel.Children.Add(smartPreset);
        panel.Children.Add(smartPresetPanel);

        var manualPanel = new StackPanel { Spacing = 6 };
        Bind(manualPanel, UIElement.VisibilityProperty, nameof(MainViewModel.ManualIntensityVisibility));
        manualPanel.Children.Add(new TextBlock { Text = "Cut intensity" });
        var cutSlider = new Slider
        {
            Minimum = 0.1,
            Maximum = 16,
            SmallChange = 0.1,
            StepFrequency = 0.1,
        };
        Bind(cutSlider, Slider.ValueProperty, nameof(MainViewModel.CutIntensity), BindingMode.TwoWay);
        manualPanel.Children.Add(cutSlider);
        var cutCaption = new TextBlock();
        Bind(cutCaption, TextBlock.TextProperty, nameof(MainViewModel.CutIntensityCaption));
        manualPanel.Children.Add(cutCaption);
        panel.Children.Add(manualPanel);

        return panel;
    }

    private StackPanel CreateExportSection()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(CreateText("Export pipeline", 20, SemiBoldWeight));

        var processingMode = new ComboBox
        {
            Header = "Processing mode",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(processingMode, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.ProcessingModes));
        Bind(processingMode, Selector.SelectedValueProperty, nameof(MainViewModel.ProcessingMode), BindingMode.TwoWay);
        panel.Children.Add(processingMode);

        var standardQualityPanel = new StackPanel();
        Bind(standardQualityPanel, UIElement.VisibilityProperty, nameof(MainViewModel.StandardQualityVisibility));
        var standardQuality = new ComboBox
        {
            Header = "Standard quality",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(standardQuality, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.StandardQualities));
        Bind(standardQuality, Selector.SelectedValueProperty, nameof(MainViewModel.StandardQuality), BindingMode.TwoWay);
        standardQualityPanel.Children.Add(standardQuality);
        panel.Children.Add(standardQualityPanel);

        var proresPanel = new StackPanel();
        Bind(proresPanel, UIElement.VisibilityProperty, nameof(MainViewModel.ProresDeliveryVisibility));
        var proresToggle = new ToggleSwitch
        {
            Header = "Create delivery MP4 copy",
            OnContent = "Yes",
            OffContent = "No",
        };
        Bind(proresToggle, ToggleSwitch.IsOnProperty, nameof(MainViewModel.CreateProresDeliveryMp4), BindingMode.TwoWay);
        proresPanel.Children.Add(proresToggle);
        panel.Children.Add(proresPanel);

        return panel;
    }

    private StackPanel CreatePlaybackSection()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(CreateText("Playback", 20, SemiBoldWeight));

        var direction = new ComboBox
        {
            Header = "Direction",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(direction, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.Directions));
        Bind(direction, Selector.SelectedValueProperty, nameof(MainViewModel.Direction), BindingMode.TwoWay);
        panel.Children.Add(direction);

        var playbackSpeed = new ComboBox
        {
            Header = "Playback speed",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(playbackSpeed, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.PlaybackSpeeds));
        Bind(playbackSpeed, Selector.SelectedValueProperty, nameof(MainViewModel.PlaybackSpeed), BindingMode.TwoWay);
        panel.Children.Add(playbackSpeed);

        var timingOffset = new NumberBox
        {
            Header = "Timing offset (seconds)",
            Minimum = -0.5,
            Maximum = 0.5,
            SmallChange = 0.01,
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact,
        };
        Bind(timingOffset, NumberBox.ValueProperty, nameof(MainViewModel.TimingOffset), BindingMode.TwoWay);
        panel.Children.Add(timingOffset);

        return panel;
    }

    private StackPanel CreateRenderSection()
    {
        var panel = new StackPanel { Spacing = 12 };
        panel.Children.Add(CreateText("Render settings", 20, SemiBoldWeight));

        var resolution = new ComboBox
        {
            Header = "Target resolution",
            DisplayMemberPath = "Label",
            SelectedValuePath = "Value",
        };
        Bind(resolution, ItemsControl.ItemsSourceProperty, nameof(MainViewModel.ResolutionPresets));
        Bind(resolution, Selector.SelectedValueProperty, nameof(MainViewModel.CustomResolution), BindingMode.TwoWay);
        panel.Children.Add(resolution);

        var customFps = new TextBox
        {
            Header = "Custom FPS",
            PlaceholderText = "Auto-detect from the first source clip",
        };
        Bind(customFps, TextBox.TextProperty, nameof(MainViewModel.CustomFpsText), BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
        panel.Children.Add(customFps);

        panel.Children.Add(new TextBlock { Text = "Parallel workers" });
        var workersSlider = new Slider
        {
            Minimum = 1,
            StepFrequency = 1,
            SmallChange = 1,
        };
        Bind(workersSlider, Slider.MaximumProperty, nameof(MainViewModel.MaximumParallelWorkers));
        Bind(workersSlider, Slider.ValueProperty, nameof(MainViewModel.ParallelWorkers), BindingMode.TwoWay);
        panel.Children.Add(workersSlider);
        var workersCaption = new TextBlock();
        Bind(workersCaption, TextBlock.TextProperty, nameof(MainViewModel.ParallelWorkersCaption));
        panel.Children.Add(workersCaption);

        var outputName = new TextBox { Header = "Output filename" };
        Bind(outputName, TextBox.TextProperty, nameof(MainViewModel.OutputFilename), BindingMode.TwoWay, UpdateSourceTrigger.PropertyChanged);
        panel.Children.Add(outputName);

        var renderButton = new Button();
        Bind(renderButton, Button.CommandProperty, nameof(MainViewModel.RenderCommand));
        Bind(renderButton, ContentControl.ContentProperty, nameof(MainViewModel.RenderButtonText));
        panel.Children.Add(renderButton);

        return panel;
    }

    private StackPanel CreateInfoSection(string heading, string bindingPath)
    {
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(CreateText(heading, 20, SemiBoldWeight));
        var body = new TextBlock { TextWrapping = TextWrapping.WrapWholeWords };
        Bind(body, TextBlock.TextProperty, bindingPath);
        panel.Children.Add(body);
        return panel;
    }

    private static Grid CreateTwoColumnGrid()
    {
        var grid = new Grid { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        return grid;
    }

    private static Grid CreateBrowseGrid()
    {
        var grid = new Grid { ColumnSpacing = 12 };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        return grid;
    }

    private static Border CreateCard(UIElement child)
    {
        return new Border
        {
            Padding = new Thickness(16),
            CornerRadius = new CornerRadius(18),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(48, 120, 120, 120)),
            Background = new SolidColorBrush(ColorHelper.FromArgb(18, 255, 255, 255)),
            Child = child,
        };
    }

    private static TextBlock CreateText(string text, double fontSize, FontWeight fontWeight)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = fontSize,
            FontWeight = fontWeight,
        };
    }

    private static TextBlock CreateWrappedText(string text)
    {
        return new TextBlock
        {
            Text = text,
            TextWrapping = TextWrapping.WrapWholeWords,
        };
    }

    private static void Bind(
        FrameworkElement target,
        DependencyProperty property,
        string path,
        BindingMode mode = BindingMode.OneWay,
        UpdateSourceTrigger updateSourceTrigger = UpdateSourceTrigger.Default)
    {
        target.SetBinding(
            property,
            new Binding
            {
                Path = new PropertyPath(path),
                Mode = mode,
                UpdateSourceTrigger = updateSourceTrigger,
            }
        );
    }

    private async void RootScrollViewer_Loaded(object sender, RoutedEventArgs e)
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        await ViewModel.InitializeAsync();
        UpdatePreviewSource();
    }

    private async void BrowseAudioButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = await _pickerService.PickAudioFileAsync(this);
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            ViewModel.AudioPath = selectedPath;
        }
    }

    private async void BrowseVideoButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedPath = await _pickerService.PickFolderAsync(this);
        if (!string.IsNullOrWhiteSpace(selectedPath))
        {
            ViewModel.VideoFolder = selectedPath;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PreviewPath))
        {
            UpdatePreviewSource();
        }
    }

    private void UpdatePreviewSource()
    {
        if (string.IsNullOrWhiteSpace(ViewModel.PreviewPath) || !File.Exists(ViewModel.PreviewPath))
        {
            _previewPlayer.Source = null;
            return;
        }

        _previewPlayer.Source = MediaSource.CreateFromUri(new Uri(ViewModel.PreviewPath));
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        ViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _previewPlayer.Source = null;
        _previewPlayer.MediaPlayer?.Dispose();
    }
}
