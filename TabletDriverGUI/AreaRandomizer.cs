using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace TabletDriverGUI
{
    public class AreaRandomizer
    {
        Configuration config;
        Random rng = new Random();
        Thread CalculateAreaThread;
        TabletDriver driver;

        public Area initialArea;
        public Area currentArea;
        long startTime;

        public bool run = false;

        // configurable settings
        public static int timestepMin = 15000;
        public static int timestepMax = 30000;
        public static Area areaBoundsMin = new Area(36, 26.30, 0, 0);
        public static Area areaBoundsMax = new Area(115.555555555, 80, 0, 0);
        public static double areaChangeStdDev = 0.1;
        public static bool forceProportions = true;

        public AreaRandomizer()
        {
        }

        public void Initialize(TabletDriver driver, Configuration config)
        {
            this.driver = driver;
            this.config = config;

            if (initialArea == null)
            {
                initialArea = new Area(config.TabletAreas?[0]);
                currentArea = new Area(initialArea);
                //maximumArea = config.TabletFullArea;
            }
        }

        public void Start()
        {
            UpdateArea(initialArea);
            startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (!run)
            {
                run = true;
                this.CalculateAreaThread = new Thread(new ThreadStart(RunRandomizer));
                CalculateAreaThread.Start();
            }
        }

        public void Stop()
        {
            if (!run) return;

            run = false;
            CalculateAreaThread.Interrupt();

            currentArea.Set(initialArea);
            config.TabletArea?.Set(initialArea);
            config.TabletAreas[0]?.Set(initialArea);
        }

        public void RunRandomizer()
        {
            if (!run) return;

            try
            {
                do
                {
                    int nextAreaTime = rng.Next(0, timestepMax - timestepMin) + timestepMin;
                    long deltaTime = DateTimeOffset.Now.ToUnixTimeMilliseconds() - startTime;

                    Area newArea = CalculateNewArea(deltaTime);
                    UpdateArea(newArea);

                    Thread.Sleep(nextAreaTime);
                }
                while (run);
            }
            catch (ThreadInterruptedException tie) { }
        }

        private void UpdateArea(Area newArea)
        {
            currentArea.Set(newArea);
            config.TabletArea?.Set(newArea);
            config.TabletAreas[0]?.Set(newArea);
            driver.SendCommand(GetCommandFromArea(newArea));
        }

        private string GetCommandFromArea(Area area)
        {
            return string.Format("TabletArea {0} {1} {2} {3}",
                Utils.GetNumberString(area.Width),
                Utils.GetNumberString(area.Height),
                Utils.GetNumberString(area.X),
                Utils.GetNumberString(area.Y));
        }

        


        //
        // Math section
        //


        private Area CalculateNewArea(long deltaTime)
        {
            double newWidth = -1;
            double newHeight = -1;

            double setWidth = -1;
            double setHeight = -1;
            do
            {
                if (forceProportions)
                {
                    double variance = NextGaussian(0, areaChangeStdDev);
                    newWidth = currentArea.Width + initialArea.Width * variance;
                    newHeight = currentArea.Height + initialArea.Height * variance;
                }
                else
                {
                    if (setWidth < areaBoundsMin.Width || setWidth > areaBoundsMax.Width)
                    {
                        setWidth = newWidth = currentArea.Width + initialArea.Width * NextGaussian(0, areaChangeStdDev);
                    }

                    if (setHeight < areaBoundsMin.Height || setHeight > areaBoundsMax.Height)
                    {
                        setHeight = newHeight = currentArea.Height + initialArea.Height * NextGaussian(0, areaChangeStdDev);
                    }
                }
            }
            while (newWidth < areaBoundsMin.Width || newWidth > areaBoundsMax.Width
                || newHeight < areaBoundsMin.Height || newHeight > areaBoundsMax.Height);

            double newX = initialArea.X;
            double newY = initialArea.Y;

            if (newX < newWidth / 2) 
                newX = newWidth / 2;
            if (newY < newHeight / 2) 
                newY = newHeight / 2;
            Area newArea = new Area(newWidth, newHeight, newX, newY);
            return newArea;
        }

        // from Superbest_random extensions
        public double NextGaussian(double mu = 0, double sigma = 1)
        {
            var u1 = rng.NextDouble();
            var u2 = rng.NextDouble();

            var rand_std_normal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                                Math.Sin(2.0 * Math.PI * u2);

            var rand_normal = mu + sigma * rand_std_normal;

            return rand_normal;
        }

    }
}
