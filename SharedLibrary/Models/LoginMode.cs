namespace SharedLibrary.Models
{
    public class LoginMode
    {
        public string? LoginUserMode { get; set; }//Режим входа: 0 - обычный вход ГСО / 1 - смешанный (доменный + локальный). Определяется формой ввода "ДОМЕН\Пользователь"

        public string? LoginSSOMode { get; set; }//Режим входа в домен: 0 - С вводом пользователя и пароля / 1 - автологин текущего пользователя ОС

        public string? LoginADBase { get; set; }//Домен или Distinguished name (DN)

        public LoginMode()
        {
        }

        public LoginMode(LoginMode other)
        {
            this.LoginUserMode = other.LoginUserMode;
            this.LoginSSOMode = other.LoginSSOMode;
            this.LoginADBase = other.LoginADBase;
        }

    }
}
