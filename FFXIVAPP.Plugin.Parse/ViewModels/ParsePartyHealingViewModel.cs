﻿// FFXIVAPP.Plugin.Parse
// ParsePartyHealingViewModel.cs
//  
// Created by Ryan Wilson.
// Copyright © 2007-2013 Ryan Wilson - All Rights Reserved

#region Usings

using System.ComponentModel;
using System.Runtime.CompilerServices;

#endregion

namespace FFXIVAPP.Plugin.Parse.ViewModels
{
    internal sealed class ParsePartyHealingViewModel : INotifyPropertyChanged
    {
        #region Property Bindings

        private static ParsePartyHealingViewModel _instance;

        public static ParsePartyHealingViewModel Instance
        {
            get { return _instance ?? (_instance = new ParsePartyHealingViewModel()); }
        }

        #endregion

        #region Declarations

        #endregion

        #region Loading Functions

        #endregion

        #region Utility Functions

        #endregion

        #region Command Bindings

        #endregion

        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        private void RaisePropertyChanged([CallerMemberName] string caller = "")
        {
            PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

        #endregion
    }
}