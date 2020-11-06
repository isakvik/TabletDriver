using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace TabletDriverGUI
{
    public partial class MainWindow : Window
    {

        // Randomizer
        private AreaRandomizer randomizer;


        private void RandomizerStatusChanged(object sender, RoutedEventArgs e)
        {
            if (isLoadingSettings) return;

            if (!randomizer.run && (bool)checkBoxEnableRandomizer.IsChecked)
                randomizer.Start();
            else
                randomizer.Stop();
        }

        private void RandomizerForceProportionsChanged(object sender, RoutedEventArgs e)
        {
            AreaRandomizer.forceProportions = (bool)checkBoxRandomizerForceProportions.IsChecked;
        }

        private void RandomizerDarkModeChanged(object sender, RoutedEventArgs e)
        {
            UpdateRandomizerDarkModeElements((bool)checkBoxDarkMode.IsChecked);
        }


        private void RandomizerTextChanged(object sender, RoutedEventArgs e)
        {
            if (Utils.ParseNumber(textBoxRandomizerWidthMin.Text, out double value))
                AreaRandomizer.areaBoundsMin.Width = value;
            if (Utils.ParseNumber(textBoxRandomizerHeightMin.Text, out value))
                AreaRandomizer.areaBoundsMin.Height = value;
            if (Utils.ParseNumber(textBoxRandomizerWidthMax.Text, out value))
                AreaRandomizer.areaBoundsMax.Width = value;
            if (Utils.ParseNumber(textBoxRandomizerHeightMax.Text, out value))
                AreaRandomizer.areaBoundsMax.Height = value;
            if (Utils.ParseNumber(textBoxRandomizerTimeMin.Text, out value))
                AreaRandomizer.timestepMin = (int)(value * 1000);
            if (Utils.ParseNumber(textBoxRandomizerTimeMax.Text, out value))
                AreaRandomizer.timestepMax = (int)(value * 1000);
            if (Utils.ParseNumber(textBoxRandomizerStdDev.Text, out value))
                AreaRandomizer.areaChangeStdDev = value / 100.0;

            UpdateSettingsToConfiguration();
        }

        //
        // Area update timer tick (randomizer)
        //
        private void TimerAreaUpdate_Tick(object sender, EventArgs e)
        {
            if (tabControl.SelectedItem == tabRandomizer && WindowState != WindowState.Minimized)
            {
                UpdateRandomizerAreaCanvas();
                UpdateRandomizerAreaInformation();
            }
        }


        void UpdateRandomizerAreaCanvas()
        {
            double fullWidth = config.TabletFullArea.Width;
            double fullHeight = config.TabletFullArea.Height;

            // Canvas element scaling
            double scaleX = (canvasTabletArea.ActualWidth - 2) / fullWidth;
            double scaleY = (canvasTabletArea.ActualHeight - 2) / fullHeight;
            double scale = scaleX;
            if (scaleX > scaleY)
                scale = scaleY;

            double offsetX = canvasTabletArea.ActualWidth / 2.0 - fullWidth * scale / 2.0;
            double offsetY = canvasTabletArea.ActualHeight / 2.0 - fullHeight * scale / 2.0;

            //
            // Tablet full area
            //
            Point[] corners = config.TabletFullArea.Corners;
            for (int i = 0; i < 4; i++)
            {
                Point p = corners[i];
                p.X *= scale;
                p.Y *= scale;
                p.X += config.TabletFullArea.X * scale + offsetX;
                p.Y += config.TabletFullArea.Y * scale + offsetY;
                polygonTabletFullArea.Points[i] = p;
                polygonRandomizerTabletFullArea.Points[i] = p;
            }

            //
            // Tablet area
            //
            corners = config.TabletAreas[0].Corners;
            for (int j = 0; j < 4; j++)
            {
                Point p = corners[j];
                p.X *= scale;
                p.Y *= scale;
                p.X += config.TabletAreas[0].X * scale + offsetX;
                p.Y += config.TabletAreas[0].Y * scale + offsetY;
                polygonRandomizerTabletArea.Points[j] = p;
            }


            //
            // Tablet area arrow
            //
            polygonRandomizerAreaArrow.Points[0] = new Point(
                offsetX + config.TabletAreas[0].X * scale,
                offsetY + config.TabletAreas[0].Y * scale
            );
            polygonRandomizerAreaArrow.Points[1] = new Point(
                offsetX + corners[2].X * scale + config.TabletAreas[0].X * scale,
                offsetY + corners[2].Y * scale + config.TabletAreas[0].Y * scale
            );
            polygonRandomizerAreaArrow.Points[2] = new Point(
                offsetX + corners[3].X * scale + config.TabletAreas[0].X * scale,
                offsetY + corners[3].Y * scale + config.TabletAreas[0].Y * scale
            );

            canvasRandomizerArea.InvalidateVisual();

        }

        void UpdateRandomizerAreaInformation()
        {
            // Tablet area
            labelRandomizerAreaInfo.Content = 
                Utils.GetNumberString(config.SelectedTabletArea.Width) + "mm x " +
                Utils.GetNumberString(config.SelectedTabletArea.Height) + "mm | X=" +
                Utils.GetNumberString(config.SelectedTabletArea.X) + " Y=" +
                Utils.GetNumberString(config.SelectedTabletArea.Y) + " | " +
                Utils.GetNumberString(config.SelectedTabletArea.Width / config.SelectedTabletArea.Height, "0.000") + ":1"
                /*Utils.GetNumberString(config.SelectedTabletArea.Width * config.SelectedTabletArea.Height, "0") + " mm² " +
                Utils.GetNumberString(
                    config.SelectedTabletArea.Width * config.SelectedTabletArea.Height /
                    (config.TabletFullArea.Width * config.TabletFullArea.Height) * 100.0
                    , "0") + "% of " +
                Utils.GetNumberString(config.TabletFullArea.Width) + "x" + Utils.GetNumberString(config.TabletFullArea.Height) + " mm | " +
                Utils.GetNumberString(config.SelectedScreenArea.Width / config.SelectedTabletArea.Width, "0.0") + "x" +
                Utils.GetNumberString(config.SelectedScreenArea.Height / config.SelectedTabletArea.Height, "0.0") + " px/mm"*/;
        }

        void UpdateRandomizerDarkModeElements(bool darkmodeEnabled)
        {
            if (darkmodeEnabled)
            {
                stackPanelRandomizerArea.Background = Brushes.Black;

                textRandomizerArea.Foreground = Brushes.White;
                polygonRandomizerTabletFullArea.Stroke = new SolidColorBrush(Color.FromRgb(155,155,155));
                polygonRandomizerTabletArea.Stroke = Brushes.White;
                polygonRandomizerAreaArrow.Fill = new SolidColorBrush(Color.FromArgb(50, 235, 235, 235));
                labelRandomizerAreaInfo.Foreground = Brushes.White;
            }
            else
            {
                stackPanelRandomizerArea.Background = Brushes.Transparent;

                textRandomizerArea.Foreground = Brushes.Black;
                polygonRandomizerTabletFullArea.Stroke = new SolidColorBrush(Color.FromRgb(100,100,100));
                polygonRandomizerTabletArea.Stroke = Brushes.Black;
                polygonRandomizerAreaArrow.Fill = new SolidColorBrush(Color.FromArgb(50, 20, 20, 20));
                labelRandomizerAreaInfo.Foreground = Brushes.Black;
            }
            //this.randomizerDarkMode = darkmodeEnabled;
        }

        //private Polygon polygonRandomizerTabletFullArea;
        //private Polygon polygonRandomizerTabletArea;
        //private Polygon polygonRandomizerAreaArrow;
    }
}