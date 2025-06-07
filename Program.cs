using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace QueuingSystemSimulation
{
    class Simulation
    {
        private static readonly List<double> arrivalRates = new List<double>();
        private static readonly List<double> theoIdleProbs = new List<double>();
        private static readonly List<double> expIdleProbs = new List<double>();
        private static readonly List<double> theoRejectProbs = new List<double>();
        private static readonly List<double> expRejectProbs = new List<double>();
        private static readonly List<double> theoThroughputs = new List<double>();
        private static readonly List<double> expThroughputs = new List<double>();
        private static readonly List<double> theoAbsThroughputs = new List<double>();
        private static readonly List<double> expAbsThroughputs = new List<double>();
        private static readonly List<double> theoAvgBusyChannels = new List<double>();
        private static readonly List<double> expAvgBusyChannels = new List<double>();

        static void Main(string[] args)
        {
            try
            {
                ExecuteSimulation();
                GeneratePlots();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка выполнения программы: {ex.Message}");
            }
        }

        static void ExecuteSimulation()
        {
            const double serviceRate = 1.0;
            const int channels = 5;
            const int totalQueries = 25;
            const double minArrivalRate = 0.1;
            const double maxArrivalRate = 10.0;
            const double stepArrivalRate = 0.1;

            arrivalRates.Clear();
            theoIdleProbs.Clear();
            expIdleProbs.Clear();
            theoRejectProbs.Clear();
            expRejectProbs.Clear();
            theoThroughputs.Clear();
            expThroughputs.Clear();
            theoAbsThroughputs.Clear();
            expAbsThroughputs.Clear();
            theoAvgBusyChannels.Clear();
            expAvgBusyChannels.Clear();

            for (double arrivalRate = minArrivalRate; arrivalRate <= maxArrivalRate; arrivalRate += stepArrivalRate)
            {
                PerformSingleRun(Math.Round(arrivalRate, 1), serviceRate, channels, totalQueries);
            }
        }

        static void PerformSingleRun(double arrivalRate, double serviceRate, int channels, int totalQueries)
        {
            Console.WriteLine($"\nМоделирование для λ = {arrivalRate}, μ = {serviceRate}");

            var queryProcessor = new QueryProcessor(channels, serviceRate);
            var queryGenerator = new QueryGenerator(queryProcessor);

            DispatchQueries(queryGenerator, arrivalRate, totalQueries);
            AwaitProcessor(queryProcessor);

            ComputeAndStoreResults(arrivalRate, serviceRate, channels, queryProcessor);
        }

        static void DispatchQueries(QueryGenerator generator, double arrivalRate, int totalQueries)
        {
            for (int queryId = 1; queryId <= totalQueries; queryId++)
            {
                generator.Generate(queryId);
                Thread.Sleep((int)(1000 / arrivalRate));
            }
        }

        static void AwaitProcessor(QueryProcessor processor)
        {
            while (processor.ActiveChannels() > 0)
            {
                Thread.Sleep(100);
            }
        }

        static void ComputeAndStoreResults(double arrivalRate, double serviceRate, int channels, QueryProcessor processor)
        {
            double load = arrivalRate / serviceRate;
            double idleProbability = ComputeIdleProbability(load, channels);
            double rejectionProbability = ComputeRejectionProbability(load, channels, idleProbability);
            double throughput = 1 - rejectionProbability;
            double absoluteThroughput = arrivalRate * throughput;
            double avgBusyChannels = load * throughput;

            double expIdleProbability = processor.CalculateIdleProbability();
            double expRejectionProbability = processor.CalculateRejectionProbability();
            double expThroughput = processor.CalculateThroughput();
            double expAbsoluteThroughput = arrivalRate * expThroughput;
            double expAvgBusyChannelsValue = processor.CalculateAverageBusyChannels(serviceRate);

            arrivalRates.Add(arrivalRate);
            theoIdleProbs.Add(idleProbability);
            expIdleProbs.Add(expIdleProbability);
            theoRejectProbs.Add(rejectionProbability);
            expRejectProbs.Add(expRejectionProbability);
            theoThroughputs.Add(throughput);
            expThroughputs.Add(expThroughput);
            theoAbsThroughputs.Add(absoluteThroughput);
            expAbsThroughputs.Add(expAbsoluteThroughput);
            theoAvgBusyChannels.Add(avgBusyChannels);
            expAvgBusyChannels.Add(expAvgBusyChannelsValue);

            StoreResults(arrivalRate, serviceRate, idleProbability, rejectionProbability, throughput,
                         absoluteThroughput, avgBusyChannels, expIdleProbability, expRejectionProbability,
                         expThroughput, expAbsoluteThroughput, expAvgBusyChannelsValue);
        }

        static double ComputeIdleProbability(double load, int channels)
        {
            double sum = 0;
            for (int i = 0; i <= channels; i++)
            {
                sum += Math.Pow(load, i) / Factorial(i);
            }
            return sum > 0 ? 1 / sum : 0;
        }

        static double ComputeRejectionProbability(double load, int channels, double idleProbability)
        {
            return Math.Pow(load, channels) / Factorial(channels) * idleProbability;
        }

        static double Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);

        static string FormatResults(double arrivalRate, double serviceRate,
                                   double idleProb, double rejectProb, double throughput,
                                   double absThroughput, double avgBusyChannels,
                                   double expIdleProb, double expRejectProb, double expThroughput,
                                   double expAbsThroughput, double expAvgBusyChannels)
        {
            return $"{arrivalRate:F1} {serviceRate:F1} {idleProb:F4} {rejectProb:F4} {throughput:F4} " +
                   $"{absThroughput:F4} {avgBusyChannels:F4} {expIdleProb:F4} {expRejectProb:F4} " +
                   $"{expThroughput:F4} {expAbsThroughput:F4} {expAvgBusyChannels:F4}";
        }

        static void StoreResults(double arrivalRate, double serviceRate,
                                double idleProb, double rejectProb, double throughput,
                                double absThroughput, double avgBusyChannels,
                                double expIdleProb, double expRejectProb, double expThroughput,
                                double expAbsThroughput, double expAvgBusyChannels)
        {
            try
            {
                string filePath = Path.Combine(Environment.CurrentDirectory, "results.txt");
                string resultLine = FormatResults(arrivalRate, serviceRate, idleProb, rejectProb, throughput,
                                                 absThroughput, avgBusyChannels, expIdleProb, expRejectProb,
                                                 expThroughput, expAbsThroughput, expAvgBusyChannels);
                File.AppendAllText(filePath, resultLine + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при записи в файл: {ex.Message}");
            }
        }

        static void GeneratePlots()
        {
            string resultDir = Path.Combine(Environment.CurrentDirectory, "result");
            try
            {
                Directory.CreateDirectory(resultDir);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании папки result: {ex.Message}");
                return;
            }

            int width = 800;
            int height = 600;
            int margin = 80;
            int plotWidth = width - 2 * margin;
            int plotHeight = height - 2 * margin;

            double xMin = 0, xMax = 10;
            string[] titles = {
                "Вероятность простоя системы (P0)",
                "Вероятность отказа системы (Pn)",
                "Относительная пропускная способность (Q)",
                "Абсолютная пропускная способность (A)",
                "Среднее число занятых каналов (k)"
            };
            List<List<double>> theoData = new List<List<double>> { theoIdleProbs, theoRejectProbs, theoThroughputs, theoAbsThroughputs, theoAvgBusyChannels };
            List<List<double>> expData = new List<List<double>> { expIdleProbs, expRejectProbs, expThroughputs, expAbsThroughputs, expAvgBusyChannels };
            string[] yLabels = { "P0", "Pn", "Q", "A", "k" };

            for (int i = 0; i < 5; i++)
            {
                double yMax = 1.0;
                if (theoData[i].Count > 0 && expData[i].Count > 0)
                {
                    yMax = Math.Max(theoData[i].Max(), expData[i].Max());
                    yMax = Math.Ceiling(yMax * 1.1);
                }

                try
                {
                    using (Bitmap bmp = new Bitmap(width, height))
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.Clear(Color.White);
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                        using (Pen axisPen = new Pen(Color.Black, 2))
                        {
                            g.DrawLine(axisPen, margin, margin, margin, height - margin);
                            g.DrawLine(axisPen, margin, height - margin, width - margin, height - margin);
                        }

                        using (Font font = new Font("Arial", 10))
                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            g.DrawString(titles[i], new Font("Arial", 12, FontStyle.Bold), brush, new PointF(margin, 20));
                            g.DrawString("Интенсивность входного потока (λ)", font, brush, new PointF(margin + plotWidth / 2 - 50, height - 40));
                            g.DrawString(yLabels[i], font, brush, new PointF(20, margin + plotHeight / 2 - 10));
                        }

                        using (Font font = new Font("Arial", 8))
                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            for (int j = 0; j <= 10; j++)
                            {
                                float x = margin + (j / 10.0f) * plotWidth;
                                g.DrawLine(Pens.Black, x, height - margin, x, height - margin + 5);
                                g.DrawString(j.ToString(), font, brush, x - 5, height - margin + 10);
                            }

                            for (int j = 0; j <= 5; j++)
                            {
                                float y = height - margin - (j / 5.0f) * plotHeight;
                                g.DrawLine(Pens.Black, margin - 5, y, margin, y);
                                g.DrawString((j * yMax / 5.0).ToString("F1"), font, brush, margin - 40, y - 5);
                            }
                        }

                        if (arrivalRates.Count > 0 && theoData[i].Count == arrivalRates.Count && expData[i].Count == arrivalRates.Count)
                        {
                            using (Pen theoPen = new Pen(Color.Blue, 2))
                            using (Pen expPen = new Pen(Color.Red, 2))
                            {
                                PointF[] theoPoints = new PointF[arrivalRates.Count];
                                PointF[] expPoints = new PointF[arrivalRates.Count];

                                for (int j = 0; j < arrivalRates.Count; j++)
                                {
                                    float x = margin + (float)((arrivalRates[j] - xMin) / (xMax - xMin)) * plotWidth;
                                    float theoY = height - margin - (float)(theoData[i][j] / yMax) * plotHeight;
                                    float expY = height - margin - (float)(expData[i][j] / yMax) * plotHeight;
                                    theoPoints[j] = new PointF(x, theoY);
                                    expPoints[j] = new PointF(x, expY);
                                }

                                g.DrawLines(theoPen, theoPoints);
                                g.DrawLines(expPen, expPoints);
                            }
                        }

                        using (Font font = new Font("Arial", 10))
                        using (Brush brush = new SolidBrush(Color.Black))
                        {
                            g.DrawString("Теоретическая", font, brush, new PointF(width - margin - 100, margin));
                            g.DrawString("Экспериментальная", font, brush, new PointF(width - margin - 100, margin + 20));
                            g.DrawLine(new Pen(Color.Blue, 2), width - margin - 120, margin + 5, width - margin - 140, margin + 5);
                            g.DrawLine(new Pen(Color.Red, 2), width - margin - 120, margin + 25, width - margin - 140, margin + 25);
                        }

                        bmp.Save(Path.Combine(resultDir, $"p-{i + 1}.png"), ImageFormat.Png);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при создании графика p-{i + 1}.png: {ex.Message}");
                }
            }

            Console.WriteLine("Графики сохранены в папке result/");
        }
    }

    struct ChannelInfo
    {
        public Thread Thread { get; set; }
        public bool IsOccupied { get; set; }
    }

    class QueryProcessor
    {
        private readonly ChannelInfo[] channels;
        private readonly object lockObject = new object();
        private readonly DateTime[] channelStartTimes;
        private readonly DateTime simulationStart;
        private readonly double serviceRate;
        private DateTime lastIdleCheck;

        public int TotalQueries { get; private set; }
        public int ProcessedQueries { get; private set; }
        public int RejectedQueries { get; private set; }
        public double TotalBusyTime { get; private set; }
        public double TotalIdleTime { get; private set; }
        public double TotalTime { get; private set; }

        public QueryProcessor(int channelCount, double serviceRate)
        {
            channels = new ChannelInfo[channelCount];
            channelStartTimes = new DateTime[channelCount];
            this.serviceRate = serviceRate;
            simulationStart = DateTime.Now;
            lastIdleCheck = simulationStart;
        }

        public void HandleQuery(object sender, QueryEventArgs args)
        {
            lock (lockObject)
            {
                TotalQueries++;
                TotalTime = (DateTime.Now - simulationStart).TotalMilliseconds / 1000.0;


                if (ActiveChannels() == 0)
                {
                    TotalIdleTime += (DateTime.Now - lastIdleCheck).TotalMilliseconds / 1000.0;
                }
                lastIdleCheck = DateTime.Now;

                Console.WriteLine($"Запрос #{args.QueryId} поступил на сервер");
                ProcessOrRejectQuery(args.QueryId);
            }
        }

        private bool TryFindFreeChannel(out int channelIndex)
        {
            for (int i = 0; i < channels.Length; i++)
            {
                if (!channels[i].IsOccupied)
                {
                    channelIndex = i;
                    return true;
                }
            }
            channelIndex = -1;
            return false;
        }

        private void ProcessOrRejectQuery(int queryId)
        {
            if (TryFindFreeChannel(out int channelIndex))
            {
                ProcessQuery(queryId, channelIndex);
            }
            else
            {
                RejectedQueries++;
                Console.WriteLine($"Запрос #{queryId} отклонен");
            }
        }

        private void ProcessQuery(int queryId, int channelIndex)
        {
            channels[channelIndex].IsOccupied = true;
            channelStartTimes[channelIndex] = DateTime.Now;
            ProcessedQueries++;
            Console.WriteLine($"Запрос #{queryId} принят в канал {channelIndex + 1}");

            channels[channelIndex].Thread = new Thread(() =>
            {
                Console.WriteLine($"Обработка запроса #{queryId} начата");
                Thread.Sleep((int)(1000 / serviceRate));

                lock (lockObject)
                {
                    TotalBusyTime += (DateTime.Now - channelStartTimes[channelIndex]).TotalMilliseconds / 1000.0;
                    channels[channelIndex].IsOccupied = false;
                    Console.WriteLine($"Запрос #{queryId} обработан в канале {channelIndex + 1}");
                }
            });
            channels[channelIndex].Thread.Start();
        }

        public int ActiveChannels()
        {
            int active = 0;
            foreach (var channel in channels)
            {
                if (channel.IsOccupied) active++;
            }
            return active;
        }

        public double CalculateIdleProbability() => TotalTime > 0 ? TotalIdleTime / TotalTime : 0;
        public double CalculateRejectionProbability() => TotalQueries > 0 ? (double)RejectedQueries / TotalQueries : 0;
        public double CalculateThroughput() => TotalQueries > 0 ? (double)ProcessedQueries / TotalQueries : 0;
        public double CalculateAverageBusyChannels(double serviceRate) => TotalTime > 0 ? TotalBusyTime / (TotalTime * serviceRate) : 0;
    }

    class QueryGenerator
    {
        public event EventHandler<QueryEventArgs> QueryReceived;

        public QueryGenerator(QueryProcessor processor)
        {
            QueryReceived += processor.HandleQuery;
        }

        public void Generate(int queryId)
        {
            QueryReceived?.Invoke(this, new QueryEventArgs { QueryId = queryId });
        }
    }

    class QueryEventArgs : EventArgs
    {
        public int QueryId { get; set; }
    }
}