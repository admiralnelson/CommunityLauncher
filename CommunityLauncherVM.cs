﻿using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Launcher;
using TaleWorlds.MountAndBlade.Launcher.UserDatas;

namespace CommunityLauncher
{
    public class CommunityLauncherVM : ViewModel
    {
        private UserDataManager _userDataManager;
        private readonly Action _onClose;

        private readonly Action _onMinimize;

        private bool _isInitialized;

        private bool _isMultiplayer;

        private bool _isSingleplayerAvailable;

        private LauncherNewsVM _news;

        private LauncherModsVM _dlcData;

        private ModsVM _getModsData;

        private LauncherModsVM _modsData;

        private string _playText;

        private string _singleplayerText;

        private string _multiplayerText;

        private string _newsText;

        private string _dlcText;

        private string _modsText;

        private string _versionText;

        public string GameTypeArgument
        {
            get
            {
                if (!IsMultiplayer)
                {
                    return "/singleplayer";
                }

                return "/multiplayer";
            }
        }

        public CommunityLauncherVM(UserDataManager userDataManager, Action onClose, Action onMinimize)
        {
            _userDataManager = userDataManager;
            _onClose = onClose;
            _onMinimize = onMinimize;
            PlayText = "P L A Y";
            SingleplayerText = "Singleplayer";
            MultiplayerText = "Multiplayer";
            NewsText = "News";
            DlcText = "DLC";
            ModsText = "Mods";
            CanLaunch = true;

            VersionText = ApplicationVersion.FromParametersFile().ToString();
            News = new LauncherNewsVM();
            DlcData = new LauncherModsVM(_userDataManager.UserData, true);
            ModsData = new LauncherModsVM(_userDataManager.UserData, false);
            GetModsData = new ModsVM(this);
            IsSingleplayerAvailable = GameModExists("Sandbox");
            IsMultiplayer = (!IsSingleplayerAvailable ||
                             _userDataManager.UserData.GameType == GameType.Multiplayer);

            Refresh();
            _isInitialized = true;
        }


        private void UpdateAndSaveUserModsData(bool isMultiplayer)
        {
            UserData userData = _userDataManager.UserData;
            UserGameTypeData userGameTypeData = isMultiplayer ? userData.MultiplayerData : userData.SingleplayerData;
            userGameTypeData.DlcDatas.Clear();
            userGameTypeData.ModDatas.Clear();
            foreach (LauncherModuleVM launcherModuleVM in DlcData.Modules)
            {
                userGameTypeData.DlcDatas.Add(new UserModData(launcherModuleVM.Info.Id, launcherModuleVM.IsSelected));
            }

            foreach (LauncherModuleVM launcherModuleVM2 in ModsData.Modules)
            {
                userGameTypeData.ModDatas.Add(new UserModData(launcherModuleVM2.Info.Id, launcherModuleVM2.IsSelected));
            }

            _userDataManager.SaveUserData();
        }

        private bool GameModExists(string modId)
        {
            List<ModuleInfo> list = ModuleInfo.GetModules().ToList();
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Id == modId)
                {
                    return true;
                }
            }

            return false;
        }

        private void OnBeforeGameTypeChange(bool preSelectionIsMultiplayer, bool newSelectionIsMultiplayer)
        {
            if (!_isInitialized)
            {
                return;
            }

            _userDataManager.UserData.GameType =
                (newSelectionIsMultiplayer ? GameType.Multiplayer : GameType.Singleplayer);
            UpdateAndSaveUserModsData(preSelectionIsMultiplayer);
        }

        private void ExecuteStartGame()
        {
            switch (PlayText)
            {
                case "Download & Install":
                    GetModsData.Install();
                    break;
                default:

                    UpdateAndSaveUserModsData(IsMultiplayer);
                    Program.StartRPGame();
                    break;
            }
        }

        private void ExecuteClose()
        {
            UpdateAndSaveUserModsData(IsMultiplayer);
            Action onClose = _onClose;

            onClose?.Invoke();
        }

        private void ExecuteMinimize()
        {
            Action onMinimize = _onMinimize;

            onMinimize?.Invoke();
        }

        private void Refresh()
        {
            News.Refresh(IsMultiplayer);
            DlcData.Refresh(IsMultiplayer);
            ModsData.Refresh(IsMultiplayer);
            GetModsData.Refresh();
        }

        [DataSourceProperty]
        public bool IsSingleplayer
        {
            get => !_isMultiplayer;
            set
            {
                if (_isMultiplayer != !value)
                {
                    IsMultiplayer = !value;
                }
            }
        }

        [DataSourceProperty]
        public bool IsMultiplayer
        {
            get => _isMultiplayer;
            set
            {
                if (_isMultiplayer != value)
                {
                    OnBeforeGameTypeChange(_isMultiplayer, value);
                    _isMultiplayer = value;
                    OnPropertyChanged();
                    OnPropertyChanged("IsSingleplayer");
                    Refresh();
                }
            }
        }

        [DataSourceProperty]
        public bool IsSingleplayerAvailable
        {
            get => _isSingleplayerAvailable;
            set
            {
                if (value != _isSingleplayerAvailable)
                {
                    _isSingleplayerAvailable = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string VersionText
        {
            get => _versionText;
            set
            {
                if (value == _versionText) return;
                _versionText = value;
                OnPropertyChanged();
            }
        }

        [DataSourceProperty]
        public LauncherNewsVM News
        {
            get => _news;
            set
            {
                if (value == _news) return;
                _news = value;
                OnPropertyChanged();
            }
        }

        [DataSourceProperty]
        public LauncherModsVM DlcData
        {
            get => _dlcData;
            set
            {
                if (value == _dlcData) return;
                _dlcData = value;
                OnPropertyChanged();
            }
        }

        [DataSourceProperty]
        public LauncherModsVM ModsData
        {
            get { return _modsData; }
            set
            {
                if (value != _modsData)
                {
                    _modsData = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public ModsVM GetModsData
        {
            get { return _getModsData; }
            set
            {
                if (value != _getModsData)
                {
                    _getModsData = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string PlayText
        {
            get { return _playText; }
            set
            {
                if (_playText != value)
                {
                    _playText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string SingleplayerText
        {
            get { return _singleplayerText; }
            set
            {
                if (_singleplayerText != value)
                {
                    _singleplayerText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string MultiplayerText
        {
            get { return _multiplayerText; }
            set
            {
                if (_multiplayerText != value)
                {
                    _multiplayerText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string NewsText
        {
            get { return _newsText; }
            set
            {
                if (_newsText != value)
                {
                    _newsText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string DlcText
        {
            get { return _dlcText; }
            set
            {
                if (_dlcText != value)
                {
                    _dlcText = value;
                    OnPropertyChanged();
                }
            }
        }

        [DataSourceProperty]
        public string ModsText
        {
            get { return _modsText; }
            set
            {
                if (_modsText != value)
                {
                    _modsText = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _canLaunch;
        [DataSourceProperty] public bool CanLaunch
        {
            get => _canLaunch;
            set
        {
            if (_canLaunch == value) return;
            _canLaunch = value;
            OnPropertyChanged();
        }
        }
    }
}