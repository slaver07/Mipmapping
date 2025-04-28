using System;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using StbImageSharp;

namespace Mipmap
{
    internal class Mip : GameWindow
    {
        private int _texture;
        private int _vao, _vbo, _ebo;
        private int _shaderProgram;

        private readonly float[] _vertices =
        {
            // positions   // tex coords
            -1f, -1f, 0f, 0f,
             1f, -1f, 1f, 0f,
             1f,  1f, 1f, 1f,
            -1f,  1f, 0f, 1f
        };

        private readonly uint[] _indices = { 0, 1, 2, 2, 3, 0 };

        private int _currentFilterIndex = 0;

        private readonly (TextureMinFilter minFilter, string description)[] _filters =
        {
            // Новый первый пункт — без мипмаппинга
            (TextureMinFilter.Linear, "Original (no mipmapping)"),
            (TextureMinFilter.NearestMipmapNearest, "Nearest Mipmap Nearest"),
            (TextureMinFilter.LinearMipmapNearest, "Linear Mipmap Nearest"),
            (TextureMinFilter.NearestMipmapLinear, "Nearest Mipmap Linear"),
            (TextureMinFilter.LinearMipmapLinear, "Linear Mipmap Linear")
        };

        public Mip(int width, int height)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = (width, height),
                Title = "Mipmapping Example"
            })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.CornflowerBlue);

            _shaderProgram = CreateShaderProgram("Shaders/shader.vert", "Shaders/shader.frag");
            GL.UseProgram(_shaderProgram);

            LoadTexture("test.jpg");
            SetupBuffers();

            Console.WriteLine($"Current Filter: {_filters[_currentFilterIndex].description}");
            ApplyFilter();
        }

        private int CreateShaderProgram(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            CheckProgramLink(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        private void CheckShaderCompile(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation error: {infoLog}");
            }
        }

        private void CheckProgramLink(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Program linking error: {infoLog}");
            }
        }

        private void LoadTexture(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found", path);

            StbImage.stbi_set_flip_vertically_on_load(1);

            byte[] imageBytes = File.ReadAllBytes(path);
            ImageResult image = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);

            if (image == null)
                throw new Exception("Failed to load image.");

            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            if (image.Data == null || image.Data.Length == 0)
                throw new Exception("Image data is empty or corrupted.");

            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              image.Width, image.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                              PixelType.UnsignedByte, ptr);
            }
            finally
            {
                handle.Free();
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private void SetupBuffers()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(_shaderProgram);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            int textureLocation = GL.GetUniformLocation(_shaderProgram, "texture0");
            GL.Uniform1(textureLocation, 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteProgram(_shaderProgram);
            GL.DeleteTexture(_texture);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Space)
            {
                _currentFilterIndex = (_currentFilterIndex + 1) % _filters.Length;
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            // Особое поведение для оригинального изображения:
            if (_currentFilterIndex == 0)
            {
                // Нет мипмаппинга вообще — ставим обычные параметры
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }
            else
            {
                // В остальных случаях разрешаем использовать мипмапы
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)_filters[_currentFilterIndex].minFilter);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            }

            Console.WriteLine($"Current Filter: {_filters[_currentFilterIndex].description}");

            Title = _filters[_currentFilterIndex].description;
        }
    }
}








/*
 using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using StbImageSharp;
using System.Runtime.InteropServices;

namespace Mipmap
{
    internal class Mip : GameWindow
    {
        private int _texture;
        private int _vao, _vbo, _ebo;
        private int _shaderProgram;

        private readonly float[] _vertices =
        {
            // positions   // tex coords
            -1f, -1f,      0f, 0f,
             1f, -1f,      1f, 0f,
             1f,  1f,      1f, 1f,
            -1f,  1f,      0f, 1f
        };

        private readonly uint[] _indices = { 0, 1, 2, 2, 3, 0 };

        public Mip(int width, int height)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = (width, height),
                Title = "Mipmapping Example"
            })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.CornflowerBlue);

            // Загружаем шейдеры
            _shaderProgram = CreateShaderProgram("Shaders/shader.vert", "Shaders/shader.frag");
            GL.UseProgram(_shaderProgram);

            LoadTexture("test.jpg");
            SetupBuffers();
        }

        private int CreateShaderProgram(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            CheckProgramLink(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        private void CheckShaderCompile(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation error: {infoLog}");
            }
        }

        private void CheckProgramLink(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Program linking error: {infoLog}");
            }
        }

        private void LoadTexture(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found", path);

            StbImage.stbi_set_flip_vertically_on_load(1); // <-- добавить эту строчку

            byte[] imageBytes = File.ReadAllBytes(path);

            ImageResult image = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);
            if (image == null)
                throw new Exception("Failed to load image.");

            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            if (image.Data == null || image.Data.Length == 0)
                throw new Exception("Image data is empty or corrupted.");

            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              image.Width, image.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                              PixelType.UnsignedByte, ptr);
            }
            finally
            {
                handle.Free();
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private void SetupBuffers()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(_shaderProgram);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            int textureLocation = GL.GetUniformLocation(_shaderProgram, "texture0");
            GL.Uniform1(textureLocation, 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteProgram(_shaderProgram);
            GL.DeleteTexture(_texture);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
 */





/*
 using System;
using System.IO;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Runtime.InteropServices;

namespace Mipmap
{
    internal class Mip : GameWindow
    {
        private int _texture;
        private int _vao, _vbo, _ebo;
        private int _shaderProgram;

        private readonly float[] _vertices =
        {
            // positions   // tex coords
            -1f, -1f,      0f, 0f,
             1f, -1f,      1f, 0f,
             1f,  1f,      1f, 1f,
            -1f,  1f,      0f, 1f
        };

        private readonly uint[] _indices = { 0, 1, 2, 2, 3, 0 };

        private TextureMinFilter[] _minFilters = new TextureMinFilter[]
        {
            TextureMinFilter.Nearest,
            TextureMinFilter.Linear,
            TextureMinFilter.NearestMipmapNearest,
            TextureMinFilter.LinearMipmapNearest,
            TextureMinFilter.NearestMipmapLinear,
            TextureMinFilter.LinearMipmapLinear
        };

        private string[] _filterNames = new string[]
        {
            "Nearest",
            "Linear",
            "Nearest Mipmap Nearest",
            "Linear Mipmap Nearest",
            "Nearest Mipmap Linear",
            "Linear Mipmap Linear"
        };

        private int _currentFilterIndex = 5; // Linear Mipmap Linear по умолчанию

        public Mip(int width, int height)
            : base(GameWindowSettings.Default, new NativeWindowSettings()
            {
                Size = (width, height),
                Title = "Mipmapping Example"
            })
        { }

        protected override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(Color4.CornflowerBlue);

            _shaderProgram = CreateShaderProgram("Shaders/shader.vert", "Shaders/shader.frag");
            GL.UseProgram(_shaderProgram);

            LoadTexture("test.jpg");
            SetupBuffers();
            UpdateWindowTitle();
        }

        private int CreateShaderProgram(string vertexPath, string fragmentPath)
        {
            string vertexShaderSource = File.ReadAllText(vertexPath);
            string fragmentShaderSource = File.ReadAllText(fragmentPath);

            int vertexShader = GL.CreateShader(ShaderType.VertexShader);
            GL.ShaderSource(vertexShader, vertexShaderSource);
            GL.CompileShader(vertexShader);
            CheckShaderCompile(vertexShader);

            int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(fragmentShader, fragmentShaderSource);
            GL.CompileShader(fragmentShader);
            CheckShaderCompile(fragmentShader);

            int shaderProgram = GL.CreateProgram();
            GL.AttachShader(shaderProgram, vertexShader);
            GL.AttachShader(shaderProgram, fragmentShader);
            GL.LinkProgram(shaderProgram);
            CheckProgramLink(shaderProgram);

            GL.DeleteShader(vertexShader);
            GL.DeleteShader(fragmentShader);

            return shaderProgram;
        }

        private void CheckShaderCompile(int shader)
        {
            GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetShaderInfoLog(shader);
                throw new Exception($"Shader compilation error: {infoLog}");
            }
        }

        private void CheckProgramLink(int program)
        {
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
            if (success == 0)
            {
                string infoLog = GL.GetProgramInfoLog(program);
                throw new Exception($"Program linking error: {infoLog}");
            }
        }

        private void LoadTexture(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Image file not found", path);

            StbImage.stbi_set_flip_vertically_on_load(1);

            byte[] imageBytes = File.ReadAllBytes(path);

            ImageResult image = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);
            if (image == null)
                throw new Exception("Failed to load image.");

            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)_minFilters[_currentFilterIndex]);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            if (image.Data == null || image.Data.Length == 0)
                throw new Exception("Image data is empty or corrupted.");

            var handle = GCHandle.Alloc(image.Data, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();

                GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                              image.Width, image.Height, 0,
                              OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
                              PixelType.UnsignedByte, ptr);
            }
            finally
            {
                handle.Free();
            }

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        }

        private void SetupBuffers()
        {
            _vao = GL.GenVertexArray();
            _vbo = GL.GenBuffer();
            _ebo = GL.GenBuffer();

            GL.BindVertexArray(_vao);

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
            GL.BufferData(BufferTarget.ArrayBuffer, _vertices.Length * sizeof(float), _vertices, BufferUsageHint.StaticDraw);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _ebo);
            GL.BufferData(BufferTarget.ElementArrayBuffer, _indices.Length * sizeof(uint), _indices, BufferUsageHint.StaticDraw);

            GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
            GL.EnableVertexAttribArray(0);

            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
            GL.EnableVertexAttribArray(1);

            GL.BindVertexArray(0);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(_shaderProgram);

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            int textureLocation = GL.GetUniformLocation(_shaderProgram, "texture0");
            GL.Uniform1(textureLocation, 0);

            GL.BindVertexArray(_vao);
            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (KeyboardState.IsKeyPressed(Keys.Space))
            {
                _currentFilterIndex = (_currentFilterIndex + 1) % _minFilters.Length;

                GL.BindTexture(TextureTarget.Texture2D, _texture);
                GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)_minFilters[_currentFilterIndex]);

                UpdateWindowTitle();
            }
        }

        private void UpdateWindowTitle()
        {
            Title = $"Mipmapping Example - MinFilter: {_filterNames[_currentFilterIndex]} (Press Space to change)";
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteProgram(_shaderProgram);
            GL.DeleteTexture(_texture);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}*/