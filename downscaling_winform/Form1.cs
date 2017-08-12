using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing.Imaging;

namespace downscaling_winform
{
    public partial class Form1 : Form
    {
        class Kernel
        {
            public double[] Data { get; private set; } = null;
            public int Width { get; private set; }
            public int Height { get; private set; }
            public Kernel(double[] data, int w, int h)
            {
                Data = data;
                Width = w;
                Height = h;
                System.Diagnostics.Debug.Assert(Data.Length == Width * Height);
            }
        }

        unsafe Bitmap downscaling_kernel(Bitmap input, Size newSize, Kernel k)
        {
            Bitmap output = new Bitmap(newSize.Width, newSize.Height, input.PixelFormat);
            float rx = (float)input.Width / newSize.Width;
            float ry = (float)input.Height / newSize.Height;
            int kwh = k.Width / 2;
            int khh = k.Height / 2;
            using (var o_it = new FLib.BitmapIterator(output, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
            {
                byte* o_data = (byte*)o_it.PixelData;
                using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
                {
                    byte* i_data = (byte*)i_it.PixelData;
                    for (int iy = 0; iy < newSize.Height; iy++)
                    {
                        for (int ix = 0; ix < newSize.Width; ix++)
                        {
                            int cx = (int)((0.5 + ix) * rx);
                            int cy = (int)((0.5 + iy) * ry);
                            int o_idx = 4 * ix + iy * o_it.Stride;
                            double r = 0, g = 0, b = 0;
                            for (int ky = 0; ky < k.Height; ky++)
                            {
                                int py = cy - k.Height / 2 + ky;
                                for (int kx = 0; kx < k.Width; kx++)
                                {
                                    int px = cx - k.Width / 2 + kx;
                                    int i_idx = 4 * px + py * i_it.Stride;
                                    double w = k.Data[kx + ky * k.Width];
                                    b += w * i_data[i_idx + 0] / 255.0;
                                    g += w * i_data[i_idx + 1] / 255.0;
                                    r += w * i_data[i_idx + 2] / 255.0;
                                }
                            }
                            o_data[o_idx + 0] = (byte)(255.0 * b);
                            o_data[o_idx + 1] = (byte)(255.0 * g);
                            o_data[o_idx + 2] = (byte)(255.0 * r);
                            o_data[o_idx + 3] = 255;
                        }
                    }
                }
            }
            return output;
        }

        unsafe Bitmap subsampling(Bitmap input, Size newSize)
        {
            Kernel k = new Kernel(new double[] { 1.0 }, 1, 1);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap box(Bitmap input, Size newSize)
        {
            int ksizex = 11;
            int ksizey = 11;
            var kData = Enumerable.Range(0, ksizex * ksizey).Select(_ => 1.0 / (ksizex * ksizey)).ToArray();
            Kernel k = new Kernel(kData, ksizex, ksizey);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap gaussian5x5(Bitmap input, Size newSize/*, int k*/)
        {
            var kData = new double[]
            {
                1, 4, 6, 4, 1,
                4, 16, 24, 16, 4,
                6,24,36,24,6,
                4, 16, 24, 16, 4,
                1, 4, 6, 4, 1,
            }.Select(v => v / 256.0).ToArray();
            Kernel k = new Kernel(kData, 5, 5);
            return downscaling_kernel(input, newSize, k);
        }

        unsafe Bitmap bicubic(Bitmap input, Size newSize, double a)
        {
            Console.WriteLine($"[bicubic] a = {a}");

            Bitmap bmp = input;
            int width = newSize.Width;
            int height = newSize.Height;
            int sw = bmp.Width, sh = bmp.Height;
            double wf = (double)sw / width, hf = (double)sh / height;
            Func<double, byte> trimByte = x => (byte)Math.Min(Math.Max(0, (int)Math.Round(x)), 255);

            Func<double, double> weightFunc = d =>
                 d <= 1.0 ? ((a + 2.0) * d * d * d) - ((a + 3.0) * d * d) + 1 :
                 d <= 2.0 ? (a * d * d * d) - (5.0 * a * d * d) + (8.0 * a * d) - (4.0 * a) : 0.0;
            
            var output = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            for (var iy = 0; iy < height; iy++)
            {
                for (var ix = 0; ix < width; ix++)
                {
                    double wfx = wf * ix, wfy = hf * iy;
                    var x = (int)Math.Truncate(wfx);
                    var y = (int)Math.Truncate(wfy);
                    double r = .0, g = .0, b = .0;

                    for (int jy = y - 5; jy <= y + 5; jy++)
                    {
                        for (int jx = x - 5; jx <= x + 5; jx++)
                        {
                            var w = weightFunc(Math.Abs(wfx - jx)) * weightFunc(Math.Abs(wfy - jy));
                            if (w == 0) continue;
                            var sx = (jx >= sw - 1) ? x : jx;
                            var sy = (jy >= sh - 1) ? y : jy;
                            var sc = bmp.GetPixel(sx, sy);
                            r += sc.R * w;
                            g += sc.G * w;
                            b += sc.B * w;
                        }
                    }

                    output.SetPixel(ix, iy, Color.FromArgb(trimByte(r), trimByte(g), trimByte(b)));
                }
            }

            return output;
        }

        Bitmap bicubic(Bitmap input, Size newSize)
        {
            double a;
            if (false == double.TryParse(bicubic_a_TBox.Text, out a))
            {
                a = -1.0f;
            }
            return bicubic(input, newSize, a);
        }



        /**
         *
         * Perceptual
         * 
         **/

        unsafe public class IMG
        {
            public int w;
            public int h;
            public double[] data;
            public IMG(int w, int h)
            {
                this.w = w;
                this.h = h;
                this.data = new double[w * h];
            }

            public static IMG operator +(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] + img2.data[i];
                }
                return img;
            }
            public static IMG operator -(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] + img2.data[i];
                }
                return img;
            }
            public static IMG operator *(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] * img2.data[i];
                }
                return img;
            }
            public static IMG operator /(IMG img1, IMG img2)
            {
                var img = new IMG(img1.w, img1.h);
                for (int i = 0; i < img1.w * img1.h; i++)
                {
                    img.data[i] = img1.data[i] / img2.data[i];
                }
                return img;
            }
            public void ZeroIfLessThan(double eps)
            {
                for (int i = 0; i < w * h; i++)
                {
                    if (data[i] < eps) data[i] = 0.0;
                }
            }
        }

        unsafe IMG subSample(IMG img, Size s)
        {
            // TODO
            return img;
        }

        unsafe IMG convValid(IMG img, Kernel s)
        {
            // TODO
            return img;
        }

        unsafe IMG convFull(IMG img, Kernel s)
        {
            // TODO
            return img;
        }

        Kernel P(Size s)
        {
            // Average filter
            var data = Enumerable.Range(0, s.Width * s.Height).Select(_ => 1.0 / (s.Width * s.Height)).ToArray();
            return new Kernel(data, s.Width, s.Height);
        }

        IMG Sqrt(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                img.data[i] = Math.Sqrt(img1.data[i]);
            }
            return img;
        }

        unsafe IMG extractChannel(Bitmap input, int channel)
        {
            IMG img = new IMG(input.Width, input.Height);
            using (var i_it = new FLib.BitmapIterator(input, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb))
            {
                byte* i_data = (byte*)i_it.PixelData;
                for (int iy = 0; iy < input.Height; iy++)
                {
                    for (int ix = 0; ix < input.Width; ix++)
                    {
                        int i_idx = 4 * ix + iy * i_it.Stride;
                        img.data[ix + iy * input.Width] = i_data[i_idx + channel] / 255.0;
                    }
                }
            }
            return img;
        }

        IMG perceptual(IMG input, Size newSize)
        {
            // Appendix B. As simple as possible.
            Size s = new Size(input.w / newSize.Width, input.h / newSize.Height);
            Size np = new Size(2, 2);
            var H = new IMG(1, 1); // TODO
            var IM = new IMG(1, 1); // TODO: what is this?
            var L = subSample(convValid(H, P(s)), s);
            var L2 = subSample(convValid(H * H, P(s)), s);
            var M = convValid(L, P(np));
            var Sl = convValid(L * L, P(np)) - M * M;
            var Sh = convValid(L2, P(np)) - M * M;
            var R = Sqrt(Sh / Sl);
            R.ZeroIfLessThan(1e-4);
            var N = convFull(IM, P(np));
            var T = convFull(R * M, P(np));
            M = convFull(M, P(np));
            R = convFull(R, P(np));
            var D = (M + R * L - T) / N;
            return D;
        }

        unsafe Bitmap toBitmap(IMG R, IMG G, IMG B, PixelFormat pixelFormat)
        {
            int w = R.w;
            int h = R.h;
            Bitmap output = new Bitmap(w, h, pixelFormat);
            using (var o_it = new FLib.BitmapIterator(output, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb))
            {
                byte* o_data = (byte*)o_it.PixelData;
                for (int iy = 0; iy < h; iy++)
                {
                    for (int ix = 0; ix < w; ix++)
                    {
                        int o_idx = 4 * ix + iy * o_it.Stride;
                        o_data[o_idx + 0] = (byte)(255.0 * B.data[ix + iy * w]);
                        o_data[o_idx + 1] = (byte)(255.0 * G.data[ix + iy * w]);
                        o_data[o_idx + 2] = (byte)(255.0 * R.data[ix + iy * w]);
                        o_data[o_idx + 3] = 255;
                    }
                }
            }
            return output;
        }

        unsafe Bitmap perceptual(Bitmap input, Size newSize)
        {
            var imgB = extractChannel(input, 0);
            var imgG = extractChannel(input, 1);
            var imgR = extractChannel(input, 2);
            var DB = perceptual(imgB, newSize);
            var DG = perceptual(imgG, newSize);
            var DR = perceptual(imgR, newSize);
            var output= toBitmap(DR, DG, DB, input.PixelFormat);
            return output;
        }

        void downsample(ShowImageCollection collection, Bitmap input)
        {
            collection.Set(subsamplingRButton.Text, subsampling(input, new Size(32, 32)));
            collection.Set(boxRButton.Text, box(input, new Size(32, 32)));
            collection.Set(gaussianRButton.Text, gaussian5x5(input, new Size(32, 32)));
            collection.Set(bicubicRButton.Text, bicubic(input, new Size(32, 32)));
            collection.Set(perceptualRButton.Text, perceptual(input, new Size(32, 32)));
        }

        class ShowImageCollection
        {
            private Dictionary<string, Bitmap> outputDict_ = new Dictionary<string, Bitmap>();

            public ShowImageCollection()
            {
            }

            public void Clear()
            {
                foreach (var val in outputDict_.Values)
                {
                    if (val != null)
                    {
                        val.Dispose();
                    }
                }
                outputDict_.Clear();
            }

            public void Set(string key, Bitmap img)
            {
                if (outputDict_.ContainsKey(key) && outputDict_[key] != null)
                {
                    outputDict_[key].Dispose();
                    outputDict_.Remove(key);
                }
                outputDict_[key] = img;
            }

            public Bitmap Get(string key)
            {
                if (outputDict_.ContainsKey(key))
                {
                    return outputDict_[key];
                }
                return null;
            }
        }

        ShowImageCollection collection = new ShowImageCollection();
        string showImageKey = "input";
        Bitmap showImage = null;

        public Form1()
        {
            InitializeComponent();
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            openImage(@"../../../../data/input/mario1_input.png");
        }

        void openImage(string fileName)
        {
            using (var bmp = new Bitmap(fileName))
            {
                // コピーしないと参照元の画像ファイルがロックされる
                collection.Clear();
                var input = new Bitmap(bmp);
                collection.Set(inputRButton.Text, input);
                downsample(collection, input);
                inputRButton.Checked = true;
                radioButton_CheckedChanged(inputRButton, null);
            }
        }

        private void openImageOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            openFileDialog.FileName = "";
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(@"../../../../data/input");
            openFileDialog.Filter = "*.png;*.jpg;*.bmp|*.png;*.jpg;*.bmp|すべてのファイル(*.*)|*.*";
            openFileDialog.FilterIndex = 2;
            openFileDialog.Title = "Select Image File";
            openFileDialog.RestoreDirectory = true;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                openImage(openFileDialog.FileName);
            }
        }

        void updateShowImage()
        {
            var bmp = collection.Get(showImageKey);
            if (bmp != null)
            {
                if (showImage != null)
                {
                    showImage.Dispose();
                    showImage = null;
                }
                showImage = new Bitmap(bmp);
                canvas.Invalidate();
            }
        }

        private void radioButton_CheckedChanged(object sender, EventArgs e)
        {
            var radioButton = sender as RadioButton;
            if (radioButton != null && radioButton.Checked)
            {
                showImageKey = radioButton.Text;
                updateShowImage();
            }
        }

        private void canvas_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.Clear(Color.White);
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
            if (showImage != null)
            {
                var clip = e.Graphics.VisibleClipBounds;
                int iw = showImage.Width;
                int ih = showImage.Height;
                float rw = (float)clip.Width / iw;
                float rh = (float)clip.Height / ih;
                float ratio = Math.Min(rw, Math.Min(rh, 100000.0f));
                float w = iw * ratio;
                float h = ih * ratio;
                float x = clip.X + clip.Width * 0.5f - w * 0.5f;
                float y = clip.Y + clip.Height * 0.5f - h * 0.5f;
                e.Graphics.DrawImage(showImage, x, y, w, h);
            }
        }

        private void bicubic_a_TBox_TextChanged(object sender, EventArgs e)
        {
            if (collection != null)
            {
                var input = collection.Get(inputRButton.Text);
                if (input != null)
                {
                    collection.Set(bicubicRButton.Text, bicubic(input, new Size(32, 32)));
                    updateShowImage();
                }
            }
        }
    }
}
