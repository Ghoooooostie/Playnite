using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace GameActivityReview
{
    // 侧边栏页面的数据模型和命令。
    public class GameActivityReviewViewModel : ObservableObject
    {
        private readonly IPlayniteAPI api;
        private readonly GameActivityStore store;
        private ActivityReviewPeriod selectedPeriod;
        private ActivityReviewSummary summary;
        private readonly List<ActivityReviewPeriodOption> periodOptions;

        public ActivityReviewSummary Summary
        {
            get { return summary; }
            private set { SetValue(ref summary, value, "Summary", "HasRecords"); }
        }

        public bool HasRecords
        {
            get { return Summary != null && Summary.TopGames != null && Summary.TopGames.Any(); }
        }

        public ObservableCollection<GameActivityRankItem> TopGames { get; private set; }

        public IEnumerable<ActivityReviewPeriodOption> PeriodOptions
        {
            get { return periodOptions; }
        }

        public ActivityReviewPeriod SelectedPeriod
        {
            get { return selectedPeriod; }
            set
            {
                selectedPeriod = value;
                OnPropertyChanged();
                Refresh();
            }
        }

        public ICommand RefreshCommand { get; private set; }
        public ICommand ExportCommand { get; private set; }
        public ICommand OpenGameCommand { get; private set; }

        public GameActivityReviewViewModel(IPlayniteAPI api, GameActivityStore store)
        {
            if (api == null)
            {
                throw new ArgumentNullException("api");
            }

            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            this.api = api;
            this.store = store;
            TopGames = new ObservableCollection<GameActivityRankItem>();
            periodOptions = new List<ActivityReviewPeriodOption>
            {
                new ActivityReviewPeriodOption(ActivityReviewPeriod.All, "全部"),
                new ActivityReviewPeriodOption(ActivityReviewPeriod.Day, "今天"),
                new ActivityReviewPeriodOption(ActivityReviewPeriod.Week, "本周"),
                new ActivityReviewPeriodOption(ActivityReviewPeriod.Month, "本月"),
                new ActivityReviewPeriodOption(ActivityReviewPeriod.Year, "今年")
            };
            RefreshCommand = new RelayCommand(Refresh);
            ExportCommand = new RelayCommand(Export);
            OpenGameCommand = new RelayCommand<GameActivityRankItem>(OpenGame);
            selectedPeriod = ActivityReviewPeriod.All;
            Refresh();
        }

        // 重新读取记录并刷新摘要。
        public void Refresh()
        {
            try
            {
                var records = store.LoadSessions();
                Summary = ActivityReviewCalculator.BuildSummary(records, SelectedPeriod, DateTime.Now);
                TopGames.Clear();
                foreach (var item in Summary.TopGames.Take(20))
                {
                    FillGameIcon(item);
                    TopGames.Add(item);
                }
            }
            catch (Exception e)
            {
                api.Dialogs.ShowErrorMessage(e.Message, "游戏时光回顾");
                throw;
            }
        }

        // 填充常用游戏列表里的图标路径。
        private void FillGameIcon(GameActivityRankItem item)
        {
            if (item == null || item.GameId == Guid.Empty)
            {
                return;
            }

            var game = api.Database.Games.Get(item.GameId);
            if (game == null || string.IsNullOrWhiteSpace(game.Icon))
            {
                return;
            }

            item.IconPath = File.Exists(game.Icon) || game.Icon.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                ? game.Icon
                : api.Database.GetFullFilePath(game.Icon);
        }

        // 保存当前摘要为文本海报。
        private void Export()
        {
            var path = api.Dialogs.SaveFile("Text File|*.txt", true);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            System.IO.File.WriteAllText(path, GameActivityShareText.Build(Summary), Encoding.UTF8);
            api.Dialogs.ShowMessage("分享海报已保存。", "游戏时光回顾");
        }

        // 在 Playnite 中选中榜单游戏。
        private void OpenGame(GameActivityRankItem item)
        {
            if (item == null || item.GameId == Guid.Empty)
            {
                return;
            }

            api.MainView.SwitchToLibraryView();
            api.MainView.SelectGame(item.GameId);
        }
    }
}
