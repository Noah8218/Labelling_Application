using OpenVisionLab;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MvcVisionSystem
{
    public sealed class WpfLocalizationViewModel : WpfObservableViewModel
    {
        private static readonly Lazy<WpfLocalizationViewModel> LazyInstance =
            new Lazy<WpfLocalizationViewModel>(() => new WpfLocalizationViewModel());

        private OpenVisionLanguageOption selectedLanguage;

        private WpfLocalizationViewModel()
        {
            OpenVisionLanguageService.Load();
            LanguageOptions = OpenVisionLanguageService.GetLanguageOptions();
            selectedLanguage = FindCurrentLanguage();
            OpenVisionLanguageService.LanguageChanged += OpenVisionLanguageService_LanguageChanged;
        }

        public static WpfLocalizationViewModel Instance => LazyInstance.Value;

        public event EventHandler LanguageChanged;

        public IReadOnlyList<OpenVisionLanguageOption> LanguageOptions { get; }

        public OpenVisionLanguageOption SelectedLanguage
        {
            get => selectedLanguage;
            set
            {
                if (value == null || ReferenceEquals(selectedLanguage, value))
                {
                    return;
                }

                selectedLanguage = value;
                OnPropertyChanged();
                OpenVisionLanguageService.SetLanguage(value.Language, save: true);
            }
        }

        public string this[string key] => OpenVisionLanguageService.T(key);

        public void Reload()
        {
            OpenVisionLanguageService.Load();
            SynchronizeLanguage(notifyLanguageChanged: true);
        }

        private void OpenVisionLanguageService_LanguageChanged(object sender, EventArgs e)
        {
            SynchronizeLanguage(notifyLanguageChanged: true);
        }

        private void SynchronizeLanguage(bool notifyLanguageChanged)
        {
            OpenVisionLanguageOption current = FindCurrentLanguage();
            if (!ReferenceEquals(selectedLanguage, current))
            {
                selectedLanguage = current;
                OnPropertyChanged(nameof(SelectedLanguage));
            }

            OnPropertyChanged(string.Empty);
            if (notifyLanguageChanged)
            {
                LanguageChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private OpenVisionLanguageOption FindCurrentLanguage()
        {
            return LanguageOptions.First(option => option.Language == OpenVisionLanguageService.CurrentLanguage);
        }
    }
}
