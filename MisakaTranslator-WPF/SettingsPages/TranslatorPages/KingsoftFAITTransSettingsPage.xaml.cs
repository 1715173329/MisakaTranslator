﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TranslatorLibrary;

namespace MisakaTranslator_WPF.SettingsPages.TranslatorPages
{
    /// <summary>
    /// KingsoftFAITTransSettingsPage.xaml 的交互逻辑
    /// </summary>
    public partial class KingsoftFAITTransSettingsPage : Page
    {
        public KingsoftFAITTransSettingsPage()
        {
            InitializeComponent();
            PathBox.Text = Common.appSettings.KingsoftFastAITPath;
        }

        private void ChoosePathBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "请选择金山快译2009本体所在文件夹";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    HandyControl.Controls.Growl.Error("文件夹路径不能为空");
                }
                else
                {
                    PathBox.Text = dialog.SelectedPath;
                    Common.appSettings.KingsoftFastAITPath = PathBox.Text;
                }
            }
        }

        private void TransTestBtn_Click(object sender, RoutedEventArgs e)
        {
            ITranslator Trans = new KingsoftFastAITTranslator();
            Trans.TranslatorInit(Common.appSettings.KingsoftFastAITPath, "");
            string res = Trans.Translate(TestSrcText.Text, "zh", TestSrcLang.Text);
            if (res != null)
            {
                HandyControl.Controls.MessageBox.Show(res, "翻译结果");
            }
            else
            {
                HandyControl.Controls.Growl.Error("金山快译翻译API工作异常\n" + Trans.GetLastError());
            }
        }
    }
}
