using System.Windows;
using System.Windows.Input;
using ForeverGuiDesigner.Controls;

namespace ForeverGuiDesigner;

/// <summary>
/// Phase-3 mouse manipulation layer. It intentionally lives outside MinecraftBlockModelViewport so
/// the mature Block Editor viewport keeps its existing interaction contract. Only the Machine Builder
/// consumes Move/Scale drag gestures here.
/// </summary>
public partial class MainWindow
{
    private bool _machineBuilderVisualDirectManipulationInitialized;
    private bool _machineBuilderVisualTransformDragging;
    private Point _machineBuilderVisualTransformLastPoint;
    private int _machineBuilderVisualTransformElementIndex = -1;

    public void EnableForeverMachineBuilderVisualDirectManipulationPhase3()
    {
        if (_machineBuilderVisualDirectManipulationInitialized || _machineBuilderVisualViewport is null)
            return;
        _machineBuilderVisualDirectManipulationInitialized = true;

        _machineBuilderVisualViewport.PreviewMouseLeftButtonDown += MachineBuilderVisualTransformMouseDown;
        _machineBuilderVisualViewport.PreviewMouseMove += MachineBuilderVisualTransformMouseMove;
        _machineBuilderVisualViewport.PreviewMouseLeftButtonUp += MachineBuilderVisualTransformMouseUp;
        _machineBuilderVisualViewport.LostMouseCapture += (_, _) => EndMachineBuilderVisualTransformDrag();

        SetMachineBuilderVisualViewportMode(MinecraftViewportEditMode.None);
        SetMachineBuilderVisualViewportAxis("x");
    }

    private void MachineBuilderVisualTransformMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_machineBuilderVisualViewport is null ||
            _machineBuilderVisualViewport.EditMode == MinecraftViewportEditMode.None ||
            string.IsNullOrWhiteSpace(_machineBuilderVisualSelectedComponentId))
            return;

        _machineBuilderVisualTransformElementIndex = _machineBuilderVisualElementComponentIds.FindIndex(id =>
            string.Equals(id, _machineBuilderVisualSelectedComponentId, StringComparison.OrdinalIgnoreCase));
        if (_machineBuilderVisualTransformElementIndex < 0)
            return;

        _machineBuilderVisualTransformDragging = true;
        _machineBuilderVisualTransformLastPoint = e.GetPosition(_machineBuilderVisualViewport);
        _machineBuilderVisualViewport.CaptureMouse();
        _machineBuilderVisualViewport.Cursor = Cursors.SizeAll;
        e.Handled = true;
    }

    private void MachineBuilderVisualTransformMouseMove(object sender, MouseEventArgs e)
    {
        if (!_machineBuilderVisualTransformDragging || _machineBuilderVisualViewport is null ||
            e.LeftButton != MouseButtonState.Pressed)
            return;

        var current = e.GetPosition(_machineBuilderVisualViewport);
        var pixelDelta = current - _machineBuilderVisualTransformLastPoint;
        _machineBuilderVisualTransformLastPoint = current;

        // Horizontal and vertical mouse motion both contribute so manipulation remains comfortable
        // regardless of the active X/Y/Z axis. Keep increments small and quantized to model units.
        var raw = (pixelDelta.X - pixelDelta.Y) * 0.03;
        var delta = Math.Round(raw / 0.05, MidpointRounding.AwayFromZero) * 0.05;
        if (Math.Abs(delta) < 0.0001)
        {
            e.Handled = true;
            return;
        }

        MachineBuilderVisualViewportTransformDelta(
            _machineBuilderVisualTransformElementIndex,
            _machineBuilderVisualViewport.EditAxis,
            delta,
            _machineBuilderVisualViewport.EditMode);
        e.Handled = true;
    }

    private void MachineBuilderVisualTransformMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!_machineBuilderVisualTransformDragging)
            return;
        EndMachineBuilderVisualTransformDrag();
        e.Handled = true;
    }

    private void EndMachineBuilderVisualTransformDrag()
    {
        _machineBuilderVisualTransformDragging = false;
        _machineBuilderVisualTransformElementIndex = -1;
        if (_machineBuilderVisualViewport?.IsMouseCaptured == true)
            _machineBuilderVisualViewport.ReleaseMouseCapture();
        if (_machineBuilderVisualViewport is not null)
            _machineBuilderVisualViewport.Cursor = Cursors.Arrow;
    }
}
