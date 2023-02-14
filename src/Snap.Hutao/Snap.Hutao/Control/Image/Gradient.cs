﻿// Copyright (c) DGP Studio. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Snap.Hutao.Win32;
using System.IO;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace Snap.Hutao.Control.Image;

/// <summary>
/// 渐变图像
/// </summary>
[HighQuality]
internal sealed class Gradient : CompositionImage
{
    private static readonly DependencyProperty BackgroundDirectionProperty = Property<Gradient>.Depend(nameof(BackgroundDirection), GradientDirection.TopToBottom);
    private static readonly DependencyProperty ForegroundDirectionProperty = Property<Gradient>.Depend(nameof(ForegroundDirection), GradientDirection.TopToBottom);

    private double imageAspectRatio;

    /// <summary>
    /// 背景方向
    /// </summary>
    public GradientDirection BackgroundDirection
    {
        get => (GradientDirection)GetValue(BackgroundDirectionProperty);
        set => SetValue(BackgroundDirectionProperty, value);
    }

    /// <summary>
    /// 前景方向
    /// </summary>
    public GradientDirection ForegroundDirection
    {
        get => (GradientDirection)GetValue(ForegroundDirectionProperty);
        set => SetValue(ForegroundDirectionProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnUpdateVisual(SpriteVisual spriteVisual)
    {
        if (spriteVisual is not null)
        {
            Height = Math.Clamp(ActualWidth / imageAspectRatio, 0D, MaxHeight);
            spriteVisual.Size = ActualSize;
        }
    }

    /// <inheritdoc/>
    protected override async Task<LoadedImageSurface> LoadImageSurfaceAsync(string file, CancellationToken token)
    {
        TaskCompletionSource loadCompleteTaskSource = new();
        LoadedImageSurface surface = LoadedImageSurface.StartLoadFromUri(new(file));
        surface.LoadCompleted += (s, e) => loadCompleteTaskSource.TrySetResult();
        await loadCompleteTaskSource.Task.ConfigureAwait(true);
        imageAspectRatio = surface.NaturalSize.AspectRatio();
        return surface;
    }

    /// <inheritdoc/>
    protected override SpriteVisual CompositeSpriteVisual(Compositor compositor, LoadedImageSurface imageSurface)
    {
        CompositionSurfaceBrush imageSurfaceBrush = compositor.CompositeSurfaceBrush(imageSurface, stretch: CompositionStretch.UniformToFill, vRatio: 0f);

        CompositionLinearGradientBrush backgroundBrush = compositor.CompositeLinearGradientBrush(BackgroundDirection, new(0, Colors.White), new(1, Colors.Black));
        CompositionLinearGradientBrush foregroundBrush = compositor.CompositeLinearGradientBrush(ForegroundDirection, new(0, Colors.White), new(1, Colors.Black));

        CompositionEffectBrush gradientEffectBrush = compositor.CompositeBlendEffectBrush(backgroundBrush, foregroundBrush);
        CompositionEffectBrush opacityMaskEffectBrush = compositor.CompositeLuminanceToAlphaEffectBrush(gradientEffectBrush);
        CompositionEffectBrush alphaMaskEffectBrush = compositor.CompositeAlphaMaskEffectBrush(imageSurfaceBrush, opacityMaskEffectBrush);

        return compositor.CompositeSpriteVisual(alphaMaskEffectBrush);
    }
}