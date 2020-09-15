﻿using System.Diagnostics;

namespace Coictus
{
    /*This class is responasble for providing game loop timings so that the game runs
      at a consistant rate over multiple fps rates and refresh rates.*/
    public static class TicksAndFps
    {
        private static Stopwatch stopwatch = new Stopwatch();
        private static long currentTime = 0;
        private static long lastTime = 0;
        private static double deltaTime = 0.005D;
        private static int ticksPerSecond;
        private static int ticksElapsed = 0;
        private static double timePerTick;
        private static double percentToNextTick; //a decimal value between 0 and 1 which can be used as a percentage of progress towards next tick, usefull for interpolation.
        private static bool paused = false; //true when game is paused

        private static float[] recordedFrameTimes = new float[100];//used for calculating average FPS per frame
        private static float combinedFrameTimes = 0;
        private static int frameUpdateIndex = 0;
        public static void init(int tps)
        {
            ticksPerSecond = tps;
            timePerTick = 1 / (double)ticksPerSecond;
            stopwatch.Start();
            lastTime = stopwatch.ElapsedMilliseconds;
        }

        /*called every frame*/
        public static void update()
        {
            /*updating FPS*/
            lastTime = currentTime;
            currentTime = stopwatch.ElapsedMilliseconds;
            deltaTime = (currentTime - lastTime) / 1000.0F;

            combinedFrameTimes -= recordedFrameTimes[frameUpdateIndex];
            combinedFrameTimes += (float)deltaTime;
            recordedFrameTimes[frameUpdateIndex++] = (float)deltaTime;
            frameUpdateIndex %= 100;


            if (paused)
            {
                ticksElapsed = 0;
            }
            else
            {
                /*updating Ticks*/

                /*resets tick counts if atleast 1 tick time has passed. Update() must only be called after using ticksElapsed to tick the game. This way we can
                  access the accumulated tick value since the last update before it resets.*/
                if (ticksElapsed > 0)
                {
                    ticksElapsed = 0;
                    percentToNextTick = 0;
                }


                /*increase partialTicks by the time passed divided by time per tick. Time per tick is calulcated in constructor and is a constant.
                In other words, How many "time per ticks"'s has passed since the last update? Each time the time of one tick is passed (30 tps is 0.0333333 seconds)
                parialTicks is increased by 1.*/
                percentToNextTick += deltaTime / timePerTick;
                /*ticksElapsed is equal to the number of times the passed time has reached the full duration of one tick (at 30 ticks per second, thats 0.0333333)*/
                ticksElapsed = (int)percentToNextTick;

                /*limit ticks elapsed to a second's worth, so the game does not speed out of control in some laggy circumstances
                  In other words, the game will only "catch up" by half a second at a time. Any hangs more than half a second will not 
                  be corrected for.*/
                if (ticksElapsed > ticksPerSecond)
                {
                    ticksElapsed = ticksPerSecond;
                }
            }
        }

        public static void pause()
        {
            paused = true;
        }

        public static void unPause()
        {
            paused = false;
        }
       
        public static int getTicksElapsed()
        {
            return ticksElapsed;
        }

        public static float getNumOfTicksForSeconds(float seconds)
        {
            return (float) (seconds * ticksPerSecond);
        }
        public static double getFrameTickPercentage()
        {
           return deltaTime / timePerTick;
        }
        public static float getPercentageToNextTick()
        {
            return (float)percentToNextTick;
        }

        public static long getCurrentTime()
        {
            return stopwatch.ElapsedMilliseconds;
        }

        /*ticks per second*/
        public static int tps { get => ticksPerSecond; }

        /*seconds per tick*/
        public static float spt { get => 1F / (float)ticksPerSecond; }

        /*frames per second*/
        public static int fps { get => (int)( 1F / (combinedFrameTimes / 100F)); }

        /*seconds per frame*/
        public static float spf { get => 1F / (float)fps; }

    }
}
