using System.Collections.Generic;

namespace Couchpotato.Business.Settings.Models
{
    public class UserSettingsValidation
    {

        private readonly static int MAX_CONTENT_LENGTH = 10000000;
        private int _minimumContentLength = 100000;

        public UserSettingsValidation()
        {
            Enabled = false;
            ContentTypes = new string[] { };
            ShowInvalid = false;
        }

        public bool SingleEnabled { get; set; }
        public bool GroupEnabled { get; set; }
        public string[] ContentTypes { get; set; }
        public bool ShowInvalid { get; set; }
        public string InvalidSufix { get; set; }
        public int MinimumContentLength
        {
            get
            {
                return _minimumContentLength;
            }
            set
            {
                if (value > MAX_CONTENT_LENGTH)
                {
                    value = MAX_CONTENT_LENGTH;
                }
                else
                {
                    _minimumContentLength = value;
                }


            }
        }
        public List<UserSettingsValidationFallback> DefaultFallbacks { get; set; }
    }
}



