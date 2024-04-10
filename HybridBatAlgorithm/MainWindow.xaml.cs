using Microsoft.Win32;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Text.RegularExpressions;
using DataVis = System.Windows.Forms.DataVisualization;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Rebar;
using System.Drawing;
using System.Windows.Forms.DataVisualization.Charting;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;

namespace HybridBatAlgorithm
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //============================Модели================================

        private class Bat
        {
            public List<int> Routes { get; set; }
        }

        private class Point
        {
            public int[] Position { get; set; }
            public int Capacity { get; set; }
        }

        //========================Общие переменные==========================

        private string filePatch;

        private int maxCapacity = 0;

        List<Point> points = new List<Point>();
        //============================Алгоритм==============================

        private void OpenFile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "Файлы txt|*.txt";
            if (OPF.ShowDialog() == true)
            {
                filePatch = OPF.FileName;
            }
            ButtonChoseFile.Content = OPF.FileName;
        }

        private void Calculation(object sender, RoutedEventArgs e)
        {
            if (filePatch != null)
            {
                points = FileToVars(filePatch);
                int[,] distanceMatrix = DistanceMatrix(points);
                BatAlgorithm(distanceMatrix, points.Select(x => x.Capacity).ToArray());
            }
            else
            {
                MessageBox.Show("Файл не выбран", "", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private List<Point> FileToVars(string FilePatch)
        {
            List<Point> points = new List<Point>();
            string text = File.ReadAllText(FilePatch);
            string[] rows = text.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            bool coordSection = false;
            bool demandSection = false;
            foreach (string row in rows)
            {
                string[] words = row.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (words[0] == "CAPACITY") maxCapacity = int.Parse(words[2].Trim());
                if (words[0] == "DEPOT_SECTION") demandSection = false;
                if (demandSection) points[int.Parse(words[0].Trim()) - 1].Capacity = int.Parse(words[1].Trim());
                if (words[0] == "DEMAND_SECTION")
                {
                    demandSection = true;
                    coordSection = false;
                }
                if (coordSection)
                {
                    points.Add(
                        new Point
                        {
                            Position = new int[]
                            {
                                int.Parse(words[1].Trim()),
                                int.Parse(words[2].Trim())
                            }
                        });
                }
                if (words[0] == "NODE_COORD_SECTION") coordSection = true;

            }
            return points;
        }

        private int[,] DistanceMatrix(List<Point> points)
        {
            int[,] distanceMatrix = new int[points.Count, points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = 0; j < points.Count; j++)
                {
                    distanceMatrix[i, j] = ((int)Math.Round(Math.Sqrt(
                        Math.Pow(points[i].Position[0] - points[j].Position[0], 2) +
                        Math.Pow(points[i].Position[1] - points[j].Position[1], 2)
                        )));
                }
            }
            return distanceMatrix;
        }

        int[,] distances;
        int[] delivery;



        List<int> bestRoute = new List<int>();
        int minDistance = int.MaxValue;

        Random random = new Random();

        private List<Bat> InitializeBats(int numOfBats)
        {
            minDistance = int.MaxValue;
            bestRoute = new List<int>();
            List<Bat> bats = new List<Bat>();
            for (int i = 0; i < numOfBats; i++)
            {
                int currentLoad = 0;
                List<int> route = new List<int>();
                route.Add(0);
                List<int> randomRoute = Enumerable.Range(1, distances.GetLength(0) - 1).OrderBy(x => random.Next()).ToList();
                while (randomRoute.Count > 0)
                {
                    int indexRnd = random.Next(randomRoute.Count);
                    if (currentLoad + delivery[indexRnd] < maxCapacity)
                    {
                        route.Add(randomRoute[indexRnd]);
                        randomRoute.RemoveAt(indexRnd);
                        currentLoad += delivery[indexRnd];
                    }
                    else
                    {
                        currentLoad = 0;
                        route = route.Concat(new int[] { 0 }).ToList();
                    }
                }
                route.Add(0);

                bats.Add(new Bat { Routes = route });
            }
            return bats;
        }

        private int CalculateDistance(List<int> route)
        {
            int distance = 0;
            int totalDelivery = 0;
            int n = route.Count;

            for (int i = 0; i < n - 1; i++)
            {
                if (route[i] == 0)
                {
                    totalDelivery = 0;
                }

                if (totalDelivery + delivery[route[i]] <= maxCapacity)
                {
                    distance += distances[route[i], route[i + 1]] * delivery[route[i]];
                    totalDelivery += delivery[route[i]];
                }
                else
                {
                    return int.MaxValue;
                }
            }

            if (totalDelivery + delivery[route[n - 1]] <= maxCapacity)
            {
                distance += distances[route[n - 1], route[0]] * delivery[route[n - 1]];
                totalDelivery += delivery[route[n - 1]];
            }
            else
            {
                return int.MaxValue;
            }

            return distance;
        }

        private int CalculateStandartDistance(List<int> route)
        {
            int distance = 0;
            int n = route.Count;

            for (int i = 0; i < n - 1; i++)
            {
                distance += distances[route[i], route[i + 1]];
            }

            return distance;
        }

        private int AvgRowMatrix(int[,] matrix, int row)
        {
            int avg = 0;
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    avg += matrix[row, j];
                }
            }
            return avg / matrix.GetLength(0);
        }

        private List<int> GetRow(int[,] matrix, int index)
        {
            List<int> row = new List<int>();
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(0); j++)
                {
                    if (i != j && i == index)
                        row.Add(matrix[i, j]);
                }
            }
            return row;
        }

        private void MoveBat(List<Bat> bats, int batIndex, double rate)
        {
            Bat bat = bats[batIndex];
            List<int> newRoute = new List<int>(bat.Routes);
            newRoute = bat.Routes.Where(x => x != 0).ToList();

            for (int i = 1; i < newRoute.Count - 2; i++)
            {
                for (int j = i + 1; j < newRoute.Count - 1; j++)
                {
                    if (random.NextDouble() < rate)
                    {
                        int point1 = newRoute[i];
                        int point2 = newRoute[j];

                        List<int> reversedSubroute = newRoute.GetRange(i, j - i + 1).ToList();
                        reversedSubroute.Reverse();
                        newRoute.RemoveRange(i, j - i + 1);
                        newRoute.InsertRange(i, reversedSubroute);

                        newRoute = UpdateRouteWithCapacity(newRoute);

                        int currentDistance = CalculateDistance(bat.Routes);
                        int newDistance = CalculateDistance(newRoute);

                        if (newDistance <= currentDistance)
                        {
                            bat.Routes = newRoute;
                        }
                        newRoute = bat.Routes.Where(x => x != 0).ToList();
                    }
                }
            }
        }

        private List<int> UpdateRouteWithCapacity(List<int> route)
        {
            List<int> newRoute = new List<int>();
            int capacity = 0;

            newRoute.Add(0);
            foreach (int point in route)
            {
                if (point != 0)
                {
                    if (capacity + delivery[point] > maxCapacity)
                    {
                        newRoute.Add(0);
                        capacity = 0;
                    }

                    newRoute.Add(point);
                    capacity += delivery[point];
                }
                else
                {
                    newRoute.Add(0);
                    capacity = 0;
                }
            }

            if (newRoute[newRoute.Count - 1] != 0)
            {
                newRoute.Add(0);
            }

            return newRoute;
        }

        double greed = 0.3;
        private void BatAlgorithm(int[,] distanceMatrix, int[] delivery)
        {
            int numOfBats = int.TryParse(TB_CountBats.Text, out int value1) ? value1 : 5;
            TB_CountBats.Text = numOfBats.ToString();
            int numOfIterations = int.TryParse(TB_CountIterations.Text, out int value2) ? value2 : 100;
            TB_CountIterations.Text = numOfIterations.ToString();
            double pulseRate = double.TryParse(TB_PulseRate.Text, out double value3) ? value3 : 0.5;
            TB_PulseRate.Text = pulseRate.ToString();
            double loudness = double.TryParse(TB_Loudness.Text, out double value4) ? value4 : 0.8;
            TB_Loudness.Text = loudness.ToString();
            double minLoudness = double.TryParse(TB_MinLoudness.Text, out double value5) ? value5 : 0.1;
            TB_MinLoudness.Text = minLoudness.ToString();
            double alpha = double.TryParse(TB_Alpha.Text, out double value6) ? value6 : 0.9;
            TB_Alpha.Text = alpha.ToString();
            double gamma = double.TryParse(TB_Gamma.Text, out double value7) ? value7 : 0.9;
            TB_Gamma.Text = gamma.ToString();
            //greed = double.TryParse(TB_Greed.Text, out double value8) ? value8 : 0.3;
            //TB_Greed.Text = greed.ToString();
            distances = distanceMatrix;
            this.delivery = delivery;


            List<Bat> bats = InitializeBats(numOfBats);

            for (int iter = 0; iter < numOfIterations; iter++)
            {
                foreach (var bat in bats.Select((value, index) => new { value, index }))
                {
                    MoveBat(bats, bat.index, pulseRate);
                }

                foreach (var bat in bats)
                {
                    double rand = random.NextDouble();
                    if (rand < loudness && CalculateDistance(bat.Routes) < minDistance)
                    {
                        minDistance = CalculateDistance(bat.Routes);
                        bestRoute.Clear();
                        bestRoute.AddRange(bat.Routes);
                    }

                    loudness *= alpha;
                    pulseRate = 1 - Math.Exp(-gamma * iter);
                    if (loudness < minLoudness)
                    {
                        loudness = 0.5;
                    }
                }
            }

            Graph.Series[0].Points.Clear();
            int k = 0;
            Random rnd = new Random();
            int color = 0;
            foreach (int value in bestRoute)
            {
                Graph.Series[0].Points.AddXY(points[value].Position[0], points[value].Position[1]);
                Graph.Series[0].Points[k].Color = System.Drawing.ColorTranslator.FromOle(color);
                Graph.Series[0].Points[k].BorderWidth = 2;
                Graph.Series[0].Points[k].MarkerColor = System.Drawing.Color.Red;
                Graph.Series[0].Points[k].MarkerStyle = DataVis.Charting.MarkerStyle.Circle;
                Graph.Series[0].Points[k].MarkerSize = 10;
                Graph.Series[0].Points[k].ToolTip = value.ToString();
                if (value == 0)
                {
                    color = rnd.Next(999999);
                }
                k++;
            }
            ResultBlock.Text = ("Лучший путь: " + string.Join(" -> ", bestRoute));
            ResultBlock.Text += ("\nДистанция: " + CalculateStandartDistance(bestRoute));
        }



    }
}
