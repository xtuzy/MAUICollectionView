using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using System.Windows.Input;
using GestureRecognizer = Microsoft.UI.Input.GestureRecognizer;

namespace MauiUICollectionView.Gestures
{
    public class GestureManager : IGestureManager
    {
        readonly GestureRecognizer detector;
        private object commandParameter;

        /// <summary>
        /// Take a Point parameter
        /// Except panPointCommand which takes a (Point,GestureStatus) parameter (its a tuple) 
        /// </summary>
        public ICommand? SelectPointCommand { get; set; }
        public ICommand? DragPointCommand { get; set; }
        public ICommand? LongPressPointCommand { get; set; }

        public GestureManager()
        {
            detector = new()
            {
                GestureSettings =
                    GestureSettings.Drag | GestureSettings.Hold
                    | GestureSettings.HoldWithMouse,
                ShowGestureFeedback = false,
                //CrossSlideHorizontally = true
                //AutoProcessInertia = true //default
            };

            detector.Dragging += (sender, args) =>
            {
                //Debug.WriteLine("drag");
                if (!IsSupportDrag)
                    return;
                var gestureStatus = args.DraggingState switch
                {
                    DraggingState.Started => GestureStatus.Started,
                    DraggingState.Continuing => GestureStatus.Running,
                    DraggingState.Completed => GestureStatus.Completed,
                    _ => GestureStatus.Canceled
                };
                var parameters = new MauiUICollectionView.DragEventArgs(gestureStatus, new Point(args.Position.X, args.Position.Y));
                parameters.Device = args.PointerDeviceType switch
                {
                    PointerDeviceType.Mouse => GestureDevice.Mouse,
                    _ => GestureDevice.Touch
                };
                TriggerCommand(DragPointCommand, parameters);
            };

            //pan gesture
            /*detector.Holding += (sender, args) =>
            {
                Debug.WriteLine("holding");
                if (args.HoldingState == HoldingState.Started)
                {
                    if (selectStatus == SelectStatus.WillSelect)
                    {
                        selectStatus = SelectStatus.Selected;
                        TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(args.Position.X, args.Position.Y)));
                    }
                    TriggerCommand(LongPressPointCommand, new Point(args.Position.X, args.Position.Y));
                }
            };*/
        }

        private void TriggerCommand(ICommand command, object parameter)
        {
            if (command?.CanExecute(parameter) == true)
                command.Execute(parameter);
        }

        bool IsSupportDrag;
        public void SetCanDrag(bool can = false)
        {
            IsSupportDrag = can;
        }

        View virtualView;
        FrameworkElement view;
        public void SubscribeGesture(View view)
        {
            var platformView = view.Handler.PlatformView as FrameworkElement;
            this.virtualView = view;
            this.view = platformView;
            var control = platformView;
            control.Tapped += Control_Tapped;
            control.PointerMoved += ControlOnPointerMoved;
            control.PointerPressed += ControlOnPointerPressed;
            control.PointerReleased += ControlOnPointerReleased;
            control.PointerCanceled += ControlOnPointerCanceled;
            control.PointerCaptureLost += ControlOnPointerCanceled;
        }

        public void CancleSubscribeGesture()
        {
            var control = view;
            control.Tapped -= Control_Tapped;
            control.PointerMoved -= ControlOnPointerMoved;
            control.PointerPressed -= ControlOnPointerPressed;
            control.PointerReleased -= ControlOnPointerReleased;
            control.PointerCanceled -= ControlOnPointerCanceled;
            control.PointerCaptureLost -= ControlOnPointerCanceled;
        }

        private void Control_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Debug.WriteLine("tapped");
            var p = GetPositionRelativeToPlatformElement(e, view);
            //Tap must be selection
            if (selectStatus == SelectStatus.WillSelect)
            {
                selectStatus = SelectStatus.Selected;
                TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(p.X, p.Y)));
            }
        }

        public static Windows.Foundation.Point GetPositionRelativeToPlatformElement(RoutedEventArgs e, UIElement? relativeTo)
        {
            if (e is RightTappedRoutedEventArgs rt)
                return rt.GetPosition(relativeTo);
            else if (e is TappedRoutedEventArgs t)
                return t.GetPosition(relativeTo);
            else if (e is DoubleTappedRoutedEventArgs dt)
                return dt.GetPosition(relativeTo);
            else if (e is PointerRoutedEventArgs p)
            {
                var point = p.GetCurrentPoint(relativeTo);
                return new Windows.Foundation.Point(point.Position.X, point.Position.Y);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        SelectStatus selectStatus = SelectStatus.CancelWillSelect;
        private void ControlOnPointerPressed(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            view.CapturePointer(pointerRoutedEventArgs.Pointer);
            Debug.WriteLine("press");
            var point = pointerRoutedEventArgs.GetCurrentPoint(view);
            if (point.Properties.IsRightButtonPressed)
            {
                //right mouse button be used to show contextmenu by winui3, so we not deal with it.
                return;
            }
            else
            {
                selectStatus = SelectStatus.WillSelect;
                TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(point.Position.X, point.Position.Y)));
            }
            detector.ProcessDownEvent(point);
            pointerRoutedEventArgs.Handled = true;
        }

        private void ControlOnPointerMoved(object sender, PointerRoutedEventArgs pointerRoutedEventArgs)
        {
            //Debug.WriteLine("move");
            var point = pointerRoutedEventArgs.GetCurrentPoint(view);
            if (point.Properties.IsRightButtonPressed)// not use right mouse button
            {
                return;
            }
            //if move, it is not selection
            if (selectStatus == SelectStatus.WillSelect)
            {
                selectStatus = SelectStatus.CancelWillSelect;
                TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(point.Position.X, point.Position.Y)));
            }

            var points = pointerRoutedEventArgs.GetIntermediatePoints(view);
            detector.ProcessMoveEvents(points);
            pointerRoutedEventArgs.Handled = true;
        }

        private void ControlOnPointerCanceled(object sender, PointerRoutedEventArgs args)
        {
            Debug.WriteLine("cancel");
            var point = args.GetCurrentPoint(view);
            
            if (selectStatus == SelectStatus.WillSelect)
            {
                selectStatus = SelectStatus.CancelWillSelect;
                TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(point.Position.X, point.Position.Y)));
            }

            detector.CompleteGesture();
            args.Handled = true;
            view.ReleasePointerCapture(args.Pointer);
        }

        private void ControlOnPointerReleased(object sender, PointerRoutedEventArgs args)
        {
            //Debug.WriteLine("up");
            var point = args.GetCurrentPoint(view);
            
            if (selectStatus == SelectStatus.WillSelect)
            {
                selectStatus = SelectStatus.Selected;
                TriggerCommand(SelectPointCommand, new SelectEventArgs(selectStatus, new Point(point.Position.X, point.Position.Y)));
            }

            detector.ProcessUpEvent(point);
            args.Handled = true;
            view.ReleasePointerCapture(args.Pointer);
        }
    }
}
