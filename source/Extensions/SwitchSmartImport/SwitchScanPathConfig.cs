// 文件用途：定义 Switch 智能导入的扫描目录配置。
using Playnite.SDK;
using Playnite.SDK.Data;
using System.Collections.Generic;

namespace SwitchSmartImport
{
    // 扫描目录类型提示，仅用于增强判断。
    public enum SwitchScanPathType
    {
        Auto,
        Base,
        Update,
        Dlc
    }

    // 单条扫描目录配置。
    public class SwitchScanPathConfig : ObservableObject
    {
        private string name;
        private string path;
        private bool enabled = true;
        private int priority;
        private SwitchScanPathType typeHint;

        public string Name
        {
            get => name;
            set => SetValue(ref name, value);
        }

        public string Path
        {
            get => path;
            set => SetValue(ref path, value);
        }

        public bool Enabled
        {
            get => enabled;
            set => SetValue(ref enabled, value);
        }

        public int Priority
        {
            get => priority;
            set => SetValue(ref priority, value);
        }

        public SwitchScanPathType TypeHint
        {
            get => typeHint;
            set => SetValue(ref typeHint, value);
        }
    }
}
