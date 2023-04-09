using System;
using CeebEngine;
using System.Numerics;
using System.Drawing;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Boids
{
    class BoidLogic
    {
        public static RenderCanvas cv;
        public Weights currWeights;
        public int lastUpdateMs;
        public float stepSize;
        public List<Boid> boids;
        public Vector2 speedLimit = new Vector2(2f, 2f);
        const float alignWeight = 0.12f;
        const float cohereWeight = 0.05f;
        const float avoidWeight = 0.1f;
        public BoidLogic(int count, RenderCanvas _cv)
        {
            cv = _cv;
            boids = new List<Boid>();
            for (int i = 0; i < count; i++)
            {
                boids.Add(new Boid());
            }
        }
        public void boidTick()
        {
            Hashtable desiredVects = new Hashtable();
            Vector2 accel = new Vector2();
            Stopwatch s = new Stopwatch();
            s.Start();

            currWeights = new Weights(alignWeight, cohereWeight, avoidWeight);
            while (true)
            {
                //update weights of different vectors
                lastUpdateMs = (int)s.ElapsedMilliseconds;
                stepSize = (float)lastUpdateMs/1000f;
                s.Restart();

                //update nearby boid list
                updateInfo();

                for (int i = 0; i < boids.Count; i++)
                {   
                    //reset and update vars
                    desiredVects.Clear();
                    accel = Vector2.Zero;
                    desiredVects = calcDesiredVects(boids[i], currWeights);
                    desiredVects.Add("avoidedges", avoidEdges(boids[i]));

                    //sum all desired vectors to get the acceleration (change in velocity for this frame)
                    foreach (DictionaryEntry vect in desiredVects)
                    {
                        accel += (Vector2)vect.Value;
                    }

                    //clamp the acceleration so they dont change direction too fast
                    if(Math.Abs(accel.Length()) > speedLimit.Length())
                    {
                        accel = Vector2.Normalize(accel) * speedLimit;
                    }

                    //clamp velocity to the speedlimit if over
                    if(Math.Abs(boids[i].vel.Length()) > speedLimit.Length())
                    {
                        boids[i].vel = Vector2.Multiply(Vector2.Normalize(boids[i].vel), speedLimit);
                    }
                    
                    //change velocity by a small component of the acceleration
                    //the size of this component is based on the frametime
                    boids[i].vel += accel;
                    boids[i].vel += (Vector2)desiredVects["avoidedges"];
                
                    //change the position by the new velocity
                    boids[i].pos.X += boids[i].vel.X*stepSize;
                    boids[i].pos.Y += boids[i].vel.Y*stepSize;

                    //restrict position to inside the canvas
                    boids[i].pos.X = Math.Clamp(boids[i].pos.X, 0f, cv.Width);
                    boids[i].pos.Y = Math.Clamp(boids[i].pos.Y, 0f, cv.Height);
                }
            }
        }

        public Hashtable calcDesiredVects(Boid boid, Weights currWeights)
        {
            Vector2 avoidVect = new Vector2(); //vector to avoid boids that are too close (avoidweight)

            Vector2 avgPosVect = new Vector2(); //vector to go towards the avg centre pos of the flock (cohereweight)

            Vector2 avgVect = new Vector2(); //vector to align towards the avg direction of the flock (alignweight)

            PointF flockPos = new PointF(); //average position of the flock (for debugging)

            for (var i = 0; i < boid.nearbyBoids.Count; i++) //sum all values of the nearby boids (those in the flock)
            {
                avgPosVect.X += boid.nearbyBoids[i].pos.X;
                avgPosVect.Y += boid.nearbyBoids[i].pos.Y;

                avgVect.X += boid.nearbyBoids[i].vel.X;
                avgVect.Y += boid.nearbyBoids[i].vel.Y;

                //only add vectors for avoidance if they are too close
                if (distance(boid, boid.nearbyBoids[i]) < boid.detectRange / currWeights.avoidDivisor) 
                {
                    avoidVect.X += boid.pos.X - boid.nearbyBoids[i].pos.X;
                    avoidVect.Y += boid.pos.Y - boid.nearbyBoids[i].pos.Y;
                }

                flockPos.X += boid.nearbyBoids[i].pos.X;
                flockPos.Y += boid.nearbyBoids[i].pos.Y;
            }

            if (boid.nearbyBoids.Count != 0) //only divide the sums to find the avg if there is more than 0 nearby (avoids dividing by 0)
            {
                avgPosVect.X /= boid.nearbyBoids.Count;
                avgPosVect.Y /= boid.nearbyBoids.Count;
                avgPosVect.X -= boid.pos.X;
                avgPosVect.Y -= boid.pos.Y;

                avgVect.X /= boid.nearbyBoids.Count;
                avgVect.Y /= boid.nearbyBoids.Count;

                flockPos.X /= boid.nearbyBoids.Count;
                flockPos.Y /= boid.nearbyBoids.Count;
            }

            boid.flockPos = flockPos;

            //return the resultant vector (pretty i know)
            return new Hashtable {{"align", avgVect*currWeights.align}, {"cohere", avgPosVect*currWeights.cohere}, {"avoid", avoidVect*currWeights.avoid}};
        }
        public Vector2 avoidEdges(Boid boid) //logic to check if boids are out of / will go out of bounds
        {
            Vector2 output = Vector2.Zero;
            if (boid.pos.X >= cv.Width)
            {
                output.X += -1f;
            }
            if (boid.pos.X <= 0)
            {
                output.X += 1f;
            }
            if (boid.pos.Y >= cv.Height)
            {
                output.Y += -1f;
            }
            if (boid.pos.Y <= 0)
            {
                output.Y += 1f;
            }
            return output;
        }
        public void updateInfo() //updates nearbyboids list
        {
            
            for (int i = 0; i < boids.Count; i++)
            {
                //update info about boids, for each boid
                boids[i].nearbyBoids.Clear();
                for (var j = 0; j < boids.Count; j++)
                {
                    if (distance(boids[i], boids[j]) < boids[i].detectRange)
                    {
                        boids[i].nearbyBoids.Add(boids[j]);
                    }
                }
            }
        }

        public float distance(Boid boid1, Boid boid2) //find distance between two boids
        {
            return MathF.Sqrt(MathF.Abs(MathF.Pow(boid1.pos.X - boid2.pos.X, 2) + MathF.Pow(boid1.pos.Y - boid2.pos.Y, 2)));
        }
    }
    class Boid //generic boid class
    {
        public PointF pos;
        public Vector2 vel;

        public int detectRange;
        public List<Boid> nearbyBoids;
        public PointF flockPos;
        public Boid()
        {
            pos.X = new Random().Next(0, BoidLogic.cv.Width);
            pos.Y = new Random().Next(0, BoidLogic.cv.Height);

            vel.X = (float)(new Random().NextDouble() * 4) - 2f;
            vel.Y = (float)(new Random().NextDouble() * 4) - 2f;

            detectRange = 25;
            nearbyBoids = new List<Boid>();
        }
    }

    class Weights
    {
        public float align;
        public float avoid;
        public float avoidDivisor = 2;
        public float cohere;
        public Weights(float _align, float _cohere, float _avoid)
        {
            align = _align;
            avoid = _avoid;
            cohere = _cohere;
        }
    }
}