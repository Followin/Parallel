using System;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Timer = System.Timers.Timer;

namespace Parallel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Stopwatch _sw;
        private ConcurrentDictionary<char, int> _dict;
        private bool _isTerminated;
        private bool _isPaused;
        private Timer _timer;
        private ManualResetEventSlim _reset;
        private CancellationTokenSource _tokenSource;


        public MainWindow()
        {
            InitializeComponent();
            _sw = new Stopwatch();
            _dict = new ConcurrentDictionary<char, int>();
            _timer = new Timer();
            _tokenSource = new CancellationTokenSource();

            Read();

            _timer.Elapsed += UpdateSource;
            _timer.Interval = 100;
            _timer.Start();
        }

        private async void Read()
        {
            _sw.Start();




            using (var reader = new StreamReader(@"C:\Users\Pavlo_Yeremenko\Desktop\Толстой Лев. Война и мир. All.txt"))
            {

                var str = await reader.ReadToEndAsync();
                ParallelLoopResult result = new ParallelLoopResult();

                try
                {
                    await Task.Factory.StartNew(() => result = System.Threading.Tasks.Parallel.For(
                        0,
                        str.Length,
                        new ParallelOptions
                        {
                            CancellationToken = _tokenSource.Token
                        },
                        (i, state) =>
                        {

                            if (_isPaused)
                            {
                                TextBox.Dispatcher.Invoke(() => TextBox.Text = "paused");
                                state.Break();
                            }
                            else if (_isTerminated)
                            {
                                state.Stop();
                            }
                            else
                            {
                                TextBox.Dispatcher.Invoke(() => TextBox.Text = "running");
                                UpdateSymbol(str[i]);
                            }

                        }), _tokenSource.Token);
                    _sw.Stop();
                    TextBox.Text = "Completed in " + _sw.ElapsedMilliseconds + "ms";
                }
                catch (OperationCanceledException exception)
                {
                    TextBox.Text = "Operation stopped";
                }

            }
            



        }

        private void UpdateSource(object state, ElapsedEventArgs elapsedEventArgs)
        {
            Dispatcher.Invoke(() =>
            {
                Grid1.ItemsSource = null;

                var orderedCollection =
                    _dict.OrderByDescending(c => c.Value)
                    .Select(kv => new Tuple<char, long, string>(kv.Key, kv.Value, Math.Round(kv.Value / (double)_dict.Sum(c => c.Value) * 100, 5) + "%"))
                    .ToList();

                Grid1.ItemsSource = orderedCollection;
            });
        }

        private void UpdateSymbol(char ch)
        {
            //Thread.Sleep(10);
            _dict.AddOrUpdate(ch, c => 1, (c, i) => ++i);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            _isPaused = !_isPaused;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _tokenSource.Cancel();
        }
    }
}
