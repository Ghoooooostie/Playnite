using Playnite.SDK.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GameActivityReview
{
    // 负责读写插件自己的会话记录文件。
    public class GameActivityStore
    {
        private readonly string recordsPath;

        public GameActivityStore(string dataDirectory)
        {
            recordsPath = Path.Combine(dataDirectory, "sessions.json");
        }

        // 读取所有已保存的会话记录。
        public List<GameSessionRecord> LoadSessions()
        {
            if (!File.Exists(recordsPath))
            {
                return new List<GameSessionRecord>();
            }

            List<GameSessionRecord> records;
            Exception error;
            if (!Serialization.TryFromJsonFile(recordsPath, out records, out error))
            {
                throw new InvalidDataException("游戏时光记录文件无法读取。", error);
            }

            return records ?? new List<GameSessionRecord>();
        }

        // 追加一条会话记录并写回文件。
        public void AddSession(GameSessionRecord record)
        {
            if (record == null)
            {
                throw new ArgumentNullException("record");
            }

            var records = LoadSessions();
            records.Add(record);
            SaveSessions(records);
        }

        // 保存完整会话列表。
        public void SaveSessions(IEnumerable<GameSessionRecord> records)
        {
            var directory = Path.GetDirectoryName(recordsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(recordsPath, Serialization.ToJson(records.ToList(), true), Encoding.UTF8);
        }
    }
}
