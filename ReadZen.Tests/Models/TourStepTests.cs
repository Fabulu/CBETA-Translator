using System;
using System.Linq;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Models;

/// <summary>
/// Tests for TourStep model, TourStepType enum, and TourPlacement enum.
/// </summary>
public class TourStepTests
{
    // ---- 15. Default values are correct ----

    [Fact]
    public void TourStep_DefaultValues_AreCorrect()
    {
        var step = new TourStep();

        Assert.Equal("", step.Id);
        Assert.Equal("", step.Title);
        Assert.Equal("", step.Body);
        Assert.Null(step.TargetControlName);
        Assert.Equal(TourStepType.Passive, step.Type);
        Assert.Equal(TourPlacement.Bottom, step.Placement);
        Assert.Null(step.SwitchToTabIndex);
        Assert.Null(step.WaitForEvent);
    }

    // ---- 16. TourStepType enum has expected values ----

    [Fact]
    public void TourStepType_HasExpectedValues()
    {
        var values = Enum.GetValues<TourStepType>();

        Assert.Equal(3, values.Length);
        Assert.Contains(TourStepType.Passive, values);
        Assert.Contains(TourStepType.Active, values);
        Assert.Contains(TourStepType.Wait, values);
    }

    // ---- 17. TourPlacement enum has expected values ----

    [Fact]
    public void TourPlacement_HasExpectedValues()
    {
        var values = Enum.GetValues<TourPlacement>();

        Assert.Equal(5, values.Length);
        Assert.Contains(TourPlacement.Top, values);
        Assert.Contains(TourPlacement.Bottom, values);
        Assert.Contains(TourPlacement.Left, values);
        Assert.Contains(TourPlacement.Right, values);
        Assert.Contains(TourPlacement.Center, values);
    }

    // ---- ActionButtonLabel defaults to null ----

    [Fact]
    public void TourStep_ActionButtonLabel_DefaultsToNull()
    {
        var step = new TourStep();
        Assert.Null(step.ActionButtonLabel);
    }

    // ---- CanSkipWait defaults to false ----

    [Fact]
    public void TourStep_CanSkipWait_DefaultsToFalse()
    {
        var step = new TourStep();
        Assert.False(step.CanSkipWait);
    }

    // ---- IsMandatory defaults to false (only initial setup steps opt in) ----

    [Fact]
    public void TourStep_IsMandatory_DefaultsToFalse()
    {
        var step = new TourStep();
        Assert.False(step.IsMandatory);
    }

    [Fact]
    public void TourStep_IsMandatory_IsSettable()
    {
        var step = new TourStep { IsMandatory = true };
        Assert.True(step.IsMandatory);
    }

    // ---- ActionButtonLabel and CanSkipWait are settable ----

    [Fact]
    public void TourStep_ActionButtonLabel_And_CanSkipWait_AreSettable()
    {
        var step = new TourStep
        {
            ActionButtonLabel = "Sync Now",
            CanSkipWait = true
        };

        Assert.Equal("Sync Now", step.ActionButtonLabel);
        Assert.True(step.CanSkipWait);
    }

    // ---- Additional: TourStep properties are settable ----

    [Fact]
    public void TourStep_Properties_AreSettable()
    {
        var step = new TourStep
        {
            Id = "test-step",
            Title = "Test Title",
            Body = "Test body text",
            TargetControlName = "SomeControl",
            Type = TourStepType.Wait,
            Placement = TourPlacement.Left,
            SwitchToTabIndex = 2,
            WaitForEvent = "some-event"
        };

        Assert.Equal("test-step", step.Id);
        Assert.Equal("Test Title", step.Title);
        Assert.Equal("Test body text", step.Body);
        Assert.Equal("SomeControl", step.TargetControlName);
        Assert.Equal(TourStepType.Wait, step.Type);
        Assert.Equal(TourPlacement.Left, step.Placement);
        Assert.Equal(2, step.SwitchToTabIndex);
        Assert.Equal("some-event", step.WaitForEvent);
    }
}
