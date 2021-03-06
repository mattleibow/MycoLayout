using Android.Content;
using Android.Graphics;
using Android.Views;
using SkiaSharp;
using System;

namespace Myco.Droid
{
    public class NativeMycoContainer : View
    {
        #region Fields

        private Bitmap _bitmap = null;
        private GestureDetector _detector;
        private MycoGestureListener _listener;
        private MycoContainer _mycoContainer;
        private bool disposed = false;

        #endregion Fields

        #region Constructors

        public NativeMycoContainer(Context context) : base(context)
        {
            _listener = new MycoGestureListener(context);
            _detector = new GestureDetector(_listener);
        }

        ~NativeMycoContainer()
        {
            Dispose(false);
        }

        #endregion Constructors

        #region Properties

        public MycoContainer Container
        {
            get
            {
                return _mycoContainer;
            }

            set
            {
                if (_mycoContainer != null)
                {
                    _mycoContainer.InvalidateNative -= _skContainer_InvalidateNative;
                }

                _mycoContainer = value;
                _listener.Container = value;

                if (_mycoContainer != null)
                {
                    _mycoContainer.InvalidateNative += _skContainer_InvalidateNative;
                }
            }
        }

        #endregion Properties

        #region Methods

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                _detector.Dispose();
                _detector = null;

                _listener.Dispose();
                _listener = null;

                if (_bitmap != null)
                {
                    _bitmap.Recycle();
                    _bitmap.Dispose();
                    _bitmap = null;
                }

                disposed = true;
            }

            base.Dispose(disposing);
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            if (_bitmap == null || _bitmap.Width != canvas.Width || _bitmap.Height != canvas.Height)
            {
                if (_bitmap != null)
                {
                    _bitmap.Recycle();
                    _bitmap.Dispose();
                    _bitmap = null;
                }

                _bitmap = Bitmap.CreateBitmap(canvas.Width, canvas.Height, Bitmap.Config.Argb8888);

                (_mycoContainer as IMycoController).SendSurfaceSize(canvas.Width, canvas.Height);
            }

            try
            {
                using (var surface = SKSurface.Create(canvas.Width, canvas.Height, SKColorType.Rgba_8888, SKAlphaType.Premul, _bitmap.LockPixels(), canvas.Width * 4))
                {
                    var skcanvas = surface.Canvas;
                    skcanvas.Scale(((float)canvas.Width) / (float)_mycoContainer.Width, ((float)canvas.Height) / (float)_mycoContainer.Height);
                    (_mycoContainer as IMycoController).SendDraw(skcanvas);
                }
            }
            finally
            {
                _bitmap.UnlockPixels();
            }

            canvas.DrawBitmap(_bitmap, 0, 0, null);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);

            (_mycoContainer as IMycoController).SendLayout();
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (_detector.OnTouchEvent(e))
            {
                return true;
            }

            if (e.Action == MotionEventActions.Up)
            {
                return _listener.OnUp(e);
            }

            return false;
        }

        public override bool OnGenericMotionEvent(MotionEvent e)
        {
            return _detector.OnTouchEvent(e);
        }

        private void _skContainer_InvalidateNative(object sender, EventArgs e)
        {
            Invalidate();
        }
        

        #endregion Methods
    }
}