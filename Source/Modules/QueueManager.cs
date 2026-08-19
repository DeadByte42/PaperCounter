using DB42.Lib.WPF;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace PaperCounter.Utility
{
    public class QueueManager<TTask, TResult> : VMBase
    {
        private readonly Func<TTask, Task<TResult>> _processFuncAsync;
        private readonly Action<TResult> _uiUpdateAction;
        private readonly Dispatcher _uiDispatcher;

        private readonly ConcurrentQueue<TTask> _pendingTasks = new();
        private readonly object _lockObj = new();
        private Task _processingTask;
        private bool _isCancellationRequested = false;


        // UI-свойства
        private int _totalFiles = 0;
        public int TotalFiles
        {
            get => _totalFiles;
            private set { _totalFiles = value; OnPropertyChanged(); }
        }

        private int _processedFiles = 0;
        public int ProcessedFiles
        {
            get => _processedFiles;
            private set { _processedFiles = value; OnPropertyChanged(); }
        }

        private bool _isBusy = false;
        public bool IsBusy
        {
            get => _isBusy;
            private set { _isBusy = value; OnPropertyChanged(); }
        }

        public QueueManager(
            Func<TTask, Task<TResult>> processFuncAsync,
            Action<TResult> uiUpdateAction)
        {
            _processFuncAsync = processFuncAsync ?? throw new ArgumentNullException(nameof(processFuncAsync));
            _uiUpdateAction = uiUpdateAction ?? throw new ArgumentNullException(nameof(uiUpdateAction));
            _uiDispatcher = Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Добавить файл(ы) в очередь (можно во время обработки)
        /// </summary>
        public void AddItems(IEnumerable<TTask> items)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            Debug.WriteLine("add");
            lock (_lockObj)
            {
                int added = 0;
                foreach (var item in items)
                {
                    _pendingTasks.Enqueue(item);
                    added++;
                }

                if (added > 0)
                {
                    TotalFiles += added;

                    if (!_isBusy)
                        StartProcessing();
                }
            }
        }

        /// <summary>
        /// Отмена обработки (между файлами)
        /// </summary>
        public void RequestCancel()=>
            _isCancellationRequested = true;

        /// <summary>
        /// Полная очистка очереди (отменяет текущую обработку и удаляет всё)
        /// </summary>
        private void Cancel()
        {
            lock (_lockObj)
            {
                _uiDispatcher.Invoke(() =>
                {
                    ProcessedFiles = 0;
                    TotalFiles = 0;
                    IsBusy = false;
                });

                _pendingTasks.Clear();
            }
        }
        private void EndProcessing()
        {
            lock (_lockObj)
            {
                _uiDispatcher.Invoke(() =>
                {
                    ProcessedFiles = 0;
                    TotalFiles = 0;
                    IsBusy = false;
                });
            }
        }

        private void StartProcessing()
        {
            IsBusy = true;
            _isCancellationRequested = false;

            _processingTask = Task.Run(async () => await ProcessQueueAsync());
        }

        private async Task ProcessQueueAsync()
        {
            while (true)
            {
                // Проверяем отмену перед каждым файлом
                if (_isCancellationRequested)
                {
                    Cancel();
                    return;
                }


                TTask task;
                lock (_lockObj)
                {
                    if (!_pendingTasks.TryDequeue(out task))
                    {
                        EndProcessing();
                        return;
                    }
                }

                try
                {
                    // Выполняем обработку в фоне
                    TResult result = await _processFuncAsync(task);

                    // Проверяем отмену перед каждым файлом
                    if (_isCancellationRequested)
                    {
                        Cancel();
                        return;
                    }

                    // Обновляем UI (в потоке UI)
                    _uiDispatcher.Invoke(() =>
                    {
                        _uiUpdateAction(result);
                        ProcessedFiles++;
                    });
                }
                catch (Exception ex)
                {
                    // Обработка ошибки - можно залогировать или показать
                    _uiDispatcher.Invoke(() =>
                    {
                        // Показать ошибку, но продолжить со следующим файлом
                        Debug.WriteLine($"Ошибка: {ex.Message}");
                        ProcessedFiles++;
                    });
                }
            }
        }
    }
}
