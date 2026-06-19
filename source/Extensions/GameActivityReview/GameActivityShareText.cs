using System.Linq;
using System.Text;

namespace GameActivityReview
{
    // 生成可分享的纯文字回顾内容。
    public static class GameActivityShareText
    {
        // 按当前摘要生成文本海报。
        public static string Build(ActivityReviewSummary summary)
        {
            var builder = new StringBuilder();
            builder.AppendLine("我的游戏时光回顾");
            builder.AppendLine(summary.PeriodTitle + " · " + summary.DateRangeText);
            builder.AppendLine(summary.ReviewText);
            builder.AppendLine();
            builder.AppendLine("游戏榜单");

            var index = 1;
            foreach (var item in summary.TopGames.Take(10))
            {
                builder.AppendLine(string.Format("{0}. {1} · {2} · {3} 次", index, item.GameName, item.TimeText, item.SessionCount));
                index++;
            }

            if (index == 1)
            {
                builder.AppendLine("暂无记录");
            }

            return builder.ToString();
        }
    }
}
