using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using GameActivityReview;

namespace GameActivityReview.Tests
{
    [TestFixture]
    public class ActivityReviewCalculatorTests
    {
        [Test]
        public void BuildSummary_counts_only_seconds_overlapping_the_current_week()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨周游戏",
                    StartedAt = new DateTime(2026, 6, 14, 23, 30, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 15, 0, 30, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "本周游戏",
                    StartedAt = new DateTime(2026, 6, 16, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 16, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Week, now);

            Assert.That(summary.TotalSeconds, Is.EqualTo(5400));
            Assert.That(summary.SessionCount, Is.EqualTo(2));
            Assert.That(summary.TopGames.First().GameName, Is.EqualTo("本周游戏"));
        }

        [Test]
        public void BuildSummary_counts_current_month_and_excludes_previous_month()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨月游戏",
                    StartedAt = new DateTime(2026, 5, 31, 23, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 6, 1, 1, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 7200
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "上月游戏",
                    StartedAt = new DateTime(2026, 5, 10, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 5, 10, 21, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Month, now);

            Assert.That(summary.TotalSeconds, Is.EqualTo(3600));
            Assert.That(summary.SessionCount, Is.EqualTo(1));
            Assert.That(summary.TopGames.Single().GameName, Is.EqualTo("跨月游戏"));
        }

        [Test]
        public void BuildSummary_counts_current_year_and_formats_review_text()
        {
            var now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Local);
            var sessions = new List<GameSessionRecord>
            {
                new GameSessionRecord
                {
                    GameId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    GameName = "跨年游戏",
                    StartedAt = new DateTime(2025, 12, 31, 23, 30, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 1, 1, 0, 30, 0, DateTimeKind.Local),
                    DurationSeconds = 3600
                },
                new GameSessionRecord
                {
                    GameId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GameName = "今年游戏",
                    StartedAt = new DateTime(2026, 3, 1, 20, 0, 0, DateTimeKind.Local),
                    EndedAt = new DateTime(2026, 3, 1, 22, 0, 0, DateTimeKind.Local),
                    DurationSeconds = 7200
                }
            };

            var summary = ActivityReviewCalculator.BuildSummary(sessions, ActivityReviewPeriod.Year, now);

            Assert.That(summary.TotalSeconds, Is.EqualTo(9000));
            Assert.That(summary.ReviewText, Does.Contain("今年游戏"));
            Assert.That(summary.ReviewText, Does.Contain("2 小时 30 分钟"));
        }
        [Test]
        public void CreateRecord_uses_elapsed_seconds_as_session_boundary_source()
        {
            var gameId = Guid.Parse("33333333-3333-3333-3333-333333333333");
            var trackedStart = new DateTime(2026, 6, 17, 19, 0, 0, DateTimeKind.Local);
            var endedAt = new DateTime(2026, 6, 17, 20, 30, 0, DateTimeKind.Local);

            var record = GameSessionRecordFactory.Create(gameId, "计时游戏", trackedStart, endedAt, 3600);

            Assert.That(record.StartedAt, Is.EqualTo(new DateTime(2026, 6, 17, 19, 30, 0, DateTimeKind.Local)));
            Assert.That(record.EndedAt, Is.EqualTo(endedAt));
            Assert.That(record.DurationSeconds, Is.EqualTo(3600));
        }
    }
}

