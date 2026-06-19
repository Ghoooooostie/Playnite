using System;

namespace GameActivityReview
{
    // 保存一次游戏启动到退出的原始记录，供统计和导出使用。
    public class GameSessionRecord
    {
        public Guid GameId { get; set; }
        public string GameName { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime EndedAt { get; set; }
        public ulong DurationSeconds { get; set; }
    }
}
