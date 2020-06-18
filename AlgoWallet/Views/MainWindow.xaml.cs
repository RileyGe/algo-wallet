using Algorand;
using Algorand.Algod.Client;
using Algorand.Algod.Client.Api;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Config.Net;
using MessageBox.Avalonia;
using MessageBox.Avalonia.DTO;
using MessageBox.Avalonia.Enums;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Org.BouncyCastle.Security;
using System.Security.AccessControl;
using System.IO;
using System.Security.Principal;
using Org.BouncyCastle.Asn1.Tsp;

namespace AlgoWallet.Views
{
    public class MainWindow : Window
    {
        private static readonly ILogger logger = LogManager.GetLogger("Logger");
        int sleepTime = 5000;
        StackPanel walletManagePanel = null;
        TabControl walletOperationTabControl = null;
        StackPanel importWalletStep2 = null;
        StackPanel newWalletStep0 = null;
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
        //Thread updateingThread = null;
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
        List<TransInfo> transList = new List<TransInfo>();
        ulong selectedAssetId = 0;
        //private string accountPassword = "";
        private string walletName = "";
        private byte[] salt = null;
        private byte[] scryptedKey = null;
        //ulong accountLastRound; //record the last round of the algoAccount

        public MainWindow()
        {
            InitializeComponent();
            //logger.Debug("debug");
            //logger.Error("error");
            //logger.Info("Info");
            var configFileName = "config.db";
            //if (!File.Exists(configFileName))
            //{

            //    File.Create(configFileName);
            //    //var security = new FileSecurity(configFileName,
            //    //    AccessControlSections.Owner |
            //    //    AccessControlSections.Group |
            //    //    AccessControlSections.Access);

            //    //var authorizationRules = security.GetAccessRules(true, true, typeof(NTAccount));
            //    //var owner = security.GetOwner(typeof(NTAccount));
            //    //var group = security.GetGroup(typeof(NTAccount));
            //    //var others = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null)
            //    //    .Translate(typeof(NTAccount));
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(owner, FileSystemRights.Modify, AccessControlType.Allow),
            //    //    out bool modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(group, FileSystemRights.Read, AccessControlType.Deny), out modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(group, FileSystemRights.Write, AccessControlType.Deny), out modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(group, FileSystemRights.ExecuteFile, AccessControlType.Deny), out modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(others, FileSystemRights.Read, AccessControlType.Deny), out modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(others, FileSystemRights.Write, AccessControlType.Deny), out modified);
            //    //security.ModifyAccessRule(AccessControlModification.Add,
            //    //    new FileSystemAccessRule(others, FileSystemRights.ExecuteFile, AccessControlType.Deny), out modified);
            //    //foreach (AuthorizationRule rule in authorizationRules)
            //    //{
            //    //    if (rule is FileSystemAccessRule fileRule)
            //    //    {


            //    //        if (owner != null && fileRule.IdentityReference == owner)
            //    //        {
            //    //            if (fileRule.FileSystemRights.HasFlag(FileSystemRights.ExecuteFile) ||
            //    //               fileRule.FileSystemRights.HasFlag(FileSystemRights.ReadAndExecute) ||
            //    //               fileRule.FileSystemRights.HasFlag(FileSystemRights.FullControl))
            //    //            {
            //    //                fileRule.FileSystemRights.Is
            //    //                ownerRights.IsExecutable = true;
            //    //            }
            //    //        }
            //    //        else if (group != null && fileRule.IdentityReference == group)
            //    //        {
            //    //            // TO BE CONTINUED...
            //    //        }
            //    //    }
            //    //}
            //    FileInfo fInfo = new FileInfo(configFileName);
            //    FileSecurity fs = fInfo.GetAccessControl();
            //    //FileSecurity fs = new FileSecurity();
            //    fs.AddAccessRule(new FileSystemAccessRule(Environment.UserName,
            //        FileSystemRights.Read, AccessControlType.Allow));
            //    fs.AddAccessRule(new FileSystemAccessRule(Environment.UserName,
            //        FileSystemRights.Write, AccessControlType.Allow));
            //    var fi = new FileInfo("config.db");
            //    FileSystemAclExtensions.SetAccessControl(fi, fs);
            //}
            settings = new ConfigurationBuilder<IAppSettings>()
                .UseJsonFile(configFileName)
                .Build();
            initApiInfo = this.FindControl<StackPanel>("sp_initApiInfo");            
            walletOperationTabControl = this.FindControl<TabControl>("tc_walletOperation");
            walletOperationTabControl.SelectionChanged += this.WalletOperationTabControl_SelectionChanged;
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
                    logger.Info(string.Format("connect to {0} success.", settings.AlogApiAddress));
                }
                catch (Exception)
                {                    
                    connected = false;
                    logger.Info(string.Format("connect to {0} failed.", settings.AlogApiAddress));
                }
            }
            else connected = false;
            if (!connected)
            {
                ShowConfigAccessPoint();
            }
            else
            {
                CheckAccount();
            }            
            //ContentControl info = new ContentControl
            //{
            //    Content = new TransInfo() { TxID = "QIO65UJARQSVMNRKW3DMZGKCQGORSPBIBP4HIFHKCQFE3JKH3BCA" }
            //};
            //this.FindControl<StackPanel>("sp_transInfos").Children.Add(info);
        }

        private void ShowConfigAccessPoint()
        {
            this.FindControl<TextBox>("tb_apiUrl").Text = settings.AlogApiAddress;
            this.FindControl<TextBox>("tb_apiToken").Text = settings.AlgoApiToken;
            enterPassword.IsVisible = false;
            initApiInfo.IsVisible = true;
            walletOperationTabControl.IsVisible = false;
            sideBar.IsVisible = false;
        }

        public void OnChangeWalletClick(object sender, RoutedEventArgs e)
        {
            CheckAccount();
        }
        private void CheckAccount()
        {
            //sp_walletManage
            walletManagePanel.IsVisible = false;
            if (settings.Accounts is null || settings.Accounts.Length < 1)
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
                this.FindControl<TextBox>("tb_enterPassword").Focus();
            }
        }

        private void WalletOperationTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!sender.Equals(walletOperationTabControl))
            {
                return;
            }
            foreach(var item in walletOperationTabControl.Items)
            {
                var tabItem = item as TabItem;
                tabItem.Classes.Remove("selected");
            }
            var selectedItem = walletOperationTabControl.SelectedItem as TabItem;
            selectedItem.Classes.Add("selected");
            //throw new NotImplementedException();
        }

        private void TestingConnection(object message)
        {
            if(message is string[])
            {
                var msg = message as string[];                
                m_SyncContext.Post(UpdateTestResult, "Connecting...");
                var url = "";
                if (msg[0].IndexOf("//") == -1)
                    url = "http://" + msg[0];
                else
                    url = msg[0];
                try
                {                    
                    var api = new AlgodApi(url, msg[1], 10 * 1000);
                    var supply = api.GetSupply();
                    if (supply != null && supply.TotalMoney > 0)
                    {
                        m_SyncContext.Post(UpdateTestResult, "Test Success!");
                        logger.Info(string.Format("Test connection {0} success!", url));
                    }
                }
                catch (Exception ex)
                {
                    m_SyncContext.Post(UpdateTestResult, "Test Failed! Please check Logs folder for more details.");
                    logger.Error(string.Format("Test connection {0} failed. Error message: {1}",
                        url, ex.Message));
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
        private void UpdateAssetsAndTransactions()
        {
            //accountAssets.Clear();
            //transList.Clear();
            //...执行线程任务
            bool isListening = false;
            var accAdr = algoAccount.Address.ToString();
            long? roundUtill = 0;//这个数字之前的round都已经获取

            while (accAdr == algoAccount.Address.ToString())
            {
                try {
                    var act = algoInstance.AccountInformation(algoAccount.Address.ToString());
                    if (accAdr == algoAccount.Address.ToString())                    
                        m_SyncContext.Post(UpdateAlgoButton, act);
                                       
                    foreach (var item in act.Assets)
                    {
                        while (true)
                        {
                            try
                            {
                                var ap = algoInstance.AssetInformation((long?)item.Key);
                                var pair = new KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>((ulong)item.Key, ap);
                                if (accountAssets.Count(p => p.Key == pair.Key) == 0)
                                {
                                    accountAssets.Add(pair);
                                    //在线程中更新UI（通过UI线程同步上下文m_SyncContext）
                                    object[] state = new object[] { pair, act.GetHolding(pair.Key).Amount };
                                    if (accAdr == algoAccount.Address.ToString())
                                        m_SyncContext.Post(UpdateAssetsList, state);
                                }
                                break;
                            }
                            catch (Exception) { Thread.Sleep(sleepTime); }
                        }

                    }
                    var transParams = algoInstance.TransactionParams();
                    long? lastRound = (long?)transParams.LastRound;
                    if (isListening)
                    {
                        long? firstRound = roundUtill;
                        try
                        {
                            var translist = algoInstance.Transactions(algoAccount.Address.ToString(),
                                firstRound: firstRound, lastRound: lastRound).Transactions;
                            foreach (var item in translist)
                            {
                                UpdateTransaction2UI(accAdr, item);
                            }

                        }
                        catch (Exception) { }
                        Thread.Sleep(sleepTime);
                    }
                    else
                    {
                        //inital                    

                        roundUtill = lastRound;
                        long? firstRound = lastRound - 1000 * 24;
                        ulong? balance = act.Amount;
                        //var block = algoInstance.GetBlock(3692494);

                        while (true)
                        {
                            //ulong? lr = 0;
                            try
                            {
                                var translist = algoInstance.Transactions(algoAccount.Address.ToString(),
                                    firstRound: firstRound, lastRound: lastRound).Transactions;
                                //lr = 0;
                                foreach (var item in translist)
                                {
                                    if (UpdateTransaction2UI(accAdr, item))
                                    {
                                        if (item.From == accAdr)
                                        {
                                            balance += item.Fee;
                                            balance -= item.Fromrewards;
                                        }
                                        if (item.Type == "pay")
                                        {
                                            if (item.From == accAdr)
                                            {
                                                balance += item.Payment.Amount;
                                            }
                                            else if (item.Payment.To == accAdr)
                                            {
                                                balance -= item.Payment.Amount;
                                            }
                                        }
                                    }
                                    //if (transList.Count(p => p.TxID == item.Tx) == 0)
                                    //{
                                    //    var block = algoInstance.GetBlock((long?)item.Round);
                                    //    var transInfo = new TransInfo(item, accAdr)
                                    //    {
                                    //        CreateTime = ConvertIntDatetime((ulong)block.Timestamp)
                                    //    };
                                    //    var index = transList.FindIndex(p => p.CreateTime < transInfo.CreateTime);
                                    //    if (index < 0)
                                    //    {
                                    //        transList.Add(transInfo);
                                    //        //lr = item.Round;
                                    //    }
                                    //    else
                                    //        transList.Insert(index, transInfo);
                                    //    object[] param = new object[]
                                    //    {
                                    //index, transInfo
                                    //    };
                                    //    //transList.Add(transInfo);
                                    //    m_SyncContext.Post(UpdateTransListSP, param);
                                    //    //UpdateTransListSP(info);  
                                    //}
                                }

                            }
                            catch (Exception) { }
                            //if (accountLastRound > 0) break;
                            if (balance == 0)
                            {
                                //accountLastRound = (ulong)lr;
                                isListening = true;
                                break;
                            }
                            else
                            {
                                lastRound = firstRound;
                                firstRound = lastRound - 1000 * 24;
                            }
                        }
                    }
                }
                catch(Exception)
                {
                    Thread.Sleep(sleepTime);
                }                
            }
            //algoInstance.transaction
        }

        private void UpdateAlgoButton(object state)
        {
            if(state is Algorand.Algod.Client.Model.Account act)
            {
                this.FindControl<Button>("btn_algo").Content = string.Format("Algos({0})", Utils.MicroalgosToAlgos((ulong)act.Amount));
            }            
        }

        private bool UpdateTransaction2UI(string accAdr, Algorand.Algod.Client.Model.Transaction item)
        {
            if (transList.Count(p => p.TxID == item.Tx) == 0)
            {
                var block = algoInstance.GetBlock((long?)item.Round);
                var transInfo = new TransInfo(item, accAdr)
                {
                    CreateTime = ConvertIntDatetime((ulong)block.Timestamp)
                };
                var index = transList.FindIndex(p => p.CreateTime < transInfo.CreateTime);
                if (index < 0)
                {
                    transList.Add(transInfo);
                    //lr = item.Round;
                }
                else
                    transList.Insert(index, transInfo);
                object[] param = new object[]
                {
                    index, transInfo
                };
                //transList.Add(transInfo);
                if(accAdr == algoAccount.Address.ToString())
                    m_SyncContext.Post(UpdateTransListSP, param);
                return true;
            }
            return false;
        }

        private void UpdateTransListSP(object transInfo)
        {
            if(transInfo is object[] objs)
            {
                if (objs[0] is int index && objs[1] is TransInfo trans)
                {
                    if (index < 0)
                        this.FindControl<StackPanel>("sp_transInfos").Children.Add(
                            new ContentControl
                            {
                                Content = trans
                            });
                    else
                        this.FindControl<StackPanel>("sp_transInfos").Children.Insert(index,
                            new ContentControl
                            {
                                Content = trans
                            });
                }
            }        
        }
        public static DateTime ConvertIntDatetime(double utc)
        {
            DateTime startTime = new DateTime(1970, 1, 1);
            startTime = startTime.AddSeconds(utc + TimeZoneInfo.Local.BaseUtcOffset.TotalSeconds);   
            return startTime;
        }
        private void UpdateAssetsList(object state)
        {
            //state is List<KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>>
            object[] objectList = null;

            if (state is object[])
            {
                objectList = state as object[];
            }
            else if(state is List<KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>>)
            {
                objectList = (state as List<object>).ToArray();
            }
            if (objectList[0] is KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams> pair && objectList[1] is ulong amount)
            {
                Button btn = new Button()
                {
                    Content = string.Format("{0}({1})", pair.Value.Assetname, amount),
                    Name = "btn_" + pair.Key.ToString()
                };
                btn.Click += OnAssetClick;
                assetsListPanel.Children.Add(btn);
            }
            //if (state is KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams> pair)
            //{
            //    Button btn = new Button()
            //    {
            //        Content = pair.Value.Assetname,
            //        Name = "btn_" + pair.Key.ToString()
            //    };
            //    btn.Click += OnAssetClick;
            //    assetsListPanel.Children.Add(btn);
            //}
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
            new Thread(new ParameterizedThreadStart(this.TestingConnection))
            {
                IsBackground = true
            }.Start(urlAndtoken);
            //new Thread(new ParameterizedThreadStart(this.TestingConnection)).Start();
        }
        public void OnActivateAssetClick(object sender, RoutedEventArgs e)
        {
            var assetIdToActivate = this.FindControl<TextBox>("tb_assetIdToActivate");
            ulong aid = 0;
            if(assetIdToActivate.Text is null || assetIdToActivate.Text == "")
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Asset ID Error", "Please enter the asset id.").ShowDialog(this);
            }
            else
            {
                try
                {
                    aid = Convert.ToUInt64(assetIdToActivate.Text);
                }
                catch (Exception)
                {
                    MessageBoxManager.GetMessageBoxStandardWindow("Asset ID Error", "The asset should be a interger, please enter the right asset id.").ShowDialog(this);
                }
                try
                {
                    var transParams = algoInstance.TransactionParams();
                    var tx = Utils.GetActivateAssetTransaction(algoAccount.Address, aid, transParams, "opt in transaction by algo-wallet");

                    var signedTx = algoAccount.SignTransaction(tx);
                    var id = Utils.SubmitTransaction(algoInstance, signedTx);
                }
                catch (Exception apiex)
                {
                    //e.printStackTrace();
                    MessageBoxManager.GetMessageBoxStandardWindow("Request Error", "Error hanppen:" + apiex.Message);
                    //Console.WriteLine(apiex.Message);
                    //return;
                }
            }
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
            salt = Convert.FromBase64String(settings.Salt[index]);
            var checkSalt = Convert.FromBase64String(settings.CheckSalt[index]);
            var key = CryptoUtils.GenerateHash(salt, enteredPassword);
            for(int i = 0; i < checkSalt.Length; i++)
            {
                if(checkSalt[i] != key[i])
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
                    return;
                }
            }

            var cipherText = Convert.FromBase64String(settings.CipherText[index]);
            var tag = Convert.FromBase64String(settings.Tag[index]);
            var nonce = Encoding.UTF8.GetBytes("algo--wallet");
            var masterKey = CryptoUtils.DecryptAesGcm(CryptoUtils.GetMasterKey(key), nonce, cipherText, tag);
            //var mmm = CryptoUtils.DecryptWithKey(CryptoUtils.GetMasterKey(key), nonce, cipherText, tag);
            algoAccount = new Account(masterKey);
            sideBar.IsVisible = true;
            walletOperationTabControl.IsVisible = true;
            enterPassword.IsVisible = false;
            passwordTextBox.Text = "";
            ChangeWalletRefresh();
            //if (OpenBsdBCrypt.CheckPassword(settings.Passwords[index],
            //    enteredPassword.ToCharArray()))
            //{
            //    var accountPassword = enteredPassword;
            //    var seed = CryptoUtils.DecryptAES(accountPassword, settings.Mnemonics[index]);
            //    algoAccount = new Account(seed);
                
            //    //this.FindControl<TextBox>("tb_accountAddress").Text = algoAccount.Address.ToString();
            //    //new Thread(new ThreadStart(this.UpdateAssetsAndTransactions)) { IsBackground = true }.Start();
            //}
            //else
            //{
            //    var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            //    {
            //        ButtonDefinitions = ButtonEnum.Ok,
            //        ContentTitle = "Password Not Right",
            //        ContentMessage = "The wallet name and password not match, please try again."
            //    });
            //    msBoxStandardWindow.ShowDialog(this);
            //    passwordTextBox.Text = "";
            //    passwordTextBox.Focus();
            //}
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
            CheckAccount();

            //if (settings.Accounts is null || settings.Accounts.Length < 1)
            //{
            //    walletManagePanel.IsVisible = true;
            //}
            //else
            //{
            //    walletOperationTabControl.IsVisible = true;
            //    sideBar.IsVisible = true;
            //}
        }
        public void OnAssetClick(object sender, RoutedEventArgs e)
        {            
            if (sender is Button btn)
            {
                var algoBtn = this.FindControl<Button>("btn_algo");
                algoBtn.Classes.Remove("selected");
                foreach (var item in assetsListPanel.Children)
                {
                    if (item is Button button)
                    {
                        button.Classes.Remove("selected");
                    }
                }
                btn.Classes.Add("selected");
                if (btn.Name == "btn_algo")
                {
                    selectedAssetId = 0;
                    algoOperation ??= this.FindControl<TabItem>("ti_algoOperation");
                    algoOperation.IsVisible = true;
                    this.FindControl<StackPanel>("sp_assetIdInfo").IsVisible = false;
                }
                else
                {
                    selectedAssetId = Convert.ToUInt64(btn.Name.Split('_')[1]);
                    algoOperation ??= this.FindControl<TabItem>("ti_algoOperation");
                    var assetIdInfo = this.FindControl<StackPanel>("sp_assetIdInfo");
                    assetIdInfo.IsVisible = true;
                    if(assetIdInfo.Children[1] is TextBox box)
                    {
                        box.Text = selectedAssetId.ToString();
                    }
                    algoOperation.IsVisible = false;
                }
            }
            //var btn = sender as Button;            
        }
        //public void OnAlgoClick(object sender, RoutedEventArgs e)
        //{

        //    selectedAssetId = 0;
        //    algoOperation ??= this.FindControl<TabItem>("ti_algoOperation");
        //    algoOperation.IsVisible = true;
        //}
        public void OnWalletClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            walletOperationTabControl.IsVisible = false;
            sideBar.IsVisible = false;
            enterPassword.IsVisible = false;
            walletManagePanel.IsVisible = true;
        }
        public void OnNewWalletClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            newWalletStep0 ??= this.FindControl<StackPanel>("sp_newWallet_step0");
            var title = this.FindControl<TextBlock>("tb_newWallet_title");            
            var btn = sender as Button;
            if (btn.Name == "btn_createWallet")
            {
                title.Text = "New Wallet";

            }else if(btn.Name == "btn_importWallet")
            {
                title.Text = "Import Wallet";
            }
            //algoAccount = new Account();
            //mnemonic.Clear();
            //mnemonic.AddRange(algoAccount.ToMnemonic().Split(' '));
            //showMnemonic ??= this.FindControl<StackPanel>("sp_showMnemonic");
            //for (int i = 0; i < 5; i++)
            //{
            //    for (int j = 0; j < 5; j++)
            //    {
            //        var item = (showMnemonic.Children[i] as StackPanel).Children[j] as StackPanel;
            //        if(item.Children.Count > 1)
            //        {
            //            item.Children.RemoveAt(1);
            //        }
            //        Border bd = new Border() { Classes = new Classes("oneword") };
            //        bd.Child = new TextBlock { Text = mnemonic[i * 5 + j] };
            //        item.Children.Add(bd);     
                    
            //    }
            //}
            newWalletStep0.IsVisible = true;
            walletManagePanel.IsVisible = false;
        }
        public void ImportWalletStep0()
        {
            //newWalletStep1 ??= this.FindControl<StackPanel>("sp_importWallet_step2");
            
        }
        public void OnCreateWalletStep0(object sender, RoutedEventArgs e)
        {
            var title = this.FindControl<TextBlock>("tb_newWallet_title");
            StackPanel nextStepStackPanel = null;
            bool isImport = false;
            if (title.Text == "Import Wallet")
            {
                nextStepStackPanel = this.FindControl<StackPanel>("sp_importWallet_step2");
                importWalletStep2 = nextStepStackPanel;
                isImport = true;
            }
            else
            {
                nextStepStackPanel = this.FindControl<StackPanel>("sp_newWallet_step1");
                newWalletStep1 = nextStepStackPanel;
            }

            //newWalletStep1 ??= this.FindControl<StackPanel>("sp_newWallet_step1");
            var walletNameBox = this.FindControl<TextBox>("tb_walletName");
            walletName = walletNameBox.Text;
            if (walletName is null || walletName.Length < 1)
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
            //foreach (var item in mnemonicBoxes)
            //{
            //    //var box = this.FindControl<TextBox>(item);
            //    var boxContent = item.Value.Text;
            //    if (!(boxContent is null))
            //        boxContent = boxContent.Trim();
            //    var index = item.Key;
            //    if (boxContent != mnemonic[index])
            //    {
            //        var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            //        {
            //            ButtonDefinitions = ButtonEnum.Ok,
            //            ContentTitle = "Mnemonic Error",
            //            ContentMessage = "Please Enter the Right Mnemonic Phrase!"
            //        });
            //        msgBox.ShowDialog(this);
            //        item.Value.Focus();
            //        return;
            //    }
            //}
            var walletPassword = this.FindControl<TextBox>("tb_walletPassword");
            var accountPassword = walletPassword.Text;
            if (accountPassword is null || accountPassword.Length < 1)
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
            //accountPassword = password;
            salt = CryptoUtils.GenerateRandomSalt();
            //if (settings.Accounts == null || settings.Accounts.Length < 1)
            //{
            //    settings.Accounts = new string[] {
            //        this.FindControl<TextBox>("tb_walletName").Text.Trim()
            //    };
            //}
            //else
            //{
            //    var accountList = new List<string>{
            //        this.FindControl<TextBox>("tb_walletName").Text.Trim() };
            //    accountList.AddRange(settings.Accounts);
            //    settings.Accounts = accountList.ToArray();
            //}
            //if (settings.Passwords == null || settings.Passwords.Length < 1)
            //{
            //    settings.Passwords = new string[] {
            //        CryptoUtils.GenerateBcryptHash(accountPassword)
            //    };
            //}
            //else
            //{
            //    var psdList = new List<string>
            //    {
            //        CryptoUtils.GenerateBcryptHash(accountPassword)
            //    };
            //    psdList.AddRange(settings.Passwords);
            //    settings.Passwords = psdList.ToArray();
            //}
            //var mnemonicString = GetMnemonicString(mnemonic);
            //algoAccount = new Account(mnemonicString);
            //var encryptedMasterKey = CryptoUtils.EncryptAES(accountPassword, Mnemonic.ToKey(mnemonicString));
            //if (settings.Mnemonics == null || settings.Mnemonics.Length < 1)
            //{
            //    settings.Mnemonics = new string[] {
            //        encryptedMasterKey
            //    };
            //}
            //else
            //{
            //    var keyList = new List<string>
            //    {
            //        encryptedMasterKey
            //    };
            //    keyList.AddRange(settings.Mnemonics);
            //    settings.Mnemonics = keyList.ToArray();
            //}
            //newWalletStep2.IsVisible = false;
            //walletOperationTabControl.IsVisible = true;
            //sideBar.IsVisible = true;
            //ChangeWalletRefresh();
            scryptedKey = CryptoUtils.GenerateHash(salt, accountPassword);
            //checkSalt = CryptoUtils.GetCheckSalt(key);
            //var masterKey = CryptoUtils.GetMasterKey(key);
            //var mnemonicString = GetMnemonicString(mnemonic);
            //algoAccount = new Account(masterKey);
            if (!isImport)
            {
                algoAccount = new Account(); //get a random account
                                             //newWalletStep1 ??= this.FindControl<StackPanel>("sp_newWallet_step0");
                                             //algoAccount = new Account();
                mnemonic.Clear();

                mnemonic.AddRange(algoAccount.ToMnemonic().Split(' '));
                showMnemonic ??= this.FindControl<StackPanel>("sp_showMnemonic");
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var item = (showMnemonic.Children[i] as StackPanel).Children[j] as StackPanel;
                        if (item.Children.Count > 1)
                        {
                            item.Children.RemoveAt(1);
                        }
                        Border bd = new Border() { Classes = new Classes("oneword") };
                        bd.Child = new TextBlock { Text = mnemonic[i * 5 + j] };
                        item.Children.Add(bd);
                    }
                }
            }            
            nextStepStackPanel.IsVisible = true;
            newWalletStep0.IsVisible = false;
            //walletManagePanel.IsVisible = false;
        }
        public void OnNewWalletStep1ContinueClick(object sender, RoutedEventArgs e)
        {
            //TabItem settingItem = get            
            newWalletStep2 ??= this.FindControl<StackPanel>("sp_newWallet_step2");
            verifyMnemonic ??= this.FindControl<StackPanel>("sp_verifyMnemonic");
            SecureRandom rd = new SecureRandom();
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
                    if (item.Children.Count > 1)
                    {
                        item.Children.RemoveAt(1);
                    }
                    //item.Children.Clear();
                    var pos = i * 5 + j;
                    //item.Children.Add(new TextBox() { Text = (pos + 1) + "." });
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
        private void OnImportWalletFinishClicked(object sender, RoutedEventArgs e)
        {
            var importMnemonic = this.FindControl<StackPanel>("sp_importMnemonic");
            var mnemonicString = "";
            foreach(StackPanel item in importMnemonic.Children)
            {
                foreach(StackPanel childItem in item.Children)
                {
                    var mnemonicTextBox = childItem.Children[1] as TextBox;
                    var mnemonicText = mnemonicTextBox.Text.Trim();
                    if(mnemonicText == "")
                    {
                        var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                        {
                            ButtonDefinitions = ButtonEnum.Ok,
                            ContentTitle = "Mnemonic Error",
                            ContentMessage = "Please Enter the Right Mnemonic Phrase!"
                        });
                        msgBox.ShowDialog(this);
                        mnemonicTextBox.Focus();
                        return;
                    }
                    mnemonicString += mnemonicText + " ";
                }
            }

            try
            {
                algoAccount = new Account(mnemonicString.Trim());
            }
            catch (Exception)
            {
                var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Mnemonic Error",
                    ContentMessage = "Please Enter the Right Mnemonic Phrase!"
                });
                msgBox.ShowDialog(this);
                return;
            }            

            SaveSecurityInfo();

            importWalletStep2.IsVisible = false;
            walletOperationTabControl.IsVisible = true;
            sideBar.IsVisible = true;
            ChangeWalletRefresh();
        }
        private void OnCreateWalletFinishClicked(object sender, RoutedEventArgs e)
        {
            //var walletNameBox = this.FindControl<TextBox>("tb_walletName");
            //var walletName = walletNameBox.Text;
            //if (walletName is null || walletName.Length < 1)
            //{
            //    var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            //    {
            //        ButtonDefinitions = ButtonEnum.Ok,
            //        ContentTitle = "Wallet Name Error",
            //        ContentMessage = "Please Enter the Wallet Name!"
            //    });
            //    msgBox.ShowDialog(this);
            //    walletNameBox.Focus();
            //    return;
            //}
            foreach (var item in mnemonicBoxes)
            {
                //var box = this.FindControl<TextBox>(item);
                var boxContent = item.Value.Text;
                if (!(boxContent is null))
                    boxContent = boxContent.Trim();
                var index = item.Key;
                if (boxContent != mnemonic[index])
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

            ///create account success
            ///store information in the setting file

            //var walletPassword = this.FindControl<TextBox>("tb_walletPassword");
            //var password = walletPassword.Text;
            //if (password is null || password.Length < 1)
            //{
            //    var msgBox = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            //    {
            //        ButtonDefinitions = ButtonEnum.Ok,
            //        ContentTitle = "Wallet Password Error",
            //        ContentMessage = "Please Enter the Password!"
            //    });
            //    msgBox.ShowDialog(this);
            //    walletPassword.Focus();
            //    return;
            //}
            //var accountPassword = password;
            SaveSecurityInfo();

            newWalletStep2.IsVisible = false;
            walletOperationTabControl.IsVisible = true;
            sideBar.IsVisible = true;
            ChangeWalletRefresh();
        }

        private void SaveSecurityInfo()
        {
            if (settings.Accounts == null || settings.Accounts.Length < 1)
            {
                settings.Accounts = new string[] { walletName };
            }
            else
            {
                var accountList = new List<string> { walletName };
                accountList.AddRange(settings.Accounts);
                settings.Accounts = accountList.ToArray();
            }

            if (settings.Salt == null || settings.Salt.Length < 1)
            {
                settings.Salt = new string[] {
                    //CryptoUtils.EncryptAES(accountPassword)
                    Convert.ToBase64String(salt)
                };
            }
            else
            {
                var psdList = new List<string>
                {
                    Convert.ToBase64String(salt)
                };
                psdList.AddRange(settings.Salt);
                settings.Salt = psdList.ToArray();
            }
            //var mnemonicString = GetMnemonicString(mnemonic);
            //algoAccount = new Account(mnemonicString);
            //var encryptedMasterKey = CryptoUtils.EncryptAES(accountPassword, Mnemonic.ToKey(mnemonicString));
            var checkSalt = CryptoUtils.GetCheckSalt(scryptedKey);
            if (settings.CheckSalt == null || settings.CheckSalt.Length < 1)
            {
                settings.CheckSalt = new string[] {
                    Convert.ToBase64String(checkSalt)
                };
            }
            else
            {
                var keyList = new List<string>
                {
                    Convert.ToBase64String(checkSalt)
                };
                keyList.AddRange(settings.CheckSalt);
                settings.CheckSalt = keyList.ToArray();
            }

            var aesgcmKey = CryptoUtils.GetMasterKey(scryptedKey);
            var nonce = Encoding.UTF8.GetBytes("algo--wallet");
            var aesgcmCipherBytes = CryptoUtils.EncryptAesGcm(aesgcmKey, nonce,
                Mnemonic.ToKey(algoAccount.ToMnemonic()));
            //var aesgcmCipherBytessss = CryptoUtils.EncryptWithKey(aesgcmKey, nonce, Mnemonic.ToKey(algoAccount.ToMnemonic()));
            var cipherText = CryptoUtils.GetCipherTextFromAesGcmResult(aesgcmCipherBytes);
            var tag = CryptoUtils.GetTagFromAesGcmResult(aesgcmCipherBytes);

            if (settings.CipherText == null || settings.CipherText.Length < 1)
            {
                settings.CipherText = new string[] {
                    Convert.ToBase64String(cipherText)
                };
            }
            else
            {
                var keyList = new List<string>
                {
                    Convert.ToBase64String(cipherText)
                };
                keyList.AddRange(settings.CipherText);
                settings.CipherText = keyList.ToArray();
            }

            if (settings.Tag == null || settings.Tag.Length < 1)
            {
                settings.Tag = new string[] {
                    Convert.ToBase64String(tag)
                };
            }
            else
            {
                var keyList = new List<string>
                {
                    Convert.ToBase64String(tag)
                };
                keyList.AddRange(settings.Tag);
                settings.Tag = keyList.ToArray();
            }
        }

        public void OnNewWalletCancel(object sender, RoutedEventArgs e)
        {
            if (newWalletStep0 != null)
                newWalletStep0.IsVisible = false;
            if(newWalletStep1 != null)
                newWalletStep1.IsVisible = false;
            if(newWalletStep2 != null)
                newWalletStep2.IsVisible = false;
            if (importWalletStep2 != null)
                importWalletStep2.IsVisible = false;
            //sideBar.IsVisible = true;
            //walletOperationTabControl.IsVisible = true;
            CheckAccount();

        }
        //public void OnNewWalletStep2Cancel(object sender, RoutedEventArgs e)
        //{
        //    newWalletStep2.IsVisible = false;
        //    sideBar.IsVisible = true;
        //    walletOperationTabControl.IsVisible = true;
        //}
        public void OnAlgoOrAssetSendClick(object sender, RoutedEventArgs e)
        {
            var sendingStatus = this.FindControl<TextBlock>("tb_sendingStatus");
            sendingStatus.IsVisible = true;
            var sendToAddress = this.FindControl<TextBox>("tb_sendToAddress");
            var sendToAmount = this.FindControl<TextBox>("tb_sendToAmount");
            var sendToStringMessage = this.FindControl<TextBox>("tb_sendToMessage").Text ?? "";
            ulong sendToIntAmount;
            if (!Address.IsValid(sendToAddress.Text))
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Address No Valid",
                    "The address is not valid, please enter a valid address.").ShowDialog(this);
                sendToAddress.Focus();
                sendingStatus.IsVisible = false;
                return;
            }
            if(sendToAmount.Text is null || sendToAmount.Text == "")
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Please Enter Amount",
                    "The amount can not be empty, please enter a right number.").ShowDialog(this);
                sendToAmount.Focus();
                sendingStatus.IsVisible = false;
                return;
            }
            try
            {
                sendToIntAmount = Convert.ToUInt64(sendToAmount.Text);
            }
            catch
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Amount Error",
                    "The amount should be a unsigned int, please enter a right number.").ShowDialog(this);
                sendToAmount.Focus();
                sendingStatus.IsVisible = false;
                return;
            }
            Algorand.Algod.Client.Model.TransactionParams transParams = null;
            try
            {
                transParams = algoInstance.TransactionParams();
            }
            catch (ApiException apiex)
            {
                MessageBoxManager.GetMessageBoxStandardWindow("Could not get params",
                    "Could not get params with exception:" + apiex.Message + ". Please try again later.").ShowDialog(this);
                //throw new Exception("Could not get params", e);
                sendingStatus.IsVisible = false;
                return;
            }
            if (selectedAssetId == 0)//send algos
            {                
                var amount = Utils.AlgosToMicroalgos(sendToIntAmount);
                var tx = Utils.GetPaymentTransaction(algoAccount.Address, new Address(sendToAddress.Text.Trim()), 
                    amount, sendToStringMessage, transParams);

                // send the transaction to the network
                try
                {
                    var signedTx = algoAccount.SignTransaction(tx);
                    var id = Utils.SubmitTransaction(algoInstance, signedTx);
                    //Console.WriteLine("Successfully sent tx with id: " + id.TxId);
                }
                catch (ApiException apiex)
                {
                    // This is generally expected, but should give us an informative error message.
                    MessageBoxManager.GetMessageBoxStandardWindow("Exception",
                        "Exception when sending:" + apiex.Message).ShowDialog(this);
                    //throw new Exception("Could not get params", e);
                    sendingStatus.IsVisible = false;
                    return;
                }
            }else //send ASA
            {
                var tx = Utils.GetTransferAssetTransaction(algoAccount.Address, new Address(sendToAddress.Text.Trim()), 
                    selectedAssetId, sendToIntAmount, transParams, null, sendToStringMessage);
                var signedTx = algoAccount.SignTransaction(tx);

                try
                {
                    var id = Utils.SubmitTransaction(algoInstance, signedTx);
                    //Console.WriteLine("Transaction ID: " + id.TxId);
                    //Console.WriteLine(Utils.WaitTransactionToComplete(algoInstance, id.TxId));
                    // We can now list the account information for acct3 
                    // and see that it now has 5 of the new asset
                    //act = algodApiInstance.AccountInformation(acct3.Address.ToString());
                    //Console.WriteLine(act.GetHolding(assetID).Amount);
                }
                catch (Exception apiex)
                {
                    //e.printStackTrace();
                    MessageBoxManager.GetMessageBoxStandardWindow("Exception",
                        "Exception when sending:" + apiex.Message).ShowDialog(this);
                    //throw new Exception("Could not get params", e);
                    sendingStatus.IsVisible = false;
                    return;
                }
            }
            sendingStatus.Text = "Send Success!";
        }
        
        private void Box_DataContextChanged(object sender, EventArgs e)
        {
            //throw new NotImplementedException();
        }    
        private void ChangeWalletRefresh()
        {
            //change the wallet
            //step 0: change the address
            this.FindControl<TextBox>("tb_accountAddress").Text = algoAccount.Address.ToString();
            //step 1: clear the assets and assets buttons
            accountAssets.Clear();
            assetsListPanel.Children.Clear();
            //step 2: clear the transactions and the transaction infomation
            transList.Clear();
            this.FindControl<StackPanel>("sp_transInfos").Children.Clear();
            //step 3: runnig the updating
            //if (!(updateingThread is null))
            //    updateingThread.Abort();
            
            //updateingThread.c
            //CancellationTokenSource cancelTokenSource = new CancellationTokenSource();
            //new Task(() => this.UpdateAssetsAndTransactions(cancelTokenSource.Token), )
            new Thread(new ThreadStart(this.UpdateAssetsAndTransactions)) { IsBackground = true }.Start();
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
            sideBar.IsVisible = false;
            walletOperationTabControl.IsVisible = false;
            createAsset.IsVisible = true;
        }
        public void OnDoCreateAssetClick(object sender, RoutedEventArgs e)
        {
            var name = this.FindControl<TextBox>("tb_assetName").Text;
            if(name is null || name.Length < 1)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Asset Name Not Right",
                    ContentMessage = "Please enter the asset name."
                });
                msBoxStandardWindow.ShowDialog(this);
                this.FindControl<TextBox>("tb_assetName").Focus();
                return;
            }
            var unitName = this.FindControl<TextBox>("tb_unitName").Text;
            if (unitName is null || unitName.Length < 1 || unitName.Length > 8)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Unit Name Not Right",
                    ContentMessage = "Please enter the unit name. The length of unit name should between 1 and 8."
                });
                msBoxStandardWindow.ShowDialog(this);
                this.FindControl<TextBox>("tb_unitName").Focus();
                return;
            }
            ulong total;
            try
            {
                total = Convert.ToUInt64(this.FindControl<TextBox>("tb_assetTotal").Text);
            }
            catch (Exception)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Total Number Not Right",
                    ContentMessage = "The format of the total number not right. Please enter a number."
                });
                msBoxStandardWindow.ShowDialog(this);
                this.FindControl<TextBox>("tb_assetTotal").Focus();
                return;
            }
            int decimals;
            try
            {
                decimals = Convert.ToInt32(this.FindControl<TextBox>("tb_assetDecimals").Text);
                if(decimals > 19 || decimals < 0)
                {
                    var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                    {
                        ButtonDefinitions = ButtonEnum.Ok,
                        ContentTitle = "Unit Decimals Not Right",
                        ContentMessage = "Please enter the decimals. The length of decimals should between 0 and 19."
                    });
                    msBoxStandardWindow.ShowDialog(this);
                    this.FindControl<TextBox>("tb_assetDecimals").Focus();
                    return;
                }
            }
            catch (Exception)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Unit Decimals Not Right",
                    ContentMessage = "The format of the decimals not right. Please enter a number."
                });
                msBoxStandardWindow.ShowDialog(this);
                this.FindControl<TextBox>("tb_assetDecimals").Focus();
                return;
            }
            var metadatahash = this.FindControl<TextBox>("tb_metadatahash").Text;
            var metadataerror = true;
            if(metadatahash is null || metadatahash.Length == 0)
            {
                metadatahash = Utils.GetRandomAssetMetaHash();
                metadataerror = false;
            }else //if (metadatahash.Length > 0)
            {
                var metabytes = Encoding.UTF8.GetBytes(metadatahash);
                if (metabytes.Length == 32)
                {
                    metadatahash = Convert.ToBase64String(metabytes);
                    metadataerror = false;
                }
                else if(metadatahash.Length != 44 && metadatahash.EndsWith('='))
                {
                    try
                    {
                        Convert.FromBase64String(metadatahash);
                    }
                    catch (Exception) { }
                    metadataerror = false;
                }
            }
            if (metadataerror)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "The Metadata Hash Not Right",
                    ContentMessage = "The format of the Metadata Hash not right. Please enter a 32 bytes string or the base64 encoded string."
                });
                msBoxStandardWindow.ShowDialog(this);
                this.FindControl<TextBox>("tb_metadatahash").Focus();
                return;
            }
            var frozen = this.FindControl<CheckBox>("cb_assetFrozen").IsChecked;
            var url = this.FindControl<TextBox>("tb_assetUrl").Text;
            url ??= "";            
            var managerAddress = this.FindControl<TextBox>("tb_assetManager").Text;            
            var reserverAddress = this.FindControl<TextBox>("tb_assetReserver").Text;            
            var clawbackerAddress = this.FindControl<TextBox>("tb_assetClawbacker").Text;            
            var freezerAddress = this.FindControl<TextBox>("tb_assetFreezer").Text;

            //if (managerAddress is null ||
            //    reserverAddress is null ||
            //    clawbackerAddress is null ||
            //    freezerAddress is null)
            //{
            //    var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
            //    {
            //        ButtonDefinitions = ButtonEnum.Ok,
            //        ContentTitle = "Default Address",
            //        ContentMessage = "If you leave the Manager Address, Reserve Address, Clawbacke Address or Freeze Address to blank, current user account address will be used."
            //    });
            //    msBoxStandardWindow.ShowDialog(this);
            //    //this.FindControl<TextBox>("tb_metadatahash").Focus();
            //}
            managerAddress ??= algoAccount.Address.ToString();
            reserverAddress ??= algoAccount.Address.ToString();
            clawbackerAddress ??= algoAccount.Address.ToString();
            freezerAddress ??= algoAccount.Address.ToString();
            var ap = new Algorand.Algod.Client.Model.AssetParams(creator: algoAccount.Address.ToString(), assetname: name,
                unitname: unitName, defaultfrozen: frozen, total: total,
                url: url, managerkey: managerAddress, reserveaddr: reserverAddress, clawbackaddr: clawbackerAddress,
                freezeaddr:freezerAddress, metadatahash: metadatahash);
            //ap.Creator = algoAccount.Address.ToString();
            //ap.Metadatahash = "16efaa3924a6fd9d3a4880099a4ac65d";

            var transParams = algoInstance.TransactionParams();
            var tx = Utils.GetCreateAssetTransaction(ap, transParams, "asset generate by AlgoWallet", decimals);

            // Sign the Transaction by sender
            SignedTransaction signedTx = algoAccount.SignTransaction(tx);
            // send the transaction to the network and
            // wait for the transaction to be confirmed
            try
            {
                var id = Utils.SubmitTransaction(algoInstance, signedTx);
                Utils.WaitTransactionToComplete(algoInstance, id.TxId);
                var ptx = algoInstance.PendingTransactionInformation(id.TxId);
                //var ptx = algoInstance.Transaction(id.TxId);
                selectedAssetId = (ulong)ptx.Txresults.Createdasset;
                var pair = new KeyValuePair<ulong, Algorand.Algod.Client.Model.AssetParams>(selectedAssetId, ap);
                accountAssets.Add(pair);
                UpdateAssetsList(accountAssets);
                //clear all the text

                createAsset.IsVisible = false;
                sideBar.IsVisible = true;
                walletOperationTabControl.IsVisible = true;
            }
            catch (Exception)
            {
                var msBoxStandardWindow = MessageBoxManager.GetMessageBoxStandardWindow(new MessageBoxStandardParams
                {
                    ButtonDefinitions = ButtonEnum.Ok,
                    ContentTitle = "Asset Create Error",
                    ContentMessage = "Error when create the asset, please try again."
                });
                msBoxStandardWindow.ShowDialog(this);
                return;
            }
            //Console.WriteLine("AssetID = " + assetID);
        }
        private void OnCreateAssetCancle(object sender, RoutedEventArgs e)
        {
            sideBar.IsVisible = true;
            walletOperationTabControl.IsVisible = true;
            createAsset.IsVisible = false;
        }
        private void OnResetAccessPoint(object sender, RoutedEventArgs e)
        {
            ShowConfigAccessPoint();
        }
        private void OnBackToHomeClicked(object sender, RoutedEventArgs e)
        {
            CheckAccount();
        }
    }
}
