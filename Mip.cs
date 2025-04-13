using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenTK.Audio.OpenAL;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

using System.Drawing;
using System.Drawing.Imaging;

using System.IO;
namespace Mipmap
{
    internal class Mip : GameWindow
    {
        private int _texture;
        private int _vao, _vbo;

        private readonly float[] _vertices =
        {
            // positions   // tex coords
            -1f, -1f,      0f, 0f,
             1f, -1f,      1f, 0f,
             1f,  1f,      1f, 1f,
            -1f,  1f,      0f, 1f
        };

        private readonly uint[] _indices = { 0, 1, 2, 2, 3, 0 };
        private int _ebo;

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
            LoadTexture(@"C:\Users\black\source\repos\Mipmap\test.jpg");
            //LoadTexture("test.jpg");
            SetupBuffers();
        }

        private void LoadTexture(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Image file not found", path);
            }

            using Bitmap bitmap = new Bitmap(path);
            _texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, _texture);

            // Настройка фильтрации с мипмапами
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

            // Загрузка изображения в текстуру
            BitmapData data = bitmap.LockBits(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          data.Width, data.Height, 0,
                          OpenTK.Graphics.OpenGL4.PixelFormat.Bgra,
                          PixelType.UnsignedByte, data.Scan0);

            bitmap.UnlockBits(data);

            // Генерация уровней мипмапа
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
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);

            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.BindTexture(TextureTarget.Texture2D, _texture);
            GL.BindVertexArray(_vao);

            GL.DrawElements(PrimitiveType.Triangles, _indices.Length, DrawElementsType.UnsignedInt, 0);

            SwapBuffers();
        }

        protected override void OnUnload()
        {
            base.OnUnload();

            GL.DeleteTexture(_texture);
            GL.DeleteBuffer(_vbo);
            GL.DeleteBuffer(_ebo);
            GL.DeleteVertexArray(_vao);
        }
    }
}
