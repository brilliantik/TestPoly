using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Tao.OpenGl;
using Tao.Platform.Windows;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;

namespace TestPoly
{
    public partial class Form1 : Form
    {
        IntPtr Handle3D;
        IntPtr HDC3D;
        IntPtr HRC3D;
        float psi = 30;
        float phi = 30;
        float r = 10;
        uint Texture;
        float b = 1.0f;
        int Font3D = 0;

        public Form1()
        {
            InitializeComponent();
            Handle3D = Handle;
            HDC3D = User.GetDC(Handle3D);
            Gdi.PIXELFORMATDESCRIPTOR PFD = new Gdi.PIXELFORMATDESCRIPTOR();
            PFD.nVersion = 1;
            PFD.nSize = (short)Marshal.SizeOf(PFD);
            PFD.dwFlags = Gdi.PFD_DRAW_TO_WINDOW | Gdi.PFD_SUPPORT_OPENGL | Gdi.PFD_DOUBLEBUFFER;
            PFD.iPixelType = Gdi.PFD_TYPE_RGBA;
            PFD.cColorBits = 24;
            PFD.cDepthBits = 32;
            PFD.iLayerType = Gdi.PFD_MAIN_PLANE;

            int nPixelFormat = Gdi.ChoosePixelFormat(HDC3D, ref PFD);

            Gdi.SetPixelFormat(HDC3D, nPixelFormat, ref PFD);

            HRC3D = Wgl.wglCreateContext(HDC3D);
            Wgl.wglMakeCurrent(HDC3D, HRC3D);

            Form1_Resize(null, null);
            Gl.glEnable(Gl.GL_DEPTH_TEST);
            //Texture = LoadTexture("C:\\Stud\\Mardanov\\Para_3\\Grani.bmp");
            CreateFont3D(Font);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            Gl.glMatrixMode(Gl.GL_PROJECTION);
            Gl.glLoadIdentity();
            //int w = ClientRectangle.Width - panel_control.Width;
            int w = ClientRectangle.Width;
            int h = ClientRectangle.Height;
            Glu.gluPerspective(30, (double)w / h, 2, 20000);
            Gl.glViewport(0, 0, w, h);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Gl.glClearColor(1f, 1f, 1f, 1);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT | Gl.GL_DEPTH_BUFFER_BIT);

            Gl.glMatrixMode(Gl.GL_MODELVIEW);
            Gl.glLoadIdentity();
            Gl.glTranslatef(0, 0, -r);
            Gl.glRotatef(phi, 1f, 0, 0);
            Gl.glRotatef(psi, 0, 1f, 0);

            //draw_point(ClientRectangle.Width / 2, ClientRectangle.Height / 2, 0);
            draw_point(0, 0, 0);
            draw_axis();

            //текст 3Д
            Gl.glColor3f(0, 0, 0);
            OutText3D(2f, 0, 0, "X");
            OutText3D(0, 2f, 0, "Y");
            OutText3D(0, 0, 2f, "Z");


            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
            //Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_LINE);
            Gl.glColor3f(0, 0, 0);
            DrawCube(false);
            Gl.glEnable(Gl.GL_POLYGON_OFFSET_FILL);
            Gl.glPolygonOffset(1f, 1f);
            Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            //Gl.glPolygonMode(Gl.GL_FRONT_AND_BACK, Gl.GL_FILL);
            Gl.glColor3f(1, 1, 1);

            //рисование с текстурой
            //Gl.glEnable(Gl.GL_TEXTURE_2D);
            //Gl.glTexEnvf(Gl.GL_TEXTURE_ENV, Gl.GL_TEXTURE_ENV_MODE, Gl.GL_DECAL);
            //Gl.glBindTexture(Gl.GL_TEXTURE_2D, Texture);
            //DrawCube(true);
            //Gl.glDisable(Gl.GL_TEXTURE_2D);
            //Gl.glDisable(Gl.GL_POLYGON_OFFSET_FILL);
            //DrawFace();



            //рисование с освещением
            Gl.glEnable(Gl.GL_LIGHTING);
            Gl.glEnable(Gl.GL_LIGHT0);
            Gl.glEnable(Gl.GL_NORMALIZE);
            Gl.glEnable(Gl.GL_COLOR_MATERIAL);
            Gl.glLightModeli(Gl.GL_LIGHT_MODEL_TWO_SIDE, 1);

            DrawCube(true);
            Gl.glDisable(Gl.GL_COLOR_MATERIAL);
            Gl.glDisable(Gl.GL_NORMALIZE);
            Gl.glDisable(Gl.GL_LIGHT0);
            Gl.glDisable(Gl.GL_LIGHTING);
            DrawFace();



            Gl.glFinish();
            Gdi.SwapBuffers(HDC3D);
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            DeleteFont3D();
            Wgl.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Wgl.wglDeleteContext(HRC3D);
            User.ReleaseDC(Handle3D, HDC3D);
        }

        static uint LoadTexture(string Filename)
        {
            uint texObject = 0;
            try
            {
                Bitmap bmp = new Bitmap(Filename);
                bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
                BitmapData bmpdata = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                texObject = MakeGlTexture(bmpdata.Scan0, bmp.Width, bmp.Height);
                bmp.UnlockBits(bmpdata);
            }
            catch
            {
                MessageBox.Show("Текстура не загружена!");
            }
            return texObject;
        }

        static uint MakeGlTexture(IntPtr pixels, int w, int h)
        {
            uint texObject;
            Gl.glGenTextures(1, out texObject);
            Gl.glPixelStorei(Gl.GL_UNPACK_ALIGNMENT, 1);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, texObject);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_NEAREST);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_NEAREST);
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGB, w, h, 0, Gl.GL_BGR, Gl.GL_UNSIGNED_BYTE, pixels);
            return texObject;
        }

        //custom methods:
        private void draw_point(int x, int y, int z)
        {
            Gl.glPointSize(10);
            Gl.glColor3f(0, 1, 0);
            Gl.glEnable(Gl.GL_POINT_SMOOTH);
            Gl.glBegin(Gl.GL_POINTS);
            Gl.glVertex3f(x, y, z);
            Gl.glEnd();
        }

        private void draw_axis()
        {
            Gl.glPointSize(10);
            //Gl.glEnable(Gl.GL_LINE_SMOOTH);
            Gl.glLineWidth(1);
            Gl.glBegin(Gl.GL_LINES);
            Gl.glColor3f(1, 0, 0);
            Gl.glVertex3f(0, 0, 0);
            Gl.glVertex3f(3, 0, 0);
            Gl.glColor3f(0, 1, 0);
            Gl.glVertex3f(0, 0, 0);
            Gl.glVertex3f(0, 3, 0);
            Gl.glColor3f(0, 0, 1);
            Gl.glVertex3f(0, 0, 0);
            Gl.glVertex3f(0, 0, 3);
            Gl.glEnd();

        }

        private void CreateFont3D(Font font)
        {
            Gdi.SelectObject(HDC3D, font.ToHfont());
            Font3D = Gl.glGenLists(256);
            Wgl.wglUseFontBitmapsA(HDC3D, 0, 256, Font3D);
        }

        void OutText3D(float x, float y, float z, string Text)
        {
            Gl.glRasterPos3f(x, y, z);
            Gl.glPushAttrib(Gl.GL_LIST_BIT);
            Gl.glListBase(Font3D);
            byte[] bText = MyGl.RussianEncoding.GetBytes(Text);
            Gl.glCallLists(Text.Length, Gl.GL_UNSIGNED_BYTE, bText);
            Gl.glPopAttrib();
        }

        void DeleteFont3D()
        {
            if (Font3D != 0)
            {
                Gl.glDeleteLists(Font3D, 256);
                Font3D = 0;
            }
        }

        private void DrawFace()
        {
            Gl.glEnable(Gl.GL_BLEND);
            Gl.glBlendFunc(Gl.GL_SRC_ALPHA, Gl.GL_ONE_MINUS_SRC_ALPHA);
            Gl.glColor4f(0, 0, 1f, b);

            //Gl.glColor3f(0, 1f, 0);
            Gl.glBegin(Gl.GL_QUADS);
            //Front
            //Gl.glColor3f(1f, 0, 0);
            Gl.glVertex3f(-1f, -1f, 2.5f);
            Gl.glVertex3f(1f, -1f, 2.5f);
            Gl.glVertex3f(1f, 1f, 2.5f);
            Gl.glVertex3f(-1f, 1f, 2.5f);
            Gl.glEnd();
            Gl.glDisable(Gl.GL_BLEND);
        }

        private void DrawCube(bool mode)
        {
            if (mode == false)
            {
                //Gl.glColor3f(0, 1f, 0);
                Gl.glBegin(Gl.GL_QUADS);
                //Bot
                Gl.glVertex3f(-1f, -1f, -1f);
                Gl.glVertex3f(1f, -1f, -1f);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glVertex3f(-1f, -1f, 1f);
                //Top
                //Gl.glColor3f(0, 1f, 0);
                Gl.glVertex3f(-1f, 1f, 1f);
                Gl.glVertex3f(1f, 1f, 1f);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glVertex3f(-1f, 1f, -1f);
                //Front
                //Gl.glColor3f(1f, 0, 0);
                Gl.glVertex3f(-1f, -1f, 1f);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glVertex3f(1f, 1f, 1f);
                Gl.glVertex3f(-1f, 1f, 1f);
                //Back
                //Gl.glColor3f(0, 0, 0);
                Gl.glVertex3f(-1f, -1f, -1f);
                Gl.glVertex3f(-1f, 1f, -1f);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glVertex3f(1f, -1f, -1f);

                //Right
                //Gl.glColor3f(0.5f, 0.5f, 0.5f);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glVertex3f(1f, -1f, -1f);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glVertex3f(1f, 1f, 1f);

                //Left
                //Gl.glColor3f(1f, 0.25f, 0.25f);
                Gl.glVertex3f(-1f, -1f, 1f);
                Gl.glVertex3f(-1f, 1f, 1f);
                Gl.glVertex3f(-1f, 1f, -1f);
                Gl.glVertex3f(-1f, -1f, -1f);

                Gl.glEnd();
            }
            else
            {
                // текстура
                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(0f, 0f);
                //Gl.glVertex3f(-1f, -1f, 1f);
                //Gl.glTexCoord2f(1f/6, 0f);
                //Gl.glVertex3f(1f, -1f, 1f);
                //Gl.glTexCoord2f(1f / 6, 1f);
                //Gl.glVertex3f(1f, 1f, 1f);
                //Gl.glTexCoord2f(0, 1f);
                //Gl.glVertex3f(-1f, 1f, 1f);
                //Gl.glEnd();

                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(1f-1f/6, 0f);
                //Gl.glVertex3f(-1f, -1f, -1f);
                //Gl.glTexCoord2f(1f, 0f);
                //Gl.glVertex3f(1f, -1f, -1f);
                //Gl.glTexCoord2f(1f, 1f);
                //Gl.glVertex3f(1f, 1f, -1f);
                //Gl.glTexCoord2f(1f - 1f / 6, 1f);
                //Gl.glVertex3f(-1f, 1f, -1f);
                //Gl.glEnd();

                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(1f - 2*1f / 6, 0f);
                //Gl.glVertex3f(-1f, -1f, -1f);
                //Gl.glTexCoord2f(1f-1f/6, 0f);
                //Gl.glVertex3f(1f, -1f, -1f);
                //Gl.glTexCoord2f(1f-1f/6, 1f);
                //Gl.glVertex3f(1f, -1f, 1f);
                //Gl.glTexCoord2f(1f - 2*1f / 6, 1f);
                //Gl.glVertex3f(-1f, -1f, 1f);
                //Gl.glEnd();

                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(1f / 6, 0f);
                //Gl.glVertex3f(-1f, 1f, -1f);
                //Gl.glTexCoord2f(2* 1f / 6, 0f);
                //Gl.glVertex3f(1f, 1f, -1f);
                //Gl.glTexCoord2f(2 * 1f / 6, 1f);
                //Gl.glVertex3f(1f, 1f, 1f);
                //Gl.glTexCoord2f( 1f / 6, 1f);
                //Gl.glVertex3f(-1f, 1f, 1f);
                //Gl.glEnd();

                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(2*1f / 6, 0f);
                //Gl.glVertex3f(-1f, -1f, 1f);
                //Gl.glTexCoord2f(3 * 1f / 6, 0f);
                //Gl.glVertex3f(-1f, 1f, 1f);
                //Gl.glTexCoord2f(3 * 1f / 6, 1f);
                //Gl.glVertex3f(-1f, 1f, -1f);
                //Gl.glTexCoord2f(2*1f / 6, 1f);
                //Gl.glVertex3f(-1f, -1f, -1f);
                //Gl.glEnd();



                //Gl.glBegin(Gl.GL_QUADS);
                //Gl.glTexCoord2f(3 * 1f / 6, 0f);
                //Gl.glVertex3f(1f, -1f, 1f);
                //Gl.glTexCoord2f(4 * 1f / 6, 0f);
                //Gl.glVertex3f(1f, -1f, -1f);
                //Gl.glTexCoord2f(4 * 1f / 6, 1f);
                //Gl.glVertex3f(1f, 1f, -1f);
                //Gl.glTexCoord2f(3 * 1f / 6, 1f);
                //Gl.glVertex3f(1f, 1f, 1f);
                //Gl.glEnd();



                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glBegin(Gl.GL_QUADS);
                //Bot
                Gl.glNormal3f(0, -1f, 0);
                Gl.glVertex3f(-1f, -1f, -1f);
                Gl.glNormal3f(0, -1f, 0);
                Gl.glVertex3f(1f, -1f, -1f);
                Gl.glNormal3f(0, -1f, 0);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glNormal3f(0, -1f, 0);
                Gl.glVertex3f(-1f, -1f, 1f);
                //Top
                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glNormal3f(0, 1f, 0);
                Gl.glVertex3f(-1f, 1f, 1f);
                Gl.glNormal3f(0, 1f, 0);
                Gl.glVertex3f(1f, 1f, 1f);
                Gl.glNormal3f(0, 1f, 0);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glNormal3f(0, 1f, 0);
                Gl.glVertex3f(-1f, 1f, -1f);
                ////Front
                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glNormal3f(0, 0, 1f);
                Gl.glVertex3f(-1f, -1f, 1f);
                Gl.glNormal3f(0, 0, 1f);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glNormal3f(0, 0, 1f);
                Gl.glVertex3f(1f, 1f, 1f);
                Gl.glNormal3f(0, 0, 1f);
                Gl.glVertex3f(-1f, 1f, 1f);
                ////Back
                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glNormal3f(0, 0, -1f);
                Gl.glVertex3f(-1f, -1f, -1f);
                Gl.glNormal3f(0, 0, -1f);
                Gl.glVertex3f(-1f, 1f, -1f);
                Gl.glNormal3f(0, 0, -1f);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glNormal3f(0, 0, -1f);
                Gl.glVertex3f(1f, -1f, -1f);

                ////Right
                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glNormal3f(1f, 0, 0);
                Gl.glVertex3f(1f, -1f, 1f);
                Gl.glNormal3f(1f, 0, 0);
                Gl.glVertex3f(1f, -1f, -1f);
                Gl.glNormal3f(1f, 0, 0);
                Gl.glVertex3f(1f, 1f, -1f);
                Gl.glNormal3f(1f, 0, 0);
                Gl.glVertex3f(1f, 1f, 1f);

                ////Left
                Gl.glColor3f(0, 0.25f, 0.75f);
                Gl.glNormal3f(-1f, 0, 0);
                Gl.glVertex3f(-1f, -1f, 1f);
                Gl.glNormal3f(-1f, 0, 0);
                Gl.glVertex3f(-1f, 1f, 1f);
                Gl.glNormal3f(-1f, 0, 0);
                Gl.glVertex3f(-1f, 1f, -1f);
                Gl.glNormal3f(-1f, 0, 0);
                Gl.glVertex3f(-1f, -1f, -1f);

                Gl.glEnd();
            }

        }

        private void InvalidateRect()
        {
            MyGl.InvalidateRect(Handle, IntPtr.Zero, false);
        }

        protected override void WndProc(ref Message mes)
        {
            base.WndProc(ref mes);
            if (mes.Msg == MyGl.WM_ERASEBKGND)
            {
                mes.Result = IntPtr.Zero;
                InvalidateRect();
            }
        }
    }

    class MyGl
    {
        internal const int WM_ERASEBKGND = 0x0014;
        [DllImport("user32.dll")]
        internal static extern bool InvalidateRect(IntPtr hWind, IntPtr lpRect, bool bErase);
        internal static Encoding RussianEncoding = Encoding.GetEncoding(1251);
    }

}
