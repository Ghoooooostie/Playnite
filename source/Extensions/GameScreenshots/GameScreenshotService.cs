// 文件用途：组合屏幕截取和文件存储，完成某个游戏的截图。
using Playnite.SDK.Models;
using System;

namespace GameScreenshots
{
    // 负责把当前屏幕截图保存到指定游戏目录。
    public class GameScreenshotService : IGameScreenshotService
    {
        private readonly IScreenshotCaptureService captureService;
        private readonly IScreenshotStore store;
        public event EventHandler<ScreenshotCapturedEventArgs> ScreenshotCaptured;

        public GameScreenshotService(IScreenshotCaptureService captureService, IScreenshotStore store)
        {
            if (captureService == null)
            {
                throw new ArgumentNullException("captureService");
            }

            if (store == null)
            {
                throw new ArgumentNullException("store");
            }

            this.captureService = captureService;
            this.store = store;
        }

        // 截取当前屏幕并按游戏保存。
        public ScreenshotItem CaptureGame(Game game, DateTime capturedAt)
        {
            if (game == null)
            {
                throw new ArgumentNullException("game");
            }

            var bytes = captureService.CapturePng();
            var item = store.SaveScreenshot(game.Id, game.Name, bytes, capturedAt);
            OnScreenshotCaptured(item);
            return item;
        }

        // 通知已打开的截图页面刷新。
        private void OnScreenshotCaptured(ScreenshotItem item)
        {
            if (ScreenshotCaptured != null)
            {
                ScreenshotCaptured(this, new ScreenshotCapturedEventArgs(item));
            }
        }
    }
}
