// 文件用途：保存全屏首页回顾栏目和主面板之间的打开状态。
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Input;

namespace GameActivityReview
{
    // 全屏回顾入口和面板共享的状态。
    public class GameActivityReviewFullscreenState : ObservableObject
    {
        private bool isPanelOpen;
        private object fullscreenMainModel;
        private INotifyPropertyChanged fullscreenMainModelNotifier;
        private bool isOpeningPanel;

        public bool IsPanelOpen
        {
            get { return isPanelOpen; }
            set { SetValue(ref isPanelOpen, value); }
        }

        public ICommand OpenPanelCommand { get; private set; }
        public ICommand ClosePanelCommand { get; private set; }

        public GameActivityReviewFullscreenState()
        {
            OpenPanelCommand = new RelayCommand(OpenPanel);
            ClosePanelCommand = new RelayCommand(ClosePanel);
            AttachToFullscreenMainModel();
        }

        // 释放全屏模型监听。
        public void Dispose()
        {
            if (fullscreenMainModelNotifier != null)
            {
                fullscreenMainModelNotifier.PropertyChanged -= OnFullscreenMainModelPropertyChanged;
                fullscreenMainModelNotifier = null;
                fullscreenMainModel = null;
            }
        }

        // 打开回顾主面板。
        private void OpenPanel()
        {
            isOpeningPanel = true;
            ClearActiveFilterPreset();
            IsPanelOpen = true;
            isOpeningPanel = false;
        }

        // 关闭回顾主面板。
        private void ClosePanel()
        {
            IsPanelOpen = false;
        }

        // 连接全屏主模型，监听原生栏目切换。
        private void AttachToFullscreenMainModel()
        {
            var applicationType = Type.GetType("Playnite.FullscreenApp.FullscreenApplication, Playnite.FullscreenApp");
            var current = applicationType?.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null, null);
            fullscreenMainModel = current?.GetType().GetProperty("MainModel")?.GetValue(current, null);
            fullscreenMainModelNotifier = fullscreenMainModel as INotifyPropertyChanged;
            if (fullscreenMainModelNotifier != null)
            {
                fullscreenMainModelNotifier.PropertyChanged += OnFullscreenMainModelPropertyChanged;
            }
        }

        // 原生筛选栏目变化时关闭回顾页。
        private void OnFullscreenMainModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ActiveFilterPreset" && !isOpeningPanel)
            {
                IsPanelOpen = false;
            }
        }

        // 打开回顾页时取消原生栏目选中圆点。
        private void ClearActiveFilterPreset()
        {
            var property = fullscreenMainModel?.GetType().GetProperty("ActiveFilterPreset");
            if (property != null && property.CanWrite)
            {
                property.SetValue(fullscreenMainModel, null, null);
            }
        }
    }
}
