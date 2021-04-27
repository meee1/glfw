
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using Xamarin.Forms.Platform.WinForms;
using System.Runtime.InteropServices;
using System.Threading;
using Xamarin.Forms.Internals;

namespace Glfw.Skia
{
    using System;
    using System.Drawing;
    using System.Runtime.InteropServices;
    using GLFW;
    using SkiaSharp;
    using KeyEventArgs = GLFW.KeyEventArgs;
    using Keys = GLFW.Keys;

    class Program
    {
        private static NativeWindow window;
        private static SKCanvas canvas;

        private static Keys? lastKeyPressed;
        private static Point? lastMousePosition;
        private static bool start;
        private static Thread winforms;

        //----------------------------------
        //NOTE: On Windows you must copy SharedLib manually (https://github.com/ForeverZer0/glfw-net#microsoft-windows)
        //----------------------------------

        static void Main(string[] args)
        {
            using (Program.window = new NativeWindow(960, 540+23, "Skia Example"))
            {
                Program.SubscribeToWindowEvents();

                using (var context = Program.GenerateSkiaContext(Program.window))
                {
                    using (var skiaSurface = Program.GenerateSkiaSurface(context, Program.window.ClientSize))
                    {
                        new System.Drawing.android.android();

                        while (!Program.window.IsClosing)
                        {
                            Program.Render(skiaSurface, Program.window.ClientSize.Width, Program.window.ClientSize.Height);
                            Program.window.SwapBuffers();
                            Glfw.WaitEvents();
                        }
                    }
                }
            }
        }

        private static void SubscribeToWindowEvents()
        {
            Program.window.SizeChanged += Program.OnWindowsSizeChanged;
            Program.window.Refreshed += Program.OnWindowRefreshed;
            Program.window.KeyPress += Program.OnWindowKeyPress;
            Program.window.MouseMoved += Program.OnWindowMouseMoved;
            Program.window.MouseButton += Program.OnMouseButton;
        }

        private static void OnMouseButton(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left)
            {
                if (e.Action == InputState.Press)
                {
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_MOUSEMOVE, IntPtr.Zero,
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_LBUTTONDOWN, new IntPtr((int) MsgButtons.MK_LBUTTON),
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
                }

                if (e.Action == InputState.Release)
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_LBUTTONUP, new IntPtr((int) MsgButtons.MK_LBUTTON),
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
            }   
            if (e.Button == MouseButton.Right)
            {
                if (e.Action == InputState.Press)
                {
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_MOUSEMOVE, IntPtr.Zero,
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_RBUTTONDOWN, new IntPtr((int) MsgButtons.MK_RBUTTON),
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
                }

                if (e.Action == InputState.Release)
                    XplatUIMine.GetInstance()
                        .SendMessage(IntPtr.Zero, Msg.WM_RBUTTONUP, new IntPtr((int) MsgButtons.MK_RBUTTON),
                            new IntPtr(Program.lastMousePosition.Value.X + (Program.lastMousePosition.Value.Y << 16)));
            }
        }

        private static GRContext GenerateSkiaContext(NativeWindow nativeWindow)
        {
            var nativeContext = Program.GetNativeContext(nativeWindow);
            var glInterface = GRGlInterface.AssembleGlInterface(nativeContext, (contextHandle, name) => Glfw.GetProcAddress(name));
            return GRContext.Create(GRBackend.OpenGL, glInterface);
        }

        private static object GetNativeContext(NativeWindow nativeWindow)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Native.GetWglContext(nativeWindow);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // XServer
                return Native.GetGLXContext(nativeWindow);
                // Wayland
                //return Native.GetEglContext(nativeWindow);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Native.GetNSGLContext(nativeWindow);
            }

            throw new PlatformNotSupportedException();
        }

        private static SKSurface GenerateSkiaSurface(GRContext skiaContext, Size surfaceSize)
        {
            var frameBufferInfo = new GRGlFramebufferInfo((uint)new UIntPtr(0), GRPixelConfig.Rgba8888.ToGlSizedFormat());
            var backendRenderTarget = new GRBackendRenderTarget(surfaceSize.Width,
                                                                surfaceSize.Height,
                                                                0,
                                                                8,
                                                                frameBufferInfo);
            return SKSurface.Create(skiaContext, backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKImageInfo.PlatformColorType);
        }

        private static void Render(SKSurface skiaSurface, in int clientSizeWidth, in int clientSizeHeight)
        {
            {
                if (!start)
                {
                    StartThreads();

                    XplatUIMine.GetInstance().Keyboard = new Keyboard();
                    start = true;
                }

                
            try
            {
                
                var size = Program.window.ClientSize;

                XplatUIMine.GetInstance()._virtualScreen = new Rectangle(0, 0, (int) size.Width, (int) size.Height);
                XplatUIMine.GetInstance()._workingArea = new Rectangle(0, 0, (int) size.Width, (int) size.Height);

                var surface =skiaSurface;

                surface.Canvas.Clear(SKColors.Gray);

                surface.Canvas.DrawCircle(0, 0, 50, new SKPaint() {Color = SKColor.Parse("ff0000")});

                //surface.Canvas.Scale((float) scale.Width, (float) scale.Height);

                foreach (Form form in Application.OpenForms.OfType<Form>().Select(a=>a).ToArray())
                {
                    if (form.IsHandleCreated)
                    {
                        if (form is MainV2 && form.WindowState != FormWindowState.Maximized)
                            form.BeginInvokeIfRequired(() => { form.WindowState = FormWindowState.Maximized; });

                        if(form is MainV2 &&size.Width != form.Width)
                            form.BeginInvokeIfRequired(() => { form.Width = size.Width; });

                        try
                        {
                            DrawOntoSurface(form.Handle, surface);
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                    }
                }

                IEnumerable<Hwnd> menu;
                lock(Hwnd.windows)
                    menu = Hwnd.windows.Values.OfType<Hwnd>()
                    .Where(hw => hw.topmost && hw.Mapped && hw.Visible).ToArray();
                foreach (Hwnd hw in menu)
                {
                    if (hw.topmost && hw.Mapped && hw.Visible)
                    {
                        var ctlmenu = Control.FromHandle(hw.ClientWindow);
                        if (ctlmenu != null)
                            DrawOntoSurface(hw.ClientWindow, surface);
                    }
                }


                surface.Canvas.DrawText("" + DateTime.Now.ToString("HH:mm:ss.fff"),
                    new SKPoint(10, 10), new SKPaint() {Color = SKColor.Parse("ffff00")});

                surface.Canvas.Flush();

            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
               
            }

            return;
            }
            {
                //Program.canvas.Clear(SKColor.Parse("#F0F0F0"));
                var headerPaint = new SKPaint {Color = SKColor.Parse("#333333"), TextSize = 50, IsAntialias = true};
                Program.canvas.DrawText("Hello from GLFW.NET + SkiaSharp!", 10, 60, headerPaint);

                var inputInfoPaint = new SKPaint {Color = SKColor.Parse("#F34336"), TextSize = 18, IsAntialias = true};
                Program.canvas.DrawText($"Last key pressed: {Program.lastKeyPressed}", 10, 90, inputInfoPaint);
                Program.canvas.DrawText($"Last mouse position: {Program.lastMousePosition}", 10, 120, inputInfoPaint);

                var exitInfoPaint = new SKPaint {Color = SKColor.Parse("#3F51B5"), TextSize = 18, IsAntialias = true};
                Program.canvas.DrawText("Press Enter to Exit.", 10, 160, exitInfoPaint);

                Program.canvas.Flush();
                Program.window.SwapBuffers();
            }
        }


        public class test : ISystemResourcesProvider
        {
            public IResourceDictionary GetSystemResources()
            {
                return new res();
            }
        }

        public class res : Dictionary<string, object>,  IResourceDictionary
        {
            public event EventHandler<ResourcesChangedEventArgs> ValuesChanged;
        }

                private static void StartThreads()
                {
                    var size = Program.window.ClientSize;

                    XplatUIMine.GetInstance()._virtualScreen = new Rectangle(0, 0, (int) size.Width, (int) size.Height);
            XplatUIMine.GetInstance()._workingArea = new Rectangle(0, 0, (int) size.Width, (int) size.Height);

            winforms = new Thread(() =>
            {
                var init = true;

                Application.Idle += (sender, args) =>
                {
                    if (MainV2.instance != null && MainV2.instance.IsHandleCreated)
                    {
                        if (init)
                        {
                            //Device.BeginInvokeOnMainThread(() => { InitDevice?.Invoke(); });
                            init = false;
                        }
                    }

                    Thread.Sleep(0);
                };

                Xamarin.Forms.DependencyService.Register<ISystemResourcesProvider, test>();

                MissionPlanner.Program.Main(new string[0]);
                
                System.Diagnostics.Process.GetCurrentProcess().CloseMainWindow();
            });
            winforms.Start();
            /*
            Forms.Device.StartTimer(TimeSpan.FromMilliseconds(1000/30), () =>
            {
                Monitor.Enter(XplatUIMine.paintlock);
                if (XplatUIMine.PaintPending)
                {
                    if (Instance.SkCanvasView != null)
                    {
                        Instance.scale = new Forms.Size((Instance.SkCanvasView.CanvasSize.Width / Instance.size.Width),
                            (Instance.SkCanvasView.CanvasSize.Height / Instance.size.Height));

                        XplatUIMine.GetInstance()._virtualScreen =
                            new Rectangle(0, 0, (int) Instance.size.Width, (int) Instance.size.Height);
                        XplatUIMine.GetInstance()._workingArea =
                            new Rectangle(0, 0, (int) Instance.size.Width, (int) Instance.size.Height);

                        Device.BeginInvokeOnMainThread(() => { Instance.SkCanvasView.InvalidateSurface(); });
                        XplatUIMine.PaintPending = false;
                    }
                }
                Monitor.Exit(XplatUIMine.paintlock);

                return true;
            });
            */
        }
                private static SKPaint paint = new SKPaint() {FilterQuality = SKFilterQuality.Low};

                        private static  bool DrawOntoSurface(IntPtr handle, SKSurface surface)
        {
            var hwnd = Hwnd.ObjectFromHandle(handle);

            var x = 0;
            var y = 0;

            XplatUI.driver.ClientToScreen(hwnd.client_window, ref x, ref y);

            var width = 0;
            var height = 0;
            var client_width = 0;
            var client_height = 0;


            if (hwnd.hwndbmp != null && hwnd.Mapped && hwnd.Visible && !hwnd.zombie)
            {
                // setup clip
                var parent = hwnd;
                surface.Canvas.ClipRect(
                    SKRect.Create(0, 0, Screen.PrimaryScreen.Bounds.Width,
                        Screen.PrimaryScreen.Bounds.Height), (SKClipOperation) 5);

                while (parent != null)
                {
                    var xp = 0;
                    var yp = 0;
                    XplatUI.driver.ClientToScreen(parent.client_window, ref xp, ref yp);

                    surface.Canvas.ClipRect(SKRect.Create(xp, yp, parent.Width, parent.Height),
                        SKClipOperation.Intersect);
                    /*
                    surface.Canvas.DrawRect(xp, yp, parent.Width, parent.Height,
                        new SKPaint()
                        {

                            Color = new SKColor(255, 0, 0),
                            Style = SKPaintStyle.Stroke


                        });
                    */
                    parent = parent.parent;
                }

                System.Threading.Monitor.Enter(XplatUIMine.paintlock);

                if (hwnd.ClientWindow != hwnd.WholeWindow)
                {
                    var frm = Control.FromHandle(hwnd.ClientWindow) as Form;

                    Hwnd.Borders borders = new Hwnd.Borders();

                    if (frm != null)
                    {
                        borders = Hwnd.GetBorders(frm.GetCreateParams(), null);

                        surface.Canvas.ClipRect(
                            SKRect.Create(0, 0, Screen.PrimaryScreen.Bounds.Width,
                                Screen.PrimaryScreen.Bounds.Height), (SKClipOperation) 5);
                    }

                    if (surface.Canvas.DeviceClipBounds.Width > 0 &&
                        surface.Canvas.DeviceClipBounds.Height > 0)
                    {
                        if (hwnd.hwndbmpNC != null)
                            surface.Canvas.DrawImage(hwnd.hwndbmpNC,
                                new SKPoint(x - borders.left, y - borders.top), paint);

                        surface.Canvas.ClipRect(
                            SKRect.Create(x, y, hwnd.width - borders.right - borders.left,
                                hwnd.height - borders.top - borders.bottom), SKClipOperation.Intersect);

                        surface.Canvas.DrawImage(hwnd.hwndbmp,
                            new SKPoint(x, y), paint);

                    }
                    else
                    {
                        System.Threading.Monitor.Exit(XplatUIMine.paintlock);
                        return true;
                    }
                }
                else
                {
                    if (surface.Canvas.DeviceClipBounds.Width > 0 &&
                        surface.Canvas.DeviceClipBounds.Height > 0)
                    {

                        surface.Canvas.DrawImage(hwnd.hwndbmp,
                            new SKPoint(x + 0, y + 0), paint);

                        /*surface.Canvas.DrawText(hwnd.ClientWindow.ToString(), new SKPoint(x,y+15),
                            new SKPaint() {Color = SKColor.Parse("ffff00")});*/

                    }
                    else
                    {
                        System.Threading.Monitor.Exit(XplatUIMine.paintlock);
                        return true;
                    }
                }

                System.Threading.Monitor.Exit(XplatUIMine.paintlock);
            }
            //surface.Canvas.DrawText(x + " " + y, x, y+10, new SKPaint() { Color =  SKColors.Red});

            if (hwnd.Mapped && hwnd.Visible)
            {
                IEnumerable<Hwnd> children;
                lock (Hwnd.windows)
                    children = Hwnd.windows.OfType<System.Collections.DictionaryEntry>()
                        .Where(hwnd2 =>
                        {
                            var Key = (IntPtr) hwnd2.Key;
                            var Value = (Hwnd) hwnd2.Value;
                            if (Value.ClientWindow == Key && Value.Parent == hwnd && Value.Visible &&
                                Value.Mapped && !Value.zombie)
                                return true;
                            return false;
                        }).Select(a => (Hwnd) a.Value).ToArray();

                children = children.OrderBy((hwnd2) =>
                {
                    var info = XplatUIMine.GetInstance().GetZOrder(hwnd2.client_window);
                    if (info.top)
                        return 1000;
                    if (info.bottom)
                        return 0;
                    return 500;

                });

                foreach (var child in children)
                {
                    DrawOntoSurface(child.ClientWindow, surface);
                }
            }

            return true;
        }



        public class Keyboard : KeyboardXplat
        {
            public void FocusIn(IntPtr focusWindow)
            {

            }

            public void FocusOut(IntPtr focusWindow)
            {

            }

            public void SetCaretPos(CaretStruct caret, IntPtr handle, int x, int y)
            {

            }
        }


        #region Window Events Handlers

        private static void OnWindowsSizeChanged(object sender, SizeChangeEventArgs e)
        {
            //Program.Render();

        }

        private static void OnWindowKeyPress(object sender, KeyEventArgs e)
        {
            Program.lastKeyPressed = e.Key;
            if (e.Key == Keys.Enter || e.Key == Keys.NumpadEnter)
            {
               // Program.window.Close();
            }
        }

        private static void OnWindowMouseMoved(object sender, MouseMoveEventArgs e)
        {
            Program.lastMousePosition = e.Position;

            XplatUIMine.GetInstance()
                .SendMessage(IntPtr.Zero, Msg.WM_MOUSEMOVE, IntPtr.Zero,
                    new IntPtr(e.Position.X + (e.Position.Y << 16)));
        }

        private static void OnWindowRefreshed(object sender, EventArgs e)
        {
            //Program.Render();
            
        }

        #endregion
    }
}