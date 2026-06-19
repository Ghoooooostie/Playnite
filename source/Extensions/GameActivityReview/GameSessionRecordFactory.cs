using System;

namespace GameActivityReview
{
    // 统一创建会话记录，确保时间边界和 Playnite 计时一致。
    public static class GameSessionRecordFactory
    {
        // 根据退出时间和 Playnite 返回的时长生成记录。
        public static GameSessionRecord Create(Guid gameId, string gameName, DateTime trackedStart, DateTime endedAt, ulong elapsedSeconds)
        {
            if (elapsedSeconds == 0)
            {
                elapsedSeconds = (ulong)Math.Max(0, Math.Floor((endedAt - trackedStart).TotalSeconds));
            }

            return new GameSessionRecord
            {
                GameId = gameId,
                GameName = gameName,
                StartedAt = endedAt.AddSeconds(-(double)elapsedSeconds),
                EndedAt = endedAt,
                DurationSeconds = elapsedSeconds
            };
        }
    }
}
