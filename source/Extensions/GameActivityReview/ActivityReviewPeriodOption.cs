namespace GameActivityReview
{
    // 下拉框中显示的中文时间范围选项。
    public class ActivityReviewPeriodOption
    {
        public ActivityReviewPeriod Period { get; private set; }
        public string Label { get; private set; }

        public ActivityReviewPeriodOption(ActivityReviewPeriod period, string label)
        {
            Period = period;
            Label = label;
        }
    }
}
