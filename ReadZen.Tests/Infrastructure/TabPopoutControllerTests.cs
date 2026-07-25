// ReadZen.Tests/Infrastructure/TabPopoutControllerTests.cs
//
// Pins the core contract of the tab pop-out reparenting primitive
// (POPOUT_TABS_DESIGN §4): a detach -> dock-back round-trip must return the EXACT SAME
// control instance to its slot (so live state — VM, scroll, selection — is preserved,
// never rebuilt), and popping out twice must not double-detach or spawn a second float.
//
// Runs on the headless Avalonia UI thread (platform bootstrapped by ModuleInit.cs), the
// same harness the RowGrid smoke tests use to Show() real windows.

using Avalonia.Controls;
using Avalonia.Threading;
using ReadZen.App.Infrastructure;
using ReadZen.App.Views;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "TabPopout")]
public class TabPopoutControllerTests
{
    private static (Window owner, Decorator slot, Control view, TabPopoutController controller) Build()
    {
        var owner = new Window();
        var view = new Border();                 // stand-in for a live tab view
        var slot = new Decorator { Child = view };
        owner.Content = slot;

        var controller = new TabPopoutController(owner);
        controller.Register(new TabPopoutDescriptor
        {
            TabIndex = 2,
            Title = "Search",
            Slot = slot,
        });
        return (owner, slot, view, controller);
    }

    [Fact]
    public void DetachThenDock_ReturnsSameControlInstanceToSlot()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var (_, slot, view, controller) = Build();

            Assert.False(controller.IsPoppedOut(2));
            Assert.Same(view, slot.Child);

            controller.PopOut(2);

            // While popped: the slot holds the placeholder, not the live view.
            Assert.True(controller.IsPoppedOut(2));
            Assert.IsType<PoppedOutPlaceholder>(slot.Child);
            Assert.NotSame(view, slot.Child);

            controller.DockBack(2);

            // Docked: the SAME instance is back — no rebuild.
            Assert.False(controller.IsPoppedOut(2));
            Assert.Same(view, slot.Child);
        });
    }

    [Fact]
    public void PopOutTwice_DoesNotDoubleDetach()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var (_, slot, view, controller) = Build();

            controller.PopOut(2);
            var placeholderAfterFirst = slot.Child;
            Assert.IsType<PoppedOutPlaceholder>(placeholderAfterFirst);

            // Second pop-out just re-activates the existing float; it must not replace
            // the placeholder or lose the detached view.
            controller.PopOut(2);
            Assert.Same(placeholderAfterFirst, slot.Child);
            Assert.True(controller.IsPoppedOut(2));

            controller.DockBack(2);
            Assert.Same(view, slot.Child);
            Assert.False(controller.IsPoppedOut(2));
        });
    }

    [Fact]
    public void DockAllBack_ClosesEveryFloat()
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var (_, slot, view, controller) = Build();

            controller.PopOut(2);
            Assert.True(controller.IsPoppedOut(2));

            controller.DockAllBack();

            Assert.False(controller.IsPoppedOut(2));
            Assert.Same(view, slot.Child);
        });
    }
}
