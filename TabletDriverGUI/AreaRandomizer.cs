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
        // Load configuration
        Configuration config;
        Random rng = new Random();
        Thread CalculateAreaThread;
        TabletDriver driver;

        Area minimumArea;
        Area maximumArea;

        public Area initialArea;
        public Area currentArea;
        long startTime;

        public bool run = false;

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
                minimumArea = initialArea * AREA_MULTIPLIER_MIN;
                maximumArea = AREA_MAXIMUM_BOUNDS;
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
            run = false;
            CalculateAreaThread.Interrupt();
            //UpdateArea(initialArea);
        }

        public void RunRandomizer()
        {
            if (!run) return;

            try
            {
                do
                {
                    int nextAreaTime = rng.Next(0, TIMESTEP_MAX - TIMESTEP_MIN) + TIMESTEP_MIN;
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

        // move to own configuration tab eventually...
        const int TIMESTEP_MIN = 15000;
        const int TIMESTEP_MAX = 30000;
        const double AREA_MULTIPLIER_MIN = 0.5;
        Area AREA_MAXIMUM_BOUNDS = new Area(115.555555555, 80, 0, 0);
        const double AREA_CHANGE_STD_DEV = 0.05;

        private Area CalculateNewArea(long deltaTime)
        {
            double newWidth;
            double newHeight;
            do
            {
                double variance = NextGaussian(0, AREA_CHANGE_STD_DEV);
                newWidth = currentArea.Width + initialArea.Width * variance;
                newHeight = currentArea.Height + initialArea.Height * variance;

            }
            while (newWidth < minimumArea.Width || newWidth > maximumArea.Width
                || newHeight < minimumArea.Height || newHeight > maximumArea.Height);

            double newX = initialArea.X;
            double newY = initialArea.Y;

            // TODO size bounds checking
            if (newX < newWidth / 2) newX = newWidth / 2;
            if (newY < newHeight / 2) newY = newHeight / 2;
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
