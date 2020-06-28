using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace HowRandomIsRandom
{
    static class Program
    {
        const int _uniqueNumbers = 10;
        const int _numbersToGenerate = int.MaxValue;
        static readonly int _threads = Environment.ProcessorCount;
        static int[] _numbers = new int[_uniqueNumbers];

        //Progress bar variables
        static event EventHandler _progressUpdate;
        static int _totalProgress;
        private static readonly object _progressLock = new object();

        static async Task Main(string[] args)
        {
            _progressUpdate += ProgressHandler;

            Console.WriteLine("Generating " + _numbersToGenerate + " random numbers between 0 and " + (_uniqueNumbers - 1) + " using " + _threads + " threads.");
            for (int i = 0; i < 100; i++)
            {
                Console.Write("#");
            }
            Console.WriteLine();

            var parallelWatch = Stopwatch.StartNew();
            _numbers = await GenerationTasks();
            parallelWatch.Stop();
            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("num\t\tcount\t\toffby\t\tpercent");
            double totalPercentOff = 0;
            for (int i = 0; i < _uniqueNumbers; i++)
            {
                double offBy = Math.Abs(_numbersToGenerate / _uniqueNumbers - _numbers[i]);
                double percent = offBy / (_numbersToGenerate / _uniqueNumbers) * 100;
                totalPercentOff += percent;

                Console.WriteLine(i + "\t\t" + _numbers[i] + "\t\t" + offBy + "\t\t" + percent + "%");
            }
            Console.WriteLine();
            Console.WriteLine("On average off by: " + totalPercentOff / _uniqueNumbers + "%");
            Console.WriteLine("Generation time: " + parallelWatch.ElapsedMilliseconds / 1000 + " seconds.");
            Console.WriteLine("Total progress updates: " + _totalProgress);
            var numbersGenerated = 0;
            for (int i = 0; i < _numbers.Length; i++)
            {
                numbersGenerated += _numbers[i];
            }

            Console.WriteLine("Numbers generated: " + numbersGenerated);
            Console.ReadKey();
        }

        private static void ProgressHandler(object sender, EventArgs e)
        {
            lock (_progressLock)
            {
                _totalProgress++;

                if (_totalProgress % _threads == 0)
                {
                    Console.Write("#");
                }
            }
        }

        private static int[] GenerateRandomNumbers(int workToDo)
        {
            var result = new int[_uniqueNumbers];

            var random = new Random();
            var percentDone = workToDo / 100;

            if (_threads == 1)
            {
                for (long i = 1; i <= workToDo; i++)
                {
                    if (percentDone > 0 && i % percentDone == 0)
                    {
                        _progressUpdate.Invoke(null, null);
                    }
                    result[random.Next(0, _uniqueNumbers)]++;
                }
            }
            else
            {
                for (int i = 1; i <= workToDo; i++)
                {
                    if (percentDone > 0 && i % percentDone == 0)
                    {
                        _progressUpdate.Invoke(null, null);
                    }
                    result[random.Next(0, _uniqueNumbers)]++;
                }
            }

            return result;
        }

        private static async Task<int[]> GenerationTasks()
        {
            var results = new List<int[]>();
            var partOfWork = _numbersToGenerate / _threads;
            var tasks = new List<Task>();

            for (int i = 0; i < _threads; i++)
            {
                if (i == 0)
                {
                    tasks.Add(new Task(() =>
                    {
                        results.Add(GenerateRandomNumbers(partOfWork + (_numbersToGenerate % _threads)));
                    }));
                }
                else
                {
                    tasks.Add(new Task(() =>
                    {
                        results.Add(GenerateRandomNumbers(partOfWork));
                    }));
                }
            }

            foreach (var task in tasks)
            {
                task.Start();
            }

            await Task.WhenAll(tasks);

            var result = new int[_uniqueNumbers];

            foreach (var resultset in results)
            {
                for (int i = 0; i < resultset.Length; i++)
                {
                    result[i] += resultset[i];
                }
            }
            return result;
        }
    }
}
