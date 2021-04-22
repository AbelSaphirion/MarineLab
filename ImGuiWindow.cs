using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using Veldrid.ImageSharp;
using ImGuiNET;

namespace marine
{
    class ImGuiWindow
    {
        protected static Sdl2Window _window;
        protected static GraphicsDevice _gd;
        private static CommandList _cl;
        private static ImGuiRenderer _ren;
        private static bool _demoWindow;

        public void Init(string name, int width, int height)
        {
            VeldridStartup.CreateWindowAndGraphicsDevice(
                new WindowCreateInfo(50, 50, width, height, WindowState.Normal, name),
                out _window,
                out _gd);

            _cl = _gd.ResourceFactory.CreateCommandList();

            _ren = new ImGuiRenderer(
                _gd,
                _gd.MainSwapchain.Framebuffer.OutputDescription,
                _window.Width,
                _window.Height);
            
            _window.Resized += () =>
            {
                _ren.WindowResized(_window.Width, _window.Height);
                _gd.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
            };

            Preload();

            while (_window.Exists)
            {
                var snapshot = _window.PumpEvents();
                _ren.Update(1f / 60f, snapshot);

                SubmitUI();

                _cl.Begin();
                _cl.SetFramebuffer(_gd.MainSwapchain.Framebuffer);
                _cl.ClearColorTarget(0, new RgbaFloat(0, 0, 0.2f, 1f));
                _ren.Render(_gd, _cl); 
                _cl.End();
                _gd.SubmitCommands(_cl);
                _gd.SwapBuffers(_gd.MainSwapchain);
            }
        }
        protected virtual void SubmitUI()
        {
            ImGui.ShowDemoWindow(ref _demoWindow);
        }

        protected virtual void Preload()
        {

        }

        protected System.IntPtr NewImage(string path)
        {
            var ist = new ImageSharpTexture(path);
            Texture t = ist.CreateDeviceTexture(_gd, _gd.ResourceFactory);
            //_newImguiTex.Add(path, t);
            return _ren.GetOrCreateImGuiBinding(_gd.ResourceFactory, t);
        }
    }
}
