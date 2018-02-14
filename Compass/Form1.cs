using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compass
{
    public partial class Form1 : Form
    {
         [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            Form1 s= new Form1();
            Application.ApplicationExit +=s.Application_ApplicationExit;
            Application.Run(s);

        }


        void Application_ApplicationExit(object sender, EventArgs e)
        {

            DisconnectGPS();

        }

        


        //Timer t = new Timer();
        Thread[] _Gps_Threads = null;
        private System.IO.Ports.SerialPort serialPort1;
        private System.IO.Ports.SerialPort[] _Serial_Ports = null;
        private String Latitude;
        // private double Heading;
        private String Longitude;
        
        double degree = 0;
        double tilt = 0;
        double pitch = 0;
        int opD = 1;
        int opP = 1;
        int opT = 1;
        public Form1()
        {
            InitializeComponent();
            this.pictureBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.Resize += Form1_Resize;
            // pictureBox1.Image = Compass.DrawCompass(162.5, -80, 80, -30, 80, pictureBox1.Size);
            pictureBox1.Image = Compass.DrawCompass(degree, pitch, 80, tilt, 80, pictureBox1.Size);

            ConnectGPS();
            
            //t.Interval = 50;
            //t.Tick += t_Tick;
            //t.Enabled = true;

        }

        void DisconnectGPS()
        {
            if (serialPort1 != null)
            {
                try
                {
                    if (serialPort1.IsOpen)
                        serialPort1.Close();
                    serialPort1.Dispose();
                }
                catch { }
                //  serialPort1 = null;
            } if (_Serial_Ports != null)
            {
                for (int i = _Serial_Ports.Length - 1; i >= 0; i--)
                {
                    System.IO.Ports.SerialPort p = _Serial_Ports[i];

                    if (p != null)
                    {
                        try
                        {
                            if (p.IsOpen)
                                p.Close();
                            p.Dispose();
                        }
                        catch
                        { }
                        //        p = null;
                    }

                }
            }
            foreach (Thread t in _Gps_Threads)
            {
                t.Abort();
            }
        }
        void ConnectGPS()
        {
            String[] portnames = System.IO.Ports.SerialPort.GetPortNames();
            _Serial_Ports = new System.IO.Ports.SerialPort[portnames.Length];
            _Gps_Threads = new Thread[portnames.Length];
            for (int i = 0; i < portnames.Length; i++)
            {
                System.IO.Ports.SerialPort ssp = new System.IO.Ports.SerialPort(portnames[i]);
                try
                {
                    object data0 = (object)new object[] { ssp, i };
                    System.Threading.Thread t1 = new Thread(delegate(object data)
                    {
                        System.IO.Ports.SerialPort sspt1 = (System.IO.Ports.SerialPort)((object[])data)[0];
                        int it1 = (int)((object[])data)[1];
                        _Serial_Ports[it1] = sspt1;
                        try
                        {
                            sspt1.DataReceived += serialPort1_DataReceived;
                            sspt1.Open();
                        }
                        catch
                        { }
                        System.Threading.Thread.Sleep(3000);
                        try
                        {
                            foreach (System.IO.Ports.SerialPort sspt2 in _Serial_Ports.Where(r => !r.PortName.Equals(serialPort1.PortName)))
                            {
                                if (sspt2.IsOpen)
                                    sspt2.Close();
                                sspt2.Dispose();
                            }
                        }
                        catch
                        { }

                        System.Threading.Thread.CurrentThread.Join();

                    });
                    _Gps_Threads[i] = t1;
                    t1.Start(data0);
                    //   t1.Join();
                }

                catch { }

            }


        }


        void serialPort1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
                try
                {
                    System.IO.Ports.SerialPort p = ((System.IO.Ports.SerialPort)sender);
                    string data = p.ReadExisting();
                    string[] strArr = data.Split('$');
                    for (int i = 0; i < strArr.Length; i++)
                    {
                        string strTemp = strArr[i];
                        string[] nmea = strTemp.Split(',');
                        if (nmea[0] == "GPRMC")
                        {
                            serialPort1 = p;
                            //Latitude
                            if (!String.IsNullOrEmpty(nmea[8]) )
                            {
                                degree = Convert.ToDouble(nmea[8]);// -(lines[11] == "E" ? 1 : -1) * (String.IsNullOrEmpty(lines[10]) ? 0 : Convert.ToDouble(lines[10]));
                                pitch = 0;
                                tilt = 0;
                                pictureBox1.Image = Compass.DrawCompass(degree, pitch, 80, tilt, 80, pictureBox1.Size);
                            }
                        }
                    }
                }
                catch
                {
                }
        }

        //void t_Tick(object sender, EventArgs e)
        //{
        //    if ((tilt > 78.2 || tilt < -78.2) && tilt != 0)
        //        opT = opT * -1;

        //    if ((pitch > 78.2 || pitch < -78.2) && pitch != 0)
        //        opP = opP * -1;
        //    if ((degree > 359.2 || degree < 0.8) && degree != 0)
        //        opD = opD * -1;


        //    pitch = pitch + opP * 0.8;
        //    tilt = tilt + opT * 0.5;
        //    degree = degree + opD * 0.8;

        //    pictureBox1.Image = Compass.DrawCompass(degree, pitch, 80, tilt, 80, pictureBox1.Size);
        //}

        void Form1_Resize(object sender, EventArgs e)
        {
            pictureBox1.Image = Compass.DrawCompass(degree, pitch, 80, tilt, 80, pictureBox1.Size);
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            DisconnectGPS();
            ConnectGPS();
        }

    }

    public class Compass
    {
        public static Bitmap DrawCompass(double degree, double pitch, double maxpitch, double tilt, double maxtilt, Size s)
        {

          
                double maxRadius = s.Width > s.Height ? s.Height / 2 : s.Width / 2;

                double sizeMultiplier = maxRadius / 200;
                double relativepitch = pitch / maxpitch;
                double relativetilt = tilt / maxtilt;

                Bitmap result=null;
                SolidBrush drawBrushWhite = new SolidBrush(Color.FromArgb(255, 244, 255));
                SolidBrush drawBrushRed = new SolidBrush(Color.FromArgb(240, 255, 0, 0));
                SolidBrush drawBrushOrange = new SolidBrush(Color.FromArgb(240, 255, 150, 0));
                SolidBrush drawBrushBlue = new SolidBrush(Color.FromArgb(100, 0, 250, 255));
                SolidBrush drawBrushWhiteGrey = new SolidBrush(Color.FromArgb(20, 255, 255, 255));
                double outerradius = (((maxRadius - sizeMultiplier * 60) / maxRadius) * maxRadius);
                double innerradius = (((maxRadius - sizeMultiplier * 90) / maxRadius) * maxRadius);
                double degreeRadius = outerradius + 37 * sizeMultiplier;
                double dirRadius = innerradius - 30 * sizeMultiplier;
                double TriRadius = outerradius + 20 * sizeMultiplier;
                double PitchTiltRadius = innerradius * 0.55;
                if (s.Width * s.Height > 0)
                {
                    result=new Bitmap(s.Width, s.Height);
                using (Font font2 = new Font("Arial", (float)(16 * sizeMultiplier)))
                {
                    using (Font font1 = new Font("Arial", (float)(14 * sizeMultiplier)))
                    {
                        using (Pen penblue = new Pen(Color.FromArgb(100, 0, 250, 255), ((int)(sizeMultiplier) < 4 ? 4 : (int)(sizeMultiplier))))
                        {
                            using (Pen penorange = new Pen(Color.FromArgb(255, 150, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
                            {
                                using (Pen penred = new Pen(Color.FromArgb(255, 0, 0), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
                                {

                                    using (Pen pen1 = new Pen(Color.FromArgb(255, 255, 255), (int)(sizeMultiplier * 4)))
                                    {

                                        using (Pen pen2 = new Pen(Color.FromArgb(255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
                                        {
                                            using (Pen pen3 = new Pen(Color.FromArgb(0, 255, 255, 255), ((int)(sizeMultiplier) < 1 ? 1 : (int)(sizeMultiplier))))
                                            {
                                                using (Graphics g = Graphics.FromImage(result))
                                                {


                                                    // Calculate some image information.
                                                    double sourcewidth = s.Width;
                                                    double sourceheight = s.Height;

                                                    int xcenterpoint = (int)(s.Width / 2);
                                                    int ycenterpoint = (int)((s.Height / 2));// maxRadius; //TODO: 

                                                    Point pA1 = new Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 45));
                                                    Point pB1 = new Point(xcenterpoint - (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));
                                                    Point pC1 = new Point(xcenterpoint, ycenterpoint - (int)(sizeMultiplier * 90));
                                                    Point pB2 = new Point(xcenterpoint + (int)(sizeMultiplier * 7), ycenterpoint - (int)(sizeMultiplier * 45));

                                                    Point[] a2 = new Point[] { pA1, pB1, pC1 };
                                                    Point[] a3 = new Point[] { pA1, pB2, pC1 };


                                                    g.DrawPolygon(penred, a2);
                                                    g.FillPolygon(drawBrushRed, a2);
                                                    g.DrawPolygon(penred, a3);
                                                    g.FillPolygon(drawBrushWhite, a3);


                                                    double[] Cos = new double[360];
                                                    double[] Sin = new double[360];

                                                    //draw centercross
                                                    g.DrawLine(pen2, new Point(((int)(xcenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint), new Point(((int)(xcenterpoint + (PitchTiltRadius - sizeMultiplier * 50))), ycenterpoint));
                                                    g.DrawLine(pen2, new Point(xcenterpoint, (int)(ycenterpoint - (PitchTiltRadius - sizeMultiplier * 50))), new Point(xcenterpoint, ((int)(ycenterpoint + (PitchTiltRadius - sizeMultiplier * 50)))));


                                                    //draw pitchtiltcross
                                                    Point PitchTiltCenter = new Point((int)(xcenterpoint + PitchTiltRadius * relativetilt), (int)(ycenterpoint - PitchTiltRadius * relativepitch));
                                                    int rad = (int)(sizeMultiplier * 8);
                                                    int rad2 = (int)(sizeMultiplier * 25);

                                                    Rectangle r = new Rectangle((int)(PitchTiltCenter.X - rad2), (int)(PitchTiltCenter.Y - rad2), (int)(rad2 * 2), (int)(rad2 * 2));
                                                    g.DrawEllipse(pen3, r);
                                                    g.FillEllipse(drawBrushWhiteGrey, r);
                                                    g.DrawLine(penorange, PitchTiltCenter.X - rad, PitchTiltCenter.Y, PitchTiltCenter.X + rad, PitchTiltCenter.Y);
                                                    g.DrawLine(penorange, PitchTiltCenter.X, PitchTiltCenter.Y - rad, PitchTiltCenter.X, PitchTiltCenter.Y + rad);


                                                    //prep here because need before and after for red triangle.
                                                    for (int d = 0; d < 360; d++)
                                                    {
                                                        //   map[y] = new long[src.Width];
                                                        double angleInRadians = ((((double)d) + 270d) - degree) / 180F * Math.PI;
                                                        Cos[d] = Math.Cos(angleInRadians);
                                                        Sin[d] = Math.Sin(angleInRadians);
                                                    }


                                                    for (int d = 0; d < 360; d++)
                                                    {



                                                        Point p1 = new Point((int)(outerradius * Cos[d]) + xcenterpoint, (int)(outerradius * Sin[d]) + ycenterpoint);
                                                        Point p2 = new Point((int)(innerradius * Cos[d]) + xcenterpoint, (int)(innerradius * Sin[d]) + ycenterpoint);

                                                        //Draw Degree labels
                                                        if (d % 30 == 0)
                                                        {
                                                            g.DrawLine(penblue, p1, p2);

                                                            Point p3 = new Point((int)(degreeRadius * Cos[d]) + xcenterpoint, (int)(degreeRadius * Sin[d]) + ycenterpoint);
                                                            SizeF s1 = g.MeasureString(d.ToString(), font1);
                                                            p3.X = p3.X - (int)(s1.Width / 2);
                                                            p3.Y = p3.Y - (int)(s1.Height / 2);

                                                            g.DrawString(d.ToString(), font1, drawBrushWhite, p3);
                                                            Point pA = new Point((int)(TriRadius * Cos[d]) + xcenterpoint, (int)(TriRadius * Sin[d]) + ycenterpoint);

                                                            int width = (int)(sizeMultiplier * 3);
                                                            int dp = d + width > 359 ? d + width - 360 : d + width;
                                                            int dm = d - width < 0 ? d - width + 360 : d - width;

                                                            Point pB = new Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dm]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dm]) + ycenterpoint);
                                                            Point pC = new Point((int)((TriRadius - (15 * sizeMultiplier)) * Cos[dp]) + xcenterpoint, (int)((TriRadius - (15 * sizeMultiplier)) * Sin[dp]) + ycenterpoint);

                                                            Pen p = penblue;
                                                            Brush b = drawBrushBlue;
                                                            if (d == 0)
                                                            {
                                                                p = penred;
                                                                b = drawBrushRed;
                                                            }
                                                            Point[] a = new Point[] { pA, pB, pC };

                                                            g.DrawPolygon(p, a);
                                                            g.FillPolygon(b, a);
                                                        }
                                                        else if (d % 2 == 0)
                                                            g.DrawLine(pen2, p1, p2);

                                                        //draw N,E,S,W
                                                        if (d % 90 == 0)
                                                        {
                                                            string dir = (d == 0 ? "N" : (d == 90 ? "E" : (d == 180 ? "S" : "W")));
                                                            Point p4 = new Point((int)(dirRadius * Cos[d]) + xcenterpoint, (int)(dirRadius * Sin[d]) + ycenterpoint);
                                                            SizeF s2 = g.MeasureString(dir, font1);
                                                            p4.X = p4.X - (int)(s2.Width / 2);
                                                            p4.Y = p4.Y - (int)(s2.Height / 2);


                                                            g.DrawString(dir, font1, d == 0 ? drawBrushRed : drawBrushBlue, p4);

                                                            //}
                                                            ////Draw red triangle at 0 degrees 
                                                            //if (d == 0)
                                                            //{


                                                        }

                                                    }
                                                    //draw course

                                                    //g.DrawLine(pen1, new Point(xcenterpoint, ycenterpoint - (int)innerradius), new Point(xcenterpoint, ycenterpoint - ((int)outerradius + (int)(sizeMultiplier * 50))));




                                                    String deg = Math.Round(degree, 2).ToString("0.00") + "°";
                                                    SizeF s3 = g.MeasureString(deg, font1);

                                                    g.DrawString(deg, font2, drawBrushOrange, new Point(xcenterpoint - (int)(s3.Width / 2), ycenterpoint - (int)(sizeMultiplier * 40)));

                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return result;
        }
    }
}
