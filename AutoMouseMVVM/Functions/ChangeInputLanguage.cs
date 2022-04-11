using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AutoMouseMVVM.Functions
{
    class ChangeInputLanguage
    {
        /// <summary>
        /// 獲取當前輸入法
        /// </summary>
        /// <returns></returns>
        public string GetCultureType()
        {
            var currentInputLanguage = InputLanguage.CurrentInputLanguage;
            var cultureInfo = currentInputLanguage.Culture;
            //同 cultureInfo.IetfLanguageTag;
            return cultureInfo.Name;
        }
        /// <summary>
        /// 切換輸入法
        /// </summary>
        /// <param name="cultureType">語言項，如zh-CN，en-US</param>
        public void SwitchToLanguageMode(string cultureType)
        {
            var installedInputLanguages = InputLanguage.InstalledInputLanguages;

            if (installedInputLanguages.Cast<InputLanguage>().Any(i => i.Culture.Name == cultureType))
            {
                InputLanguage.CurrentInputLanguage = InputLanguage.FromCulture(System.Globalization.CultureInfo.GetCultureInfo(cultureType));

                //CurrentLanguage = cultureType;
            }
        }
    }
}