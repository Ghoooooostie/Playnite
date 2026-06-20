// 文件用途：负责定时触发扫描，并把结果写入待确认缓存。
using System;
using System.Linq;
using System.Timers;

namespace SwitchSmartImport
{
    // 定时扫描服务。
    public class SwitchScheduledScanService : IDisposable
    {
        private readonly ISwitchImportScanner scanner;
        private readonly ISwitchPendingImportStore store;
        private readonly Timer timer;
        private readonly object runLock = new object();

        public event Action<SwitchCandidateMergeResult> ScanCompleted;

        public SwitchScheduledScanService(ISwitchImportScanner scanner, ISwitchPendingImportStore store, double intervalMinutes = 60)
        {
            this.scanner = scanner ?? throw new ArgumentNullException("scanner");
            this.store = store ?? throw new ArgumentNullException("store");
            timer = new Timer(Math.Max(1, intervalMinutes) * 60 * 1000);
            timer.AutoReset = true;
            timer.Elapsed += OnTimerElapsed;
        }

        // 启动定时扫描。
        public void Start()
        {
            timer.Start();
        }

        // 停止定时扫描。
        public void Stop()
        {
            timer.Stop();
        }

        // 手动执行一次扫描。
        public void RunOnce()
        {
            lock (runLock)
            {
                var result = scanner.Scan();
                store.Save(result.Candidates.ToList(), DateTime.Now, result.SkippedItems.ToList());
                ScanCompleted?.Invoke(result);
            }
        }

        // 更新定时扫描间隔。
        public void UpdateInterval(double intervalMinutes)
        {
            timer.Interval = Math.Max(1, intervalMinutes) * 60 * 1000;
        }

        // 当前扫描间隔，供测试和诊断使用。
        public double IntervalMilliseconds => timer.Interval;

        public void Dispose()
        {
            timer.Dispose();
        }

        private void OnTimerElapsed(object sender, ElapsedEventArgs e)
        {
            RunOnce();
        }
    }
}
