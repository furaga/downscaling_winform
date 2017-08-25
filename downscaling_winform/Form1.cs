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
        // Content-Adaptive Downscaling [Koph, et al., SIGGRAPH Asia 2013]
        unsafe Bitmap contentAdaptive(Bitmap input, Size newSize)
        {
            var output = input;
            return output;
        }








        #region DownscaleMethods
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
                                    if (0 <= px && px < input.Width && 0 <= py && py < input.Height)
                                    {
                                        double w = k.Data[kx + ky * k.Width];
                                        b += w * i_data[i_idx + 0] / 255.0;
                                        g += w * i_data[i_idx + 1] / 255.0;
                                        r += w * i_data[i_idx + 2] / 255.0;
                                    }
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
                    img.data[i] = img1.data[i] - img2.data[i];
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
                    img.data[i] = img2.data[i] <= 1e-4 ? 0.0 : img1.data[i] / img2.data[i];
                }
                return img;
            }
        }

        unsafe IMG subSample(IMG img1, Size newSize)
        {
            IMG img = new IMG(newSize.Width, newSize.Height);
            double pw = (double)img1.w / newSize.Width;
            double ph = (double)img1.h / newSize.Height;
            for (int y = 0; y < img.h; y++)
            {
                for (int x = 0; x < img.w; x++)
                {
                    // patchの真ん中をサンプリング
                    int sx = (int)((0.5 + x) * pw);
                    int sy = (int)((0.5 + y) * ph);
                    img.data[x + img.w * y] = img1.data[sx + img1.w * sy];
                }
            }
            return img;
        }

        unsafe IMG convValid(IMG input, Kernel k)
        {
            int iw = input.w;
            int ih = input.h;
            int kw = k.Width;
            int kh = k.Height;
            var kdata = k.Data;
            var img1data = input.data;

            IMG output = new IMG(iw - (kw - 1), ih - (kh - 1));
            int ow = output.w;
            int oh = output.h;
            var imgdata = output.data;
            for (int y = 0; y < oh; y++)
            {
                int ooffset = ow * y;
                for (int x = 0; x < ow; x++)
                {
                    double value = 0.0;
                    for (int ky = 0; ky < kh; ky++)
                    {
                        int koffset = ky * kw;
                        int offset = (y + ky) * iw + x;
                        for (int kx = 0; kx < kw; kx++)
                        {
                            value += kdata[kx + koffset] * img1data[kx + offset];
                        }
                    }
                    imgdata[x + ooffset] = value;
                }
            }
            return output;
        }

        unsafe IMG convFull(IMG img1, Kernel k)
        {
            IMG img = new IMG(img1.w + (k.Width - 1), img1.h + (k.Height - 1));
            for (int y = 0; y < img.h; y++)
            {
                for (int x = 0; x < img.w; x++)
                {
                    double value = 0.0;
                    for (int ky = 0; ky < k.Height; ky++)
                    {
                        for (int kx = 0; kx < k.Width; kx++)
                        {
                            int ix = (x + kx - k.Width + 1);
                            int iy = (y + ky - k.Height + 1);
                            if (0 <= ix && ix < img1.w && 0 <= iy && iy < img1.h)
                            {
                                value += k.Data[kx + ky * k.Width] * img1.data[ix + iy * img1.w];
                            }
                        }
                    }
                    img.data[x + img.w * y] = value;
                }
            }
            return img;
        }

        Kernel P(Size s)
        {
            // Average filter
            var data = Enumerable.Range(0, s.Width * s.Height).Select(_ => 1.0 / (s.Width * s.Height)).ToArray();
            return new Kernel(data, s.Width, s.Height);
        }

        IMG I(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                img.data[i] = 1.0;
            }
            return img;
        }

        IMG Sqrt(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                // 誤差の関係で0以下になることがある
                img.data[i] = img1.data[i] < 0 ? 0 : Math.Sqrt(img1.data[i]);
            }
            return img;
        }

        IMG Clamp01(IMG img1)
        {
            var img = new IMG(img1.w, img1.h);
            for (int i = 0; i < img1.w * img1.h; i++)
            {
                // 誤差の関係で0以下になることがある
                img.data[i] = img1.data[i] < 0 ? 0 : img1.data[i] > 1 ? 1 : img1.data[i];
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

        IMG perceptual(IMG H, Size newSize)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Follow Appendix B.
            Size s = new Size(H.w / newSize.Width, H.h / newSize.Height);
            Size np = new Size(2, 2);
            var hps = convValid(H, P(s));
            Console.WriteLine(string.Format("phase0.5: {0} ms", stopwatch.ElapsedMilliseconds));
            var L = subSample(hps, newSize);
            Console.WriteLine(string.Format("phase1: {0} ms", stopwatch.ElapsedMilliseconds));
            var L2 = subSample(convValid(H * H, P(s)), newSize);
            Console.WriteLine(string.Format("phase2: {0} ms", stopwatch.ElapsedMilliseconds));

            System.Diagnostics.Debug.Assert(L.w == newSize.Width);
            System.Diagnostics.Debug.Assert(L.h == newSize.Height);
            var M = convValid(L, P(np));
            Console.WriteLine(string.Format("phase3: {0} ms", stopwatch.ElapsedMilliseconds));

            var Sl = convValid(L * L, P(np)) - M * M;
            Console.WriteLine(string.Format("phase4: {0} ms", stopwatch.ElapsedMilliseconds));

            var Sh = convValid(L2, P(np)) - M * M;
            Console.WriteLine(string.Format("phase5: {0} ms", stopwatch.ElapsedMilliseconds));

            var R = Sqrt(Sh / Sl);
            Console.WriteLine(string.Format("phase6: {0} ms", stopwatch.ElapsedMilliseconds));

            R = Clamp01(R);
            Console.WriteLine(string.Format("phase7: {0} ms", stopwatch.ElapsedMilliseconds));

            var N = convFull(I(M), P(np));
            Console.WriteLine(string.Format("phase8: {0} ms", stopwatch.ElapsedMilliseconds));

            var T = convFull(R * M, P(np));
            Console.WriteLine(string.Format("phase9: {0} ms", stopwatch.ElapsedMilliseconds));

            M = convFull(M, P(np));
            Console.WriteLine(string.Format("phase10: {0} ms", stopwatch.ElapsedMilliseconds));

            R = convFull(R, P(np));
            Console.WriteLine(string.Format("phase11: {0} ms", stopwatch.ElapsedMilliseconds));

            var D = (M + R * L - T) / N;
            Console.WriteLine(string.Format("phase12: {0} ms", stopwatch.ElapsedMilliseconds));

            System.Diagnostics.Debug.Assert(D.w == newSize.Width);
            System.Diagnostics.Debug.Assert(D.h == newSize.Height);
            Console.WriteLine(string.Format("phase13: {0} ms", stopwatch.ElapsedMilliseconds));

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
            var output = toBitmap(DR, DG, DB, input.PixelFormat);
            return output;
        }
        #endregion

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
                donwscale(collection, input);
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
                    double scalePercentage = 10.0;
                    if (double.TryParse(scaleTBox.Text, out scalePercentage))
                    {
                        int w = (int)(input.Width * scalePercentage * 0.01);
                        int h = (int)(input.Height * scalePercentage * 0.01);
                        var size = new Size(w, h);
                        collection.Set(bicubicRButton.Text, bicubic(input, size));
                        updateShowImage();
                    }
                }
            }
        }

        private void downscaleButton_Click(object sender, EventArgs e)
        {
            var input = collection.Get(inputRButton.Text);
            donwscale(collection, input);
            updateShowImage();
        }

        class DonwscaleWorkArg
        {
            public Bitmap input;
            public ShowImageCollection collection;
        }

        void donwscale(ShowImageCollection collecton, Bitmap input)
        {
            //処理が行われているときは、何もしない
            if (backgroundWorker1.IsBusy)
            {
                return;
            }
            downscaleButton.Enabled = false;
            richTextBox1.Text = "";
            progressBar.Minimum = 0;
            progressBar.Maximum = 100;
            progressBar.Value = 0;
            backgroundWorker1.WorkerReportsProgress = true;
            backgroundWorker1.WorkerSupportsCancellation = true;
            backgroundWorker1.RunWorkerAsync(new DonwscaleWorkArg()
            {
                collection = collection,
                input = input,
            });
        }

        private void downscale_BGWork(object sender, DoWorkEventArgs e)
        {
            var bgWorker = sender as BackgroundWorker;
            var args = e.Argument as DonwscaleWorkArg;
            if (args != null)
            {
                var input = args.input;
                var collection = args.collection;
                if (input != null && collection != null)
                {
                    double scalePercentage = 10.0;
                    if (double.TryParse(scaleTBox.Text, out scalePercentage))
                    {
                        int w = (int)(input.Width * scalePercentage * 0.01);
                        int h = (int)(input.Height * scalePercentage * 0.01);
                        var size = new Size(w, h);

                        var sw = System.Diagnostics.Stopwatch.StartNew();
                        collection.Set(subsamplingRButton.Text, subsampling(input, size));
                        bgWorker.ReportProgress(0, "Subsampling: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(boxRButton.Text, box(input, size));
                        bgWorker.ReportProgress(25, "Box: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(gaussianRButton.Text, gaussian5x5(input, size));
                        bgWorker.ReportProgress(50, "Gaussian: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(bicubicRButton.Text, bicubic(input, size));
                        bgWorker.ReportProgress(75, "Bicubic: " + sw.Elapsed.TotalSeconds + " s");
                        //collection.Set(perceptualRButton.Text, perceptual(input, size));
                        //bgWorker.ReportProgress(100, "Perceptual: " + sw.Elapsed.TotalSeconds + " s");
                        collection.Set(contentAdaptiveRButton.Text, contentAdaptive(input, size));
                        bgWorker.ReportProgress(100, "ContentAdaptive: " + sw.Elapsed.TotalSeconds + " s");
                    }
                }
            }
        }

        private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
            richTextBox1.Text += e.UserState as string + "\n";
        }

        private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            downscaleButton.Enabled = true;
            richTextBox1.Text += "All downscaling methods successfully finished!";
        }
    }
}
