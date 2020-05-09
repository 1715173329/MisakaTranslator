﻿using Config.Net;
using FontAwesome.WPF.Converters;
using HandyControl.Controls;
using KeyboardMouseHookLibrary;
using OCRLibrary;
using SQLHelperLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Navigation;
using TextHookLibrary;

namespace MisakaTranslator_WPF
{
    public partial class MainWindow
    {
        List<GameInfo> gameInfolst;
        int gid;//当前选中的顺序，并非游戏ID
        IntPtr hwnd;

        public MainWindow()
        {
            IAppSettings settings = new ConfigurationBuilder<IAppSettings>().UseJsonFile("settings/settings.json").Build();
            InitializeLanguage();
            InitializeComponent();
            Initialize(settings);

            //注册全局OCR热键
            this.SourceInitialized += new EventHandler(MainWindow_SourceInitialized);
        }

        private static void InitializeLanguage()
        {
            var appResource = Application.Current.Resources.MergedDictionaries;
            Common.appSettings = new ConfigurationBuilder<IAppSettings>().UseIniFile(Environment.CurrentDirectory + "\\settings\\settings.ini").Build();
            foreach (ResourceDictionary item in appResource)
            {
                if (item.Source.ToString().Contains("lang") && item.Source.ToString() != @"lang/" + Common.appSettings.AppLanguage + ".xaml")
                {
                    appResource.Remove(item);
                    break;
                }
            }
        }

        //按下快捷键时被调用的方法
        public void CallBack()
        {
            Common.GlobalOCR();
        }

        private void Initialize(IAppSettings settings)
        {
            this.Resources["Foreground"] = (SolidColorBrush)(new BrushConverter().ConvertFrom(settings.ForegroundHex));
            gameInfolst = GameLibraryHelper.GetAllGameLibrary();
            Common.repairSettings = new ConfigurationBuilder<IRepeatRepairSettings>().UseIniFile(Environment.CurrentDirectory + "\\settings\\RepairSettings.ini").Build();
            GameLibraryPanel_Init();
            //先初始化这两个语言，用于全局OCR识别
            Common.UsingDstLang = "zh";
            Common.UsingSrcLang = "jp";
        }
        

        /// <summary>
        /// 游戏库瀑布流初始化
        /// </summary>
        private void GameLibraryPanel_Init() {
            
            List<System.Windows.Media.SolidColorBrush> bushLst = new List<System.Windows.Media.SolidColorBrush> {
                System.Windows.Media.Brushes.CornflowerBlue,
                System.Windows.Media.Brushes.IndianRed,
                System.Windows.Media.Brushes.Orange,
                System.Windows.Media.Brushes.ForestGreen
            };
            if (gameInfolst != null)
            {
                for (int i = 0; i < gameInfolst.Count; i++)
                {
                    TextBlock tb = new TextBlock()
                    {
                        Text = gameInfolst[i].GameName,
                        Foreground = System.Windows.Media.Brushes.White,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Margin = new Thickness(3)
                    };
                    System.Windows.Controls.Image ico = new System.Windows.Controls.Image()
                    {
                        Source = ImageProcFunc.ImageToBitmapImage(ImageProcFunc.GetAppIcon(gameInfolst[i].FilePath)),
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Height = 64,
                        Width = 64
                    };
                    Grid gd = new Grid();
                    gd.Children.Add(ico);
                    gd.Children.Add(tb);
                    Border back = new Border()
                    {
                        Name = "game" + i,
                        Width = 150,
                        Child = gd,
                        Margin = new Thickness(5),
                        Background = bushLst[i % 4],
                    };
                    back.MouseEnter += Border_MouseEnter;
                    back.MouseLeave += Border_MouseLeave;
                    back.MouseLeftButtonDown += Back_MouseLeftButtonDown;
                    GameLibraryPanel.RegisterName("game" + i,back);
                    GameLibraryPanel.Children.Add(back);
                }
            }
            TextBlock textBlock = new TextBlock()
            {
                Text = App.Current.Resources["MainWindow_ScrollViewer_AddNewGame"].ToString(),
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(3)
            };
            Grid grid = new Grid();
            grid.Children.Add(textBlock);
            Border border = new Border()
            {
                Name = "AddNewName",
                Width = 150,
                Child = grid,
                Margin = new Thickness(5),
                Background = (SolidColorBrush)this.Resources["Foreground"]
            };
            border.MouseEnter += Border_MouseEnter;
            border.MouseLeave += Border_MouseLeave;
            border.MouseLeftButtonDown += Border_MouseLeftButtonDown;
            GameLibraryPanel.RegisterName("AddNewGame", border);
            GameLibraryPanel.Children.Add(border);
        }

        private void Border_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddNewGameDrawer.IsOpen = true;
        }

        void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            hwnd = new WindowInteropHelper(this).Handle;
            HwndSource.FromHwnd(hwnd).AddHook(new HwndSourceHook(WndProc));
            //注册热键
            Common.GlobalOCRHotKey = new GlobalHotKey();
            if (Common.GlobalOCRHotKey.RegistHotKeyByStr(Common.appSettings.GlobalOCRHotkey, hwnd, CallBack) == false)
            {
                Growl.ErrorGlobal("全局OCR热键注册失败！");
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            Common.GlobalOCRHotKey.ProcessHotKey(System.Windows.Forms.Message.Create(hwnd, msg, wParam, lParam));
            return IntPtr.Zero;
        }

        private static SettingsWindow _settingsWindow;

        private void SettingsBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_settingsWindow == null || _settingsWindow.IsVisible == false)
            {
                _settingsWindow = new SettingsWindow();
                _settingsWindow.Show();
            }
            else
            {
                _settingsWindow.WindowState = WindowState.Normal;
                _settingsWindow.Activate();
            }
        }

        

        private void HookGuideBtn_Click(object sender, RoutedEventArgs e)
        {
            GameGuideWindow ggw = new GameGuideWindow(1);
            ggw.Show();
        }

        private void OCRGuideBtn_Click(object sender, RoutedEventArgs e)
        {
            GameGuideWindow ggw = new GameGuideWindow(2);
            ggw.Show();
        }

        private void Border_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Border b = (Border)sender;
            b.BorderThickness = new Thickness(2);
        }

        private void Border_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Border b = (Border)sender;
            b.BorderThickness = new Thickness(0);
        }

        private void Back_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Border b = (Border)sender;
            string str = b.Name;
            string temp = str.Remove(0,4);
            gid = int.Parse(temp);

            GameNameTag.Text = "游戏名：" + gameInfolst[gid].GameName;
            if (gameInfolst[gid].TransMode == 1) {
                TransModeTag.Text = "翻译模式：Hook";
            }
            else 
            {
                TransModeTag.Text = "翻译模式：OCR";
            }

            GameInfoDrawer.IsOpen = true;
        }


        private void StartTranslateBygid(int gid) {
            Process[] ps = Process.GetProcesses();
            List<Process> pidlst = new List<Process>();

            for (int i = 0; i < ps.Length; i++)
            {
                string filepath = "";
                try
                {
                    filepath = ps[i].MainModule.FileName;
                }
                catch (Exception)
                {
                    continue;
                    //这个地方直接跳过，是因为32位程序确实会读到64位的系统进程，而系统进程是不能被访问的
                    //throw ex;
                }


                if (gameInfolst[gid].FilePath == filepath) {
                    pidlst.Add(ps[i]);
                }
            }

            if (pidlst.Count == 0)
            {
                HandyControl.Controls.MessageBox.Show("未找到游戏进程！", "提示");
                return;
            }
            else {
                int pid = pidlst[0].Id;
                pidlst.Clear();
                pidlst = ProcessHelper.FindSameNameProcess(pid);
            }

            Common.transMode = 1;
            Common.UsingDstLang = gameInfolst[gid].Dst_Lang;
            Common.UsingSrcLang = gameInfolst[gid].Src_Lang;
            Common.UsingRepairFunc = gameInfolst[gid].Repair_func;

            switch (Common.UsingRepairFunc)
            {
                case "RepairFun_RemoveSingleWordRepeat":
                    Common.repairSettings.SingleWordRepeatTimes = gameInfolst[gid].Repair_param_a;
                    break;
                case "RepairFun_RemoveSentenceRepeat":
                    Common.repairSettings.SentenceRepeatFindCharNum = gameInfolst[gid].Repair_param_a;
                    break;
                case "RepairFun_RegexReplace":
                    Common.repairSettings.Regex = gameInfolst[gid].Repair_param_a;
                    Common.repairSettings.Regex_Replace = gameInfolst[gid].Repair_param_b;
                    break;
                default:
                    break;
            }

            Common.RepairFuncInit();

            if (pidlst.Count == 1)
            {
                Common.textHooker = new TextHookHandle(pidlst[0].Id);
            }
            else
            {
                Common.textHooker = new TextHookHandle(pidlst);
            }


            Common.textHooker.Init(!gameInfolst[gid].Isx64);
            Common.textHooker.HookCodeList.Add(gameInfolst[gid].Hookcode);

            if (gameInfolst[gid].IsMultiHook == true) {
                GameGuideWindow ggw = new GameGuideWindow(3);
                ggw.Show();
            }
            else
            {
                //无重复码。直接进游戏
                Common.textHooker.MisakaCodeList = null;
                Common.textHooker.DetachUnrelatedHookWhenDataRecv = Convert.ToBoolean(Common.appSettings.AutoDetach);
                Common.textHooker.StartHook(Convert.ToBoolean(Common.appSettings.AutoHook));

                TranslateWindow tw = new TranslateWindow();
                tw.Show();
            }
        }

        private void CloseDrawerBtn_Click(object sender, RoutedEventArgs e)
        {
            GameInfoDrawer.IsOpen = false;
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            Process res = Process.Start(gameInfolst[gid].FilePath);
            res.WaitForInputIdle(5000);
            GameInfoDrawer.IsOpen = false;
            Thread.Sleep(2000);
            StartTranslateBygid(gid);
        }

        /// <summary>
        /// 删除游戏按钮事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DeleteGameBtn_Click(object sender, RoutedEventArgs e)
        {
            if (HandyControl.Controls.MessageBox.Show("您是否确认要将此游戏从您的游戏库中移除？\r\n这个操作不会删除您的游戏文件，但下次进行这个游戏时需要重新设置相关参数。", "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) {
                GameLibraryHelper.DeleteGameByID(gameInfolst[gid].GameID);
                Border b = GameLibraryPanel.FindName("game" + gid) as Border;
                GameLibraryPanel.Children.Remove(b);
                GameInfoDrawer.IsOpen = false;
            }

        }

        private void UpdateNameBtn_Click(object sender, RoutedEventArgs e)
        {
            Dialog.Show(new GameNameDialog(gameInfolst,gid));
        }

        private void LEStartBtn_Click(object sender, RoutedEventArgs e)
        {
            string filepath = gameInfolst[gid].FilePath;
            ProcessStartInfo p = new ProcessStartInfo();
            string lePath = Common.appSettings.LEPath;
            p.FileName = lePath + "\\LEProc.exe";
            p.Arguments = @"-run " + filepath;
            p.UseShellExecute = false;
            p.WorkingDirectory = lePath;
            Process res = Process.Start(p);
            res.WaitForInputIdle(5000);
            GameInfoDrawer.IsOpen = false;
            Thread.Sleep(2000);
            StartTranslateBygid(gid);
        }

        private void BlurWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Common.GlobalOCRHotKey.UnRegistGlobalHotKey(hwnd,CallBack);
        }

        /// <summary>
        /// 切换语言通用事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Language_MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem menuItem)
            {
                switch (menuItem.Tag)
                {
                    case "zh-cn":
                        Common.appSettings.AppLanguage = "zh-CN";
                        HandyControl.Controls.MessageBox.Show("语言配置已修改！重启软件后生效！", "提示");
                        break;
                    case "en-us":
                        HandyControl.Controls.MessageBox.Show("Profile in English is being produced", "Hint");
                        //Common.appSettings.AppLanguage = "en-US";
                        //HandyControl.Controls.MessageBox.Show("Language configuration has been modified! It will take effect after restarting MisakaTranslator!", "Hint");
                        break;
                }
            }
        }
    }
}
