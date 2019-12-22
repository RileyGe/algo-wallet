﻿using Algorand;
using Algorand.Algod.Client.Api;
using AlgoWallet.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Config.Net;
using Config.Net.Core;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using MessageBox.Avalonia;
using MessageBox.Avalonia.Enums;
using MessageBox.Avalonia.DTO;
using Org.BouncyCastle.Crypto.Generators;
using System.Text;

namespace AlgoWallet.Views
{
    public class MainWindow : Window
    {
        StackPanel walletManagePanel = null;
        TabControl walletOperationTabControl = null;
        StackPanel newWalletStep1 = null;
        StackPanel newWalletStep2 = null;
        StackPanel verifyMnemonic = null;
        StackPanel showMnemonic = null;
        StackPanel createAsset = null;
        StackPanel assetsListPanel = null;
        StackPanel initApiInfo = null;
        StackPanel sideBar = null;
        TabItem algoOperation = null;
        StackPanel enterPassword = null;
        ComboBox accountList = null;
        List<string> mnemonic = new List<string>();
        Dictionary<int, TextBox> mnemonicBoxes = new Dictionary<int, TextBox>();
        //List<TextBox> mnemonicBoxes = new List<TextBox>();
        //const string apiAddress = "http://hackathon.algodev.network:9100";
        //const string apiToken = "ef920e2e7e002953f4b29a8af720efe8e4ecc75ff102b165e0472834b25832c1";
        AlgodApi algoInstance = null;
        Account algoAccount = null;
        SynchronizationContext m_SyncContext = null;
        IAppSettings settings = null;
        List<KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>> accountAssets = 
            new List<KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>>();
        ulong assetId = 0;
        private string accountPassword = "";

        public MainWindow()
        {
            InitializeComponent();            
            settings = new ConfigurationBuilder<IAppSettings>()
                .UseJsonFile("config.db")
                .Build();
            initApiInfo = this.FindControl<StackPanel>("sp_initApiInfo");            
            walletOperationTabControl = this.FindControl<TabControl>("tc_walletOperation");
            sideBar = this.FindControl<StackPanel>("sp_sideBar");
            walletManagePanel = this.FindControl<StackPanel>("sp_walletManage");
            assetsListPanel = this.FindControl<StackPanel>("sp_assetsList");
            enterPassword = this.FindControl<StackPanel>("sp_enterPassword");
            accountList = this.FindControl<ComboBox>("cb_accountList");
            m_SyncContext = SynchronizationContext.Current;
            bool connected = true;
#if DEBUG
            this.AttachDevTools();
#endif
            if (!(settings.AlogApiAddress is null || settings.AlgoApiToken is null ||
                settings.AlogApiAddress == "" || settings.AlgoApiToken == ""))
            {
                algoInstance = new AlgodApi(settings.AlogApiAddress, settings.AlgoApiToken);
                try
                {
                    var status = algoInstance.GetStatus();
                }
                catch (Exception)
                {
                    this.FindControl<TextBox>("tb_apiUrl").Text = settings.AlogApiAddress;
                    this.FindControl<TextBox>("tb_apiToken").Text = settings.AlgoApiToken;
                    connected = false;
                }
            }
            else connected = false;
            if (!connected)
            {
                initApiInfo.IsVisible = true;
                walletOperationTabControl.IsVisible = false;
                sideBar.IsVisible = false;
            }
            else if(settings.Accounts is null || settings.Accounts.Length < 1)
            {
                sideBar.IsVisible = false;
                walletOperationTabControl.IsVisible = false;
                walletManagePanel.IsVisible = true;
            }
            else
            {
                sideBar.IsVisible = false;
                walletOperationTabControl.IsVisible = false;
                enterPassword.IsVisible = true;
                accountList.Items = settings.Accounts;
            }
            //algoInstance ??= new AlgodApi(apiAddress, apiToken);
            //init an default account for test pupose            
            //algoAccount = new Account("typical permit hurdle hat song detail cattle merge oxygen crowd arctic cargo smooth fly rice vacuum lounge yard frown predict west wife latin absent cup");
            //algoAccount = new Account("portion never forward pill lunch organ biology"
            //                          + " weird catch curve isolate plug innocent skin grunt"
            //                          + " bounce clown mercy hole eagle soul chunk type absorb trim");
            //this.DataContext
            
            //new Thread(new ThreadStart(this.ThreadProcSafePost)).Start();
        }

        private void TestingConnection(object message)
        {
            if(message is string[])
            {
                var msg = message as string[];                
                m_SyncContext.Post(UpdateTestResult, "Connecting...");
                try
                {
                    var url = "";
                    if (msg[0].IndexOf("//") == -1)
                        url = "http://" + msg[0];
                    else
                        url = msg[0];
                    var api = new AlgodApi(url, msg[1], 10 * 1000);
                    var supply = api.GetSupply();
                    if (supply != null && supply.TotalMoney > 0)
                    {
                        m_SyncContext.Post(UpdateTestResult, "Test Success!");
                    }
                }
                catch (Exception)
                {
                    m_SyncContext.Post(UpdateTestResult, "Test Failed!");
                }
            }
            
        }
        private void UpdateTestResult(object message)
        {
            if(message is string)
            {
                var resultText = this.FindControl<TextBlock>("tb_testResult");
                resultText.IsVisible = true;
                resultText.Text = message.ToString();
            }
        }
        private void ThreadProcSafePost()
        {
            //...执行线程任务
            var act = algoInstance.AccountInformation(algoAccount.Address.ToString());
            
            foreach (var item in act.Assets)
            {
                var ap = algoInstance.AssetInformation((long?)item.Key);
                var pair = new KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>((ulong)item.Key, ap);
                accountAssets.Add(pair);
                //在线程中更新UI（通过UI线程同步上下文m_SyncContext）
                m_SyncContext.Post(UpdateAssetsList, pair);
            }
            var transParams = algoInstance.TransactionParams();
            long? lastRound = (long?)transParams.LastRound;
            long? firstRound = lastRound - 1000 * 24;
            ulong? balance = act.Amount;
            //var block = algoInstance.GetBlock(3692494);
            ////while (true)
            ////{
            ////    try
            ////    {
            ////        var translist = algoInstance.Transactions(algoAccount.Address.ToString(),
            ////            firstRound: firstRound, lastRound: lastRound, max: 40).Transactions;
            ////        foreach (var item in translist)
            ////        {
            ////            var block = algoInstance.GetBlock((long?)item.Round);
            ////            if (item.From == algoAccount.Address.ToString())
            ////            {
            ////                var info = algoInstance.TransactionInformation(algoAccount.Address.ToString(), item.Tx);
            ////                //item.cr
            ////                if (info.Type == "pay")
            ////                {

            ////                }
            ////            }
            ////        }

            ////    }
            ////    catch (Exception) { }
            ////}

            //algoInstance.transaction
        }
        private void UpdateAssetsList(object state)
        {
            if (state is KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>)
            {
                var pair = (KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>)state;
                Button btn = new Button()
                {
                    Content = pair.Value.Assetname,
                    Name = "btn_" + pair.Key.ToString()
                };
                btn.Click += OnAssetClick;
                assetsListPanel.Children.Add(btn);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        public void OnTestApiClicked(object sender, RoutedEventArgs e)
        {
            var urlAndtoken = new string[2];
            urlAndtoken[0] = this.FindControl<TextBox>("tb_apiUrl").Text;
            urlAndtoken[1] = this.FindControl<TextBox>("tb_apiToken").Text;
            new Thread(new ParameterizedThreadStart(this.TestingConnection)).Start(urlAndtoken);
        }
        public void OnCloseWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        public void OnPasswordEntered(object sender, RoutedEventArgs e)
        {
            var walletName = accountList.SelectedItem.ToString();
            var index = new List<string>(settings.Accounts).IndexOf(walletName);
            var passwordTextBox = this.FindControl<TextBox>("tb_enterPassword");
            var enteredPassword = passwordTextBox.Text.Trim();
            if(OpenBsdBCrypt.CheckPassword(settings.Passwords[index],
                enteredPassword.ToCharArray()))
            {
                accountPassword = enteredPassword;
                var seed = CryptoUtils.DecryptAES(accountPassword, settings.Mnemonics[index]);
                algoAccount = new Account(seed);
                sideBar.IsVisible = true;
                walletOperationTabControl.IsVisible = true;
                enterPassword.IsVisible = false;
            }
            else
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Password Not Right",
                    ContentMessage = "The wallet name and password not match, please try again."
                });
                msBoxStandardWindow.ShowDialog(this);
                passwordTextBox.Text = "";
                passwordTextBox.Focus();
            }
        }
        public void OnSaveApiClicked(object sender, RoutedEventArgs e)
        {            
            var url = this.FindControl<TextBox>("tb_apiUrl").Text;
            settings.AlgoApiToken = this.FindControl<TextBox>("tb_apiToken").Text;
            if (url.IndexOf("//") == -1)
            {
                url = "http://" + url;
            }
            settings.AlogApiAddress = url;
            algoInstance = new AlgodApi(settings.AlogApiAddress, settings.AlgoApiToken);

            initApiInfo.IsVisible = false;            

            if (settings.Accounts is null || settings.Accounts.Length < 1)
            {
                walletManagePanel.IsVisible = true;
            }
            else
            {
                walletOperationTabControl.IsVisible = true;
                sideBar.IsVisible = true;
            }
        }
        public void OnAssetClick(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            assetId = Convert.ToUInt64(btn.Name.Split('_')[1]);
            algoOperation ??= this.FindControl<TabItem>("ti_algoOperation");
            algoOperation.IsVisible = false;
        }
        public void OnAlgoClick(object sender, RoutedEventArgs e)
        {
            assetId = 0;
            algoOperation ??= this.FindControl<TabItem>("ti_algoOperation");
            algoOperation.IsVisible = true;
        }
        public void OnWalletClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            walletOperationTabControl.IsVisible = false;
            walletManagePanel.IsVisible = true;
        }
        public void OnNewWalletClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            newWalletStep1 ??= this.FindControl<StackPanel>("sp_newWallet_step1");
            algoAccount = new Account();
            mnemonic.AddRange(algoAccount.ToMnemonic().Split(' '));
            showMnemonic ??= this.FindControl<StackPanel>("sp_showMnemonic");
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var item = (showMnemonic.Children[i] as StackPanel).Children[j] as StackPanel;
                    Border bd = new Border() { Classes = new Classes("oneword") };
                    bd.Child = new TextBlock { Text = mnemonic[i * 5 + j] };
                    item.Children.Add(bd);                    
                }
            }
            newWalletStep1.IsVisible = true;
            walletManagePanel.IsVisible = false;
        }  
        public void OnNewWalletStep1ContinueClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            newWalletStep2 ??= this.FindControl<StackPanel>("sp_newWallet_step2");
            verifyMnemonic ??= this.FindControl<StackPanel>("sp_verifyMnemonic");
            Random rd = new Random();
            List<int> needVerifyPositions = new List<int>();
            while (true)
            {
                var rdint = rd.Next(1, 25);
                if (!needVerifyPositions.Contains(rdint))
                    needVerifyPositions.Add(rdint);
                if (needVerifyPositions.Count > 3)
                    break;
            }
            mnemonicBoxes.Clear();
            for(int i = 0; i < 5; i++)
            {
                for(int j = 0; j < 5; j++)
                {
                    var item = (verifyMnemonic.Children[i] as StackPanel).Children[j] as StackPanel;
                    var pos = i * 5 + j;
                    if (needVerifyPositions.Contains(pos))
                    {
                        //var btnName = "tb_mnemonic_" + pos.ToString();                        
                        var box = new TextBox() { Width = 70 };
                        //box.DataContextChanged += this.Box_DataContextChanged;
                        mnemonicBoxes.Add(pos, box);
                        item.Children.Add(box);
                    }
                    else
                    {
                        Border bd = new Border() { Classes = new Classes("oneword") };
                        bd.Child = new TextBlock { Text = mnemonic[i * 5 + j] };
                        item.Children.Add(bd);                        
                    }
                }
            }            
            newWalletStep1.IsVisible = false;
            newWalletStep2.IsVisible = true;
        }
        private void Box_DataContextChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }
        private void OnCreateWalletFinishClicked(object sender, RoutedEventArgs e)
        {
            var walletNameBox = this.FindControl<TextBox>("tb_walletName");
            var walletName = walletNameBox.Text;
            if(walletName is null || walletName.Length < 1)
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Wallet Name Error",
                    ContentMessage = "Please Enter the Wallet Name!"
                });
                msgBox.ShowDialog(this);
                walletNameBox.Focus();
                return;
            }
            foreach(var item in mnemonicBoxes)
            {
                //var box = this.FindControl<TextBox>(item);
                var boxContent = item.Value.Text.Trim();
                var index = item.Key;
                if(boxContent != mnemonic[index])
                {
                    var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "Mnemonic Error",
                        ContentMessage = "Please Enter the Right Mnemonic Phrase!"
                    });
                    msgBox.ShowDialog(this);
                    item.Value.Focus();
                    return;
                }
            }
            var walletPassword = this.FindControl<TextBox>("tb_walletPassword");
            var password = walletPassword.Text;
            if (password is null || password.Length < 1)
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Wallet Password Error",
                    ContentMessage = "Please Enter the Password!"
                });
                msgBox.ShowDialog(this);
                walletPassword.Focus();
                return;
            }
            accountPassword = password;
            if (settings.Accounts == null || settings.Accounts.Length < 1)
            {
                settings.Accounts = new string[] { 
                    this.FindControl<TextBox>("tb_walletName").Text.Trim() 
                };
            }
            else
            {
                var accountList = new List<string>{
                    this.FindControl<TextBox>("tb_walletName").Text.Trim() };
                accountList.AddRange(settings.Accounts);
                settings.Accounts = accountList.ToArray();
            }            
            if (settings.Passwords == null || settings.Passwords.Length < 1)
            {
                settings.Passwords = new string[] {
                    CryptoUtils.GenerateBcryptHash(accountPassword)
                };
            }
            else
            {
                var psdList = new List<string>
                {
                    CryptoUtils.GenerateBcryptHash(accountPassword)
                };
                psdList.AddRange(settings.Passwords);
                settings.Passwords = psdList.ToArray();
            }
            var mnemonicString = GetMnemonicString(mnemonic);
            algoAccount = new Account(mnemonicString);
            var encryptedMasterKey = CryptoUtils.EncryptAES(accountPassword, Mnemonic.ToKey(mnemonicString));
            if (settings.Mnemonics == null || settings.Mnemonics.Length < 1)
            {
                settings.Mnemonics = new string[] {
                    encryptedMasterKey
                };
            }
            else
            {
                var keyList = new List<string>
                {
                    encryptedMasterKey
                };
                keyList.AddRange(settings.Mnemonics);
                settings.Mnemonics = keyList.ToArray();
            }
            newWalletStep2.IsVisible = false;
            walletOperationTabControl.IsVisible = true;
            sideBar.IsVisible = true;
        }

        private string GetMnemonicString(List<string> mnemonic)
        {
            string retStr = "";
            mnemonic.ForEach(item => retStr += item + " ");
            return retStr.Trim();
            //throw new NotImplementedException();
        }

        public void OnCreateAssetClick(object sender, RoutedEventArgs e)
        {
            createAsset ??= this.FindControl<StackPanel>("sp_createAsset");

            walletOperationTabControl.IsVisible = false;
            createAsset.IsVisible = true;
        }
        public void OnDoCreateAssetClick(object sender, RoutedEventArgs e)
        {
            var name = this.FindControl<TextBox>("tb_assetName").Text;
            var unitName = this.FindControl<TextBox>("tb_unitName").Text;
            //if (unitName.Length > 8)
            //    ;//show message
            var total = Convert.ToUInt64(this.FindControl<TextBox>("tb_assetTotal").Text);
            var frozen = this.FindControl<CheckBox>("cb_assetFrozen").IsChecked;
            var url = this.FindControl<TextBox>("tb_assetUrl").Text;
            var managerAddress = this.FindControl<TextBox>("tb_assetManager").Text;
            var reserverAddress = this.FindControl<TextBox>("tb_assetReserver").Text;
            var clawbackerAddress = this.FindControl<TextBox>("tb_assetClawbacker").Text;
            var freezerAddress = this.FindControl<TextBox>("tb_assetFreezer").Text;
            var ap = new Algorand.Algod.Client.Model.AssetParams(creator: algoAccount.Address.ToString(), assetname: name,
                unitname: unitName, defaultfrozen: frozen, total: total,
                url: url, managerkey: managerAddress, reserveaddr: reserverAddress, clawbackaddr: clawbackerAddress,
                freezeaddr:freezerAddress);
            //ap.Creator = algoAccount.Address.ToString();
            //ap.Metadatahash = "16efaa3924a6fd9d3a4880099a4ac65d";

            var transParams = algoInstance.TransactionParams();
            var tx = Utils.GetCreateAssetTransaction(ap, transParams, "asset generate by AlgoWallet");

            // Sign the Transaction by sender
            SignedTransaction signedTx = algoAccount.SignTransaction(tx);
            // send the transaction to the network and
            // wait for the transaction to be confirmed
            ulong? assetID = 0;
            try
            {
                var id = Utils.SubmitTransaction(algoInstance, signedTx);
                Console.WriteLine("Transaction ID: " + id);
                Console.WriteLine(Utils.WaitTransactionToComplete(algoInstance, id.TxId));
                // Now that the transaction is confirmed we can get the assetID
                Algorand.Algod.Client.Model.Transaction ptx = algoInstance.PendingTransactionInformation(id.TxId);
                assetID = ptx.Txresults.Createdasset;
                //ap = algoInstance.AssetInformation((long?)item.Key);
                var pair = new KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>((ulong)assetID, ap);
                accountAssets.Add(pair);
                //在线程中更新UI（通过UI线程同步上下文m_SyncContext）
                UpdateAssetsList(pair);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                return;
            }
            //Console.WriteLine("AssetID = " + assetID);
        }
    }
}