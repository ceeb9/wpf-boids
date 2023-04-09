using System;
using CeebEngine;
using System.Drawing;
using Boids;
using System.ComponentModel;
using System.Threading;

namespace CeebInterface
{
    class CeebInterface
    {
        public static RenderCanvas c = new RenderCanvas(1000, 1000, new Action<RenderHandler.FrameInfo>(OnFrame));
        public static float[] frametimes = new float[5];
        public static BoidLogic b = new BoidLogic(2500, c);
        public static int boidSize;
        public static void StartRenderer(object Sender, DoWorkEventArgs e)
        {
            OnStart();
            c.renderHandler.startRendering();
        }

        //user code that runs once before rendering starts
        public static void OnStart()
        {
            //initalize thread that does boid logic
            Thread boidThread = new Thread(b.boidTick);
            boidThread.Name = "boidThread";
            boidThread.Start();

            boidSize = (float)c.Width/125f >= 1 ? c.Width/125 : 1;

            c.CreateShape("bg", (int)ShapeType.Rectangle, 0, 0, c.Width, c.Height, true, Color.Black);
            c.CreateShape("border", (int)ShapeType.Rectangle, 0, 0, c.Width, c.Height, false, Color.Red);

            for (int i = 0; i < b.boids.Count; i++)
            {
                c.CreateShape(i.ToString(), (int)ShapeType.Circle, (int)b.boids[i].pos.X, (int)b.boids[i].pos.Y, boidSize, boidSize, true, color: Color.White, 0);
                c.CreateShape(i.ToString()+"c", (int)ShapeType.Circle, (int)b.boids[i].pos.X, (int)b.boids[i].pos.Y, boidSize, boidSize, true, color:Color.FromArgb(64, 255, 0, 0));
            }

            c.CreateShape("frametime", (int)ShapeType.Text, 5, 0, color:Color.White);
            c.CreateShape("boidms", (int)ShapeType.Text, 5, 16, color:Color.White);

            c.CreateShape("align", (int)ShapeType.Text, 5, 40, color:Color.White);
            c.CreateShape("cohere", (int)ShapeType.Text, 5, 56, color:Color.White);
            c.CreateShape("avoid", (int)ShapeType.Text, 5, 72, color:Color.White);
            
            c.CreateShape("stepsize", (int)ShapeType.Text, 5, 96, color:Color.White);
            
            c.lookup("frametime").font = new Font("Consolas", 16f);
            c.lookup("align").font = new Font("Consolas", 16f);
            c.lookup("cohere").font = new Font("Consolas", 16f);
            c.lookup("avoid").font = new Font("Consolas", 16f);
            c.lookup("boidms").font = new Font("Consolas", 16f);
            c.lookup("stepsize").font = new Font("Consolas", 16f);
        }

        //user code that runs each time the frame is rendered
        public static void OnFrame(RenderHandler.FrameInfo fi)
        {
            //code for averaging frametimes (retarded)
            frametimes[frametimes.Length - 1] = 0;
            float[] tempFrametimeHolder = new float[frametimes.Length];
            for (var i = 0; i < frametimes.Length - 1; i++)
            {
                tempFrametimeHolder[i + 1] = frametimes[i];
            }
            frametimes = tempFrametimeHolder;
            frametimes[0] = fi.frametime;
            float avg = 0;
            for (var i = 0; i < frametimes.Length; i++)
            {
                avg += frametimes[i];
            }
            avg = avg / frametimes.Length;
            c.lookup("frametime").text = "ms " + Math.Round(avg).ToString();
            //-----------------------------------------

            //debugging writeouts
            c.lookup("align").text = "al " + (Math.Round(1000*b.currWeights.align)/1000).ToString();
            c.lookup("cohere").text = "co " + (Math.Round(1000*b.currWeights.cohere)/1000).ToString();
            c.lookup("avoid").text = "av " + (Math.Round(1000*b.currWeights.avoid)/1000).ToString();
            c.lookup("boidms").text = "boidms " + b.lastUpdateMs.ToString();
            c.lookup("stepsize").text = "stepsize " + b.stepSize.ToString();

            for (int i = 0; i < b.boids.Count; i++)
            {
                c.lookup(i.ToString()).pt1.X = b.boids[i].pos.X;
                c.lookup(i.ToString()).pt1.Y = b.boids[i].pos.Y;

                if(b.boids[i].nearbyBoids.Count > 1)
                {
                    c.lookup(i.ToString()+"c").pt1 = new PointF(b.boids[i].flockPos.X-c.lookup(i.ToString()+"c").pt2.X/2, b.boids[i].flockPos.Y-c.lookup(i.ToString()+"c").pt2.X/2);
                }
                else
                {
                    c.lookup(i.ToString()+"c").pt1 = new PointF(-10,-10);
                }

                if(b.boids[i].nearbyBoids.Count > 1)
                {
                    c.lookup(i.ToString()).color = Color.Green;
                }
                else
                {
                    c.lookup(i.ToString()).color = Color.Blue;
                }

            }
        }

    }

}