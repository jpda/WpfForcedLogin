﻿using System.Windows.Media.Imaging;
using WpfBasicForcedLogin.Helpers;

namespace WpfBasicForcedLogin.ViewModels
{
    public class UserViewModel : Observable
    {
        private string _name;
        private string _userPrincipalName;
        private BitmapImage _photo;

        public string Name
        {
            get => _name;
            set => Set(ref _name, value);
        }

        public string UserPrincipalName
        {
            get => _userPrincipalName;
            set => Set(ref _userPrincipalName, value);
        }

        public BitmapImage Photo
        {
            get => _photo;
            set => Set(ref _photo, value);
        }

        public UserViewModel()
        {
        }
    }
}
