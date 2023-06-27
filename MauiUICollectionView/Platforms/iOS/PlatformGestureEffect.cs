using Microsoft.Maui.Platform;
using System.ComponentModel;
using System.Windows.Input;
using UIKit;

namespace MauiUICollectionView.Platforms.iOS
{
    internal partial class PlatformGestureEffect
    {
        private UILongPressGestureRecognizer longPressDetector;
        private UIImmediatePanGestureRecognizer panDetector;
        private List<UIGestureRecognizer> recognizers;

        /// <summary>
        /// Take a Point parameter
        /// Except panPointCommand which takes a (Point,GestureStatus) parameter (its a tuple) 
        /// </summary>
        private ICommand tapPointCommand, panPointCommand, doubleTapPointCommand, longPressPointCommand;

        /// <summary>
        /// No parameter
        /// </summary>
        private ICommand tapCommand, panCommand, doubleTapCommand, longPressCommand, swipeLeftCommand, swipeRightCommand, swipeTopCommand, swipeBottomCommand;

        /// <summary>
        /// 1 parameter: PinchEventArgs
        /// </summary>
        private ICommand pinchCommand;

        private object commandParameter;

        public PlatformGestureEffect()
        {
            //if (!allSubviews)
            //    tapDetector.ShouldReceiveTouch = (s, args) => args.View != null && (args.View == view || view.Subviews.Any(v => v == args.View));
            //else
            //    tapDetector.ShouldReceiveTouch = (s, args) => true;
        }

        private UILongPressGestureRecognizer CreateLongPressRecognizer(Func<(ICommand? Command, ICommand? PointCommand)> getCommand)
        {
            return new(recognizer =>
            {
                if (recognizer.State == UIGestureRecognizerState.Began)
                {
                    var (command, pointCommand) = getCommand();
                    if (command != null || pointCommand != null)
                    {
                        var control = Control ?? Container;
                        var point = recognizer.LocationInView(control);
                        if (command?.CanExecute(commandParameter) == true)
                            command.Execute(commandParameter);
                        if (pointCommand?.CanExecute(point) == true)
                            pointCommand.Execute(point);
                    }
                }
            })
            {
                Enabled = false,
                ShouldRecognizeSimultaneously = (recognizer, gestureRecognizer) => true,
                //ShouldReceiveTouch = (recognizer, touch) => true,
            };
        }

        private UIImmediatePanGestureRecognizer CreatePanRecognizer(Func<(ICommand? Command, ICommand? PointCommand)> getCommand)
        {
            return new UIImmediatePanGestureRecognizer(recognizer =>
            {
                var (command, pointCommand) = getCommand();
                if (command != null || pointCommand != null)
                {
                    if (recognizer.NumberOfTouches > 1 && recognizer.State != UIGestureRecognizerState.Cancelled && recognizer.State != UIGestureRecognizerState.Ended)
                        return;

                    var control = Control ?? Container;
                    var point = recognizer.LocationInView(control).ToPoint();

                    if (command?.CanExecute(commandParameter) == true)
                        command.Execute(commandParameter);

                    if (pointCommand != null && recognizer.State != UIGestureRecognizerState.Began)
                    {
                        //GestureStatus.Started has already been sent by ShouldBegin. Don't sent it twice.

                        var gestureStatus = recognizer.State switch
                        {
                            UIGestureRecognizerState.Began => GestureStatus.Started,
                            UIGestureRecognizerState.Changed => GestureStatus.Running,
                            UIGestureRecognizerState.Ended => GestureStatus.Completed,
                            UIGestureRecognizerState.Cancelled => GestureStatus.Canceled,
                            _ => GestureStatus.Canceled,
                        };

                        var parameter = new MauiUICollectionView.DragEventArgs(gestureStatus, point);
                        if (pointCommand.CanExecute(parameter))
                            pointCommand.Execute(parameter);
                    }
                }
            })
            {
                Enabled = false,
                ShouldRecognizeSimultaneously = (recognizer, other) => true,
                MaximumNumberOfTouches = 1,
                ShouldBegin = recognizer =>
                {
                    var (command, pointCommand) = getCommand();
                    if (command != null)
                    {
                        if (command.CanExecute(commandParameter))
                            command.Execute(commandParameter);
                        return true;
                    }

                    if (pointCommand != null)
                    {
                        var control = Control ?? Container;
                        var point = recognizer.LocationInView(control).ToPoint();

                        var parameter = new MauiUICollectionView.DragEventArgs(GestureStatus.Started, point);
                        if (pointCommand.CanExecute(parameter))
                            pointCommand.Execute(parameter);
                        if (!parameter.CancelGesture)
                            return true;
                    }

                    return false;
                }
            };
        }

        public void SetCanDrag(UIView view)
        {
            var control = view;

            longPressDetector = CreateLongPressRecognizer(() => (longPressCommand, longPressPointCommand));
            panDetector = CreatePanRecognizer(() => (panCommand, panPointCommand));

            recognizers = new()
            {
                longPressDetector, panDetector,
            };

            foreach (var recognizer in recognizers)
            {
                control.AddGestureRecognizer(recognizer);
                recognizer.Enabled = true;
            }
        }

        public void SetCantDrag(UIView view)
        {
            var control = view;
            foreach (var recognizer in recognizers)
            {
                recognizer.Enabled = false;
                control.RemoveGestureRecognizer(recognizer);
            }
        }
    }
}