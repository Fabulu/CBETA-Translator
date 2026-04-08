using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public sealed class AnnotationMarkerOverlay : Control
{
    public static readonly StyledProperty<TextBox?> TargetProperty =
        AvaloniaProperty.Register<AnnotationMarkerOverlay, TextBox?>(nameof(Target));

    public static readonly StyledProperty<IReadOnlyList<DocAnnotation>> AnnotationsProperty =
        AvaloniaProperty.Register<AnnotationMarkerOverlay, IReadOnlyList<DocAnnotation>>(
            nameof(Annotations),
            defaultValue: Array.Empty<DocAnnotation>());

    public TextBox? Target
    {
        get => GetValue(TargetProperty);
        set => SetValue(TargetProperty, value);
    }

    public IReadOnlyList<DocAnnotation> Annotations
    {
        get => GetValue(AnnotationsProperty);
        set => SetValue(AnnotationsProperty, value ?? Array.Empty<DocAnnotation>());
    }

    public AnnotationMarkerOverlay()
    {
        // keep invalidation harmless
        AffectsRender<AnnotationMarkerOverlay>(TargetProperty, AnnotationsProperty);

        // IMPORTANT: you want *no* overlay visuals, ever.
        IsHitTestVisible = false;
        Opacity = 0; // even if someone draws later, it won't show
    }

    public override void Render(DrawingContext context)
    {
        // Intentionally empty: overlay disabled.
    }
}
