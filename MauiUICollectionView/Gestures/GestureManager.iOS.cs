using Foundation;
using Microsoft.Maui.Platform;
using System.Diagnostics;
using System.Windows.Input;
using UIKit;

namespace MauiUICollectionView.Gestures
{
    public class GestureManager : IGestureManager
    {
        private UISelectGestureRecognizer tapDetector;
        private UILongPressGestureRecognizer longPressDetector;
        private UIImmediatePanGestureRecognizer panDetector;
        private List<UIGestureRecognizer> recognizers;

        /// <summary>
        /// Take a Point parameter
        /// Except panPointCommand which takes a (Point,GestureStatus) parameter (its a tuple) 
        /// </summary>
        public ICommand? SelectPointCommand { get; set; }
        public ICommand? DragPointCommand { get; set; }
        public ICommand? LongPressPointCommand { get; set; }

        private object commandParameter;

        public GestureManager()
        {
            //if (!allSubviews)
            //    tapDetector.ShouldReceiveTouch = (s, args) => args.View != null && (args.View == view || view.Subviews.Any(v => v == args.View));
            //else
            //    tapDetector.ShouldReceiveTouch = (s, args) => true;
        }

        private UISelectGestureRecognizer CreateTapRecognizer(ICommand? PointCommand)
        {
            return new(recognizer =>
            {
                var selectPointCommand = PointCommand;
                if (selectPointCommand != null)
                {
                    var control = view as UIScrollView;
                    var point = recognizer.LocationInView(control);
                    var parameters = new SelectEventArgs(SelectStatus.Selected, new Point(point.X, point.Y - control.ContentOffset.Y));
                    Debug.WriteLine("tab");
                    if (selectPointCommand?.CanExecute(parameters) == true)
                        selectPointCommand.Execute(parameters);
                }
            })
            {
                Enabled = false,
                ShouldRecognizeSimultaneously = (recognizer, gestureRecognizer) => true,
                ShouldReceiveTouch = ShouldReceiveTouch,
                SelectAction =new WeakReference<Action<UITouch, SelectStatus>?>((touch, statue) =>
                {
                    var control = view as UIScrollView;
                    var point = touch.LocationInView(view);//���صĻ��������content��
                    //Debug.WriteLine(statue);
                    var parameters = new SelectEventArgs(statue, new Point(point.X, point.Y - control.ContentOffset.Y));
                    if (SelectPointCommand?.CanExecute(parameters) == true)
                        SelectPointCommand.Execute(parameters);
                })
            };
        }

        /// <summary>
        /// https://github.com/dotnet/maui/blob/9d4be10c63791bb8c2d6b8c697be08f6e1ab4e26/src/Controls/src/Core/Platform/GestureManager/GesturePlatformManager.iOS.cs#L677
        /// </summary>
        /// <param name="recognizer"></param>
        /// <param name="touch"></param>
        /// <returns></returns>
        bool ShouldReceiveTouch(UIGestureRecognizer recognizer,UITouch touch)
        {
            if (virtualView.InputTransparent)
            {
                return false;
            }

            if (touch.View == view)
            {
                return true;
            }

            // 是否是控件, 控件自己能处理
            if (touch.View is UIControl || touch.View is UITextView || touch.View is UITextField)
            {
                return false;
            }
            
            // 子视图有手势能处理
            if (touch.View.IsDescendantOfView(view) &&//是否是子视图
                touch.View.GestureRecognizers?.Length > 0)//子视图带有手势
            {
                return false;
            }

            return true;
        } 

        private UILongPressGestureRecognizer CreateLongPressRecognizer(ICommand? PointCommand)
        {
            return new(recognizer =>
            {
                //Debug.WriteLine("Drag:" + recognizer.State);
                var gestureStatus = recognizer.State switch
                {
                    UIGestureRecognizerState.Began => GestureStatus.Started,
                    UIGestureRecognizerState.Changed => GestureStatus.Running,
                    UIGestureRecognizerState.Ended => GestureStatus.Completed,
                    UIGestureRecognizerState.Cancelled => GestureStatus.Canceled,
                    _ => GestureStatus.Canceled,
                };
                //Debug.WriteLine("Drag gestureStatus:" + gestureStatus);
                var dragPointCommand = PointCommand;
                if (dragPointCommand != null)
                {
                    var control = view as UIScrollView;
                    var point = recognizer.LocationInView(control); 
                    var parameter = new MauiUICollectionView.DragEventArgs(gestureStatus, new Point(point.X, point.Y- control.ContentOffset.Y));
                    parameter.Device = GestureDevice.Touch;
                    if (dragPointCommand?.CanExecute(parameter) == true)
                        dragPointCommand.Execute(parameter);
                }
            })
            {
                Enabled = false,
                ShouldRecognizeSimultaneously = (recognizer, gestureRecognizer) => true,
                //ShouldReceiveTouch = (recognizer, touch) => true,
            };
        }

        private UIImmediatePanGestureRecognizer CreatePanRecognizer(ICommand? PointCommand)
        {
            return new UIImmediatePanGestureRecognizer(recognizer =>
            {
                //Debug.WriteLine("Pan:" + recognizer.State);
                var dragPointCommand = PointCommand;
                if (dragPointCommand != null)
                {
                    if (recognizer.NumberOfTouches > 1 && recognizer.State != UIGestureRecognizerState.Cancelled && recognizer.State != UIGestureRecognizerState.Ended)
                        return;

                    var control = view;
                    var point = recognizer.LocationInView(control).ToPoint();

                    if (dragPointCommand != null && recognizer.State != UIGestureRecognizerState.Began)
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
                        //if (dragPointCommand.CanExecute(parameter))
                        //dragPointCommand.Execute(parameter);
                    }
                }
            })
            {
                Enabled = false,
                ShouldRecognizeSimultaneously = (recognizer, other) => true,
                MaximumNumberOfTouches = 1,
                ShouldBegin = recognizer =>
                {
                    var pointCommand = PointCommand;

                    if (pointCommand != null)
                    {
                        var control = view;
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

        View virtualView;
        UIView view { get; set; }
        public void SubscribeGesture(View view)
        {
            var platformView = view.Handler.PlatformView as UIView;
            this.virtualView = view;
            this.view = platformView;

            var control = platformView;

            tapDetector = CreateTapRecognizer(SelectPointCommand);
            longPressDetector = CreateLongPressRecognizer(DragPointCommand);
            //panDetector = CreatePanRecognizer(DragPointCommand);

            recognizers = new()
            {
                tapDetector, longPressDetector,// panDetector,
            };

            foreach (var recognizer in recognizers)
            {
                control.AddGestureRecognizer(recognizer);
                recognizer.Enabled = true;
            }
        }

        public void CancleSubscribeGesture()
        {
            var control = view;
            foreach (var recognizer in recognizers)
            {
                recognizer.Enabled = false;
                control.RemoveGestureRecognizer(recognizer);
            }
        }

        bool IsSupportDrag;
        public void SetCanDrag(bool can = false)
        {
            IsSupportDrag = can;
        }

        internal class UISelectGestureRecognizer : UITapGestureRecognizer
        {
            public WeakReference<Action<UITouch, SelectStatus>?> SelectAction { get; set; }

            public UISelectGestureRecognizer()
            {
            }

            public UISelectGestureRecognizer(Action action) : base(action)
            {
            }

            public UISelectGestureRecognizer(Action<UITapGestureRecognizer> action) : base(action)
            {
            }

            [Preserve]
            protected internal UISelectGestureRecognizer(IntPtr handle) : base(handle)
            {
            }

            SelectStatus selectStatus = SelectStatus.CancelWillSelect;
            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                Debug.WriteLine("down");
                var touch = touches.AnyObject as UITouch;
                selectStatus = SelectStatus.WillSelect;
                var exist = SelectAction.TryGetTarget(out var selectAction);
                if(exist) selectAction.Invoke(touch, selectStatus);
                base.TouchesBegan(touches, evt);
            }

            public override void TouchesMoved(NSSet touches, UIEvent evt)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    Debug.WriteLine("move");
                    selectStatus = SelectStatus.CancelWillSelect;
                    var touch = touches.AnyObject as UITouch;
                    var exist = SelectAction.TryGetTarget(out var selectAction);
                    if (exist) selectAction.Invoke(touch, selectStatus);
                }
                base.TouchesMoved(touches, evt);
            }

            public override void TouchesCancelled(NSSet touches, UIEvent evt)
            {
                Debug.WriteLine("cancel");
                base.TouchesCancelled(touches, evt);
            }

            public override void TouchesEnded(NSSet touches, UIEvent evt)
            {
                Debug.WriteLine("end");
                base.TouchesEnded(touches, evt);
            }
        }

        internal class UIImmediatePanGestureRecognizer : UIPanGestureRecognizer
        {
            public bool IsImmediate { get; set; } = false;

            public UIImmediatePanGestureRecognizer()
            {
            }

            public UIImmediatePanGestureRecognizer(Action action) : base(action)
            {
            }

            public UIImmediatePanGestureRecognizer(Action<UIPanGestureRecognizer> action) : base(action)
            {
            }

            [Preserve]
            protected internal UIImmediatePanGestureRecognizer(IntPtr handle) : base(handle)
            {
            }

            public override void TouchesBegan(NSSet touches, UIEvent evt)
            {
                base.TouchesBegan(touches, evt);
                if (IsImmediate)
                    State = UIGestureRecognizerState.Began;
            }
        }
    }
}