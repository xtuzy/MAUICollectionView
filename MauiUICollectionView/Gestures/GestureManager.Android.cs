using Android.Content;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Microsoft.Maui.Controls;
using System.Diagnostics;
using System.Windows.Input;
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
        private DisplayMetrics displayMetrics;
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
                        var x = motionEvent.GetX();
                        var y = motionEvent.GetY();

                        var point = PxToDp(new Point(x, y));
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
                    }

                    Debug.WriteLine("Drag");
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
            point.X /= displayMetrics.Density;
            point.Y /= displayMetrics.Density;
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
            displayMetrics = context.Resources.DisplayMetrics;
            allInOneDetector.Density = displayMetrics.Density;

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
            displayMetrics = null;
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
            public Action<MotionEvent?, SelectStatus>? SelectAction { get; set; }

            public float Density { get; set; }

            bool startDrag = false;
            SelectStatus selectStatus = SelectStatus.CancelWillSelect;
            public override void OnLongPress(MotionEvent? e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.CancelWillSelect;
                    SelectAction.Invoke(e, selectStatus);//长按代表不选择
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
                //不管怎么样, Down都代表可能Select, 在CollectionView里如果作用在Item上需要显示动画
                selectStatus = SelectStatus.WillSelect;
                SelectAction.Invoke(e, selectStatus);

                return false;
            }

            public override bool OnSingleTapConfirmed(MotionEvent e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.Selected;
                    SelectAction.Invoke(e, selectStatus);//没有取消时, Tab代表确认选择
                }
                return base.OnSingleTapConfirmed(e);
            }

            public void OnMove(MotionEvent? e)
            {
                if (selectStatus == SelectStatus.WillSelect)
                {
                    selectStatus = SelectStatus.CancelWillSelect;
                    SelectAction.Invoke(e, selectStatus);//没有确认选择时Move代表取消选择
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
                    SelectAction.Invoke(e, selectStatus);//没有取消时, Up代表确认选择
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