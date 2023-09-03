using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Windows.Input;
using static Android.Views.View;
using AView = Android.Views.View;

namespace MauiUICollectionView.Gestures
{
    /// <summary>
    /// 用于管理CollectonView在Android上的手势. Maui的手势处理不能提供细节, 而处理好CollectionView的长按拖拽,长按ContextMenu,点击选择之间的关系,都需要细节.
    /// 1. 长按拖拽,长按ContextMenu是互斥的, 需要Tag标识当前使用谁.
    /// 2. 点击选择需要从Down动作开始, 因为动画是从Down开始的, 所以不从Tab事件中执行, 遇到Move
    /// </summary>
    public class GestureManager : IGestureManager
    {
        private GestureDetector? gestureRecognizer;
        private readonly InternalGestureDetector allInOneDetector;
        private double displayDensity;
        private object commandParameter;

        /// <summary>
        /// Take a Point parameter
        /// Except DragPointCommand which takes a DragEventArgs parameter 
        /// </summary>
        public ICommand? SelectPointCommand { get; set; }
        public ICommand?  DragPointCommand { get; set; }
        public ICommand? LongPressPointCommand { get; set; }

        public GestureManager()
        {
            allInOneDetector = new()
            {
                SelectAction = (motionEvent, status) =>
                {
                    if (SelectPointCommand != null)
                    {
                        var point = PxToDp(motionEvent);
                        var parameter = new SelectEventArgs(status, point);
                        if (SelectPointCommand.CanExecute(parameter))
                            SelectPointCommand.Execute(parameter);
                    }
                },

                DragAction = (initialDown, currentMove) =>
                {
                    var continueGesture = true;

                    if (DragPointCommand != null)
                    {
                        var x = currentMove.GetX();
                        var y = currentMove.GetY();
                        var point = PxToDp(new(x, y));

                        var status = currentMove.Action switch
                        {
                            MotionEventActions.Down => GestureStatus.Started,
                            MotionEventActions.Move => GestureStatus.Running,
                            MotionEventActions.Up => GestureStatus.Completed,
                            MotionEventActions.Cancel => GestureStatus.Canceled,
                            _ => GestureStatus.Canceled
                        };


                        var parameter = new DragEventArgs(status, point);
                        parameter.Device = currentMove.GetToolType(0) switch
                        {
                            MotionEventToolType.Mouse => GestureDevice.Mouse,
                            _ => GestureDevice.Touch
                        };
                        if (DragPointCommand.CanExecute(parameter))
                            DragPointCommand.Execute(parameter);
                        if (parameter.CancelGesture)
                            continueGesture = false;
                        if(status == GestureStatus.Completed ||
                            status == GestureStatus.Canceled)
                        {
                            SetScrollViewNotInterceptEventWhenDragFinish();
                        }
                    }

                    //Debug.WriteLine("Drag");
                    return continueGesture;
                },

                LongPressAction = motionEvent =>
                {
                    if (LongPressPointCommand != null)
                    {
                        var x = motionEvent.GetX();
                        var y = motionEvent.GetY();

                        var point = PxToDp(new Point(x, y));
                        if (LongPressPointCommand.CanExecute(point))
                            LongPressPointCommand.Execute(point);
                    }
                },
            };
        }

        private Point PxToDp(Point point)
        {
            point.X /= displayDensity;
            point.Y /= displayDensity;
            return point;
        }

        Microsoft.Maui.Controls.View virtualView;
        AView view;
        public void SubscribeGesture(Microsoft.Maui.Controls.View view)
        {
            var platformView = view.Handler.PlatformView as AView;
            this.virtualView = view;
            this.view = platformView;
            var context = platformView.Context;
            displayDensity = DeviceDisplay.Current.MainDisplayInfo.Density;

            if (gestureRecognizer == null)
                gestureRecognizer = new ExtendedGestureDetector(context, allInOneDetector);

            platformView.Touch += ControlOnTouch;
            platformView.Clickable = true;
        }

        public void SetCanDrag(bool can = false)
        {
            allInOneDetector.IsSupportDrag = can;
        }

        private void ControlOnTouch(object sender, AView.TouchEventArgs touchEventArgs)
        {
            System.Diagnostics.Debug.WriteLine($"{sender} GestureManager OnTouch {touchEventArgs.Event.Action}");

            gestureRecognizer?.OnTouchEvent(touchEventArgs.Event);
            touchEventArgs.Handled = false;
        }

        public void CancleSubscribeGesture()
        {
            var control = view;
            control.Touch -= ControlOnTouch;

            var g = gestureRecognizer;
            gestureRecognizer = null;
            g?.Dispose();
        }

        /// <summary>
        /// when longpress be handled by item, scrollview don't get event info, if we want handle drag in scrollview, we must intercept move/up event.
        /// </summary>
        public void SetScrollViewInterceptEventWhenViewHolderHandledLongPress()
        {
            System.Diagnostics.Debug.WriteLine($"Item LongPress");

            //notify native scrollview intercept event
            (view as MyScrollView).InterceptEvent = true;
            (view as MyScrollView).callback += upEvent;
            allInOneDetector.startDrag = true;
        }

        void upEvent(MotionEvent e)
        {
            allInOneDetector.DragAction(e, e);
        }

        /// <summary>
        /// we notify native scrollview don't intercept event
        /// </summary>
        void SetScrollViewNotInterceptEventWhenDragFinish()
        {
            var scrollView = view as MyScrollView;
            if (scrollView != null)
            {
                scrollView.InterceptEvent = false;
                scrollView.callback -= upEvent;
            }
        }

        sealed class ExtendedGestureDetector : GestureDetector
        {
            private readonly IExtendedGestureListener? myGestureListener;

            private ExtendedGestureDetector(IntPtr javaRef, JniHandleOwnership transfer) : base(javaRef, transfer)
            {
            }

            public ExtendedGestureDetector(Context context, IOnGestureListener listener) : base(context, listener)
            {
                if (listener is IExtendedGestureListener my)
                    myGestureListener = my;
            }

            public override bool OnTouchEvent(MotionEvent? e)
            {
                //Debug.WriteLine("Touch");
                if (myGestureListener != null && e?.Action == MotionEventActions.Up)
                    myGestureListener.OnUp(e);
                else if (myGestureListener != null && e?.Action == MotionEventActions.Move)
                {
                    myGestureListener.OnMove(e);
                }
                return base.OnTouchEvent(e);
            }
        }

        interface IExtendedGestureListener
        {
            void OnUp(MotionEvent? e);
            void OnMove(MotionEvent? e);
        }

        sealed class InternalGestureDetector : GestureDetector.SimpleOnGestureListener, IExtendedGestureListener
        {
            public bool IsSupportDrag { get; set; } = false;

            public Func<MotionEvent, MotionEvent?, bool>? DragAction { get; set; }
            public Action<MotionEvent?>? LongPressAction { get; set; }
            public Action<Point, SelectStatus>? SelectAction { get; set; }

            internal bool startDrag = false;
            SelectStatus selectStatus = SelectStatus.CancelWillSelect;
            Point selectPressPoint;
            public override void OnLongPress(MotionEvent? e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.CancelWillSelect;
                    SelectAction.Invoke(new Point(e.GetX(), e.GetY()), selectStatus);//长按代表不选择
                }

                if (!IsSupportDrag)
                {
                    LongPressAction?.Invoke(e);
                }
                else
                {
                    Debug.WriteLine("Maybe Start Drag");
                    startDrag = true;
                    DragAction?.Invoke(e, e);
                }
            }


            public override bool OnDown(MotionEvent? e)
            {
                selectPressPoint = new Point(e.GetX(), e.GetY());
                //不管怎么样, Down都代表可能Select, 在CollectionView里如果作用在Item上需要显示动画
                selectStatus = SelectStatus.WillSelect;
                SelectAction.Invoke(selectPressPoint, selectStatus);

                return false;
            }

            public override bool OnSingleTapConfirmed(MotionEvent e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.Selected;
                    SelectAction.Invoke(new Point(e.GetX(), e.GetY()), selectStatus);//没有取消时, Tab代表确认选择
                }
                return base.OnSingleTapConfirmed(e);
            }

            public void OnMove(MotionEvent? e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    Debug.WriteLine("move");
                    if (Math.Abs(e.GetX() - selectPressPoint.X) > 2
                    || Math.Abs(e.GetY() - selectPressPoint.Y) > 2)//after down maybe have a little move event, even you think you don't move hand.
                    {
                        selectStatus = SelectStatus.CancelWillSelect;
                        SelectAction.Invoke(selectPressPoint, selectStatus);//没有确认选择时Move代表取消选择;use old position avoid get other indexpath
                    }
                }

                if (e != null && IsSupportDrag && startDrag)
                {
                    DragAction?.Invoke(e, e);
                }
            }

            public void OnUp(MotionEvent? e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.Selected;
                    SelectAction.Invoke(selectPressPoint, selectStatus);//没有取消时, Up代表确认选择
                }

                if (e != null && IsSupportDrag && startDrag)
                {
                    startDrag = false;
                    DragAction?.Invoke(e, e);
                }
            }
        }
    }
}