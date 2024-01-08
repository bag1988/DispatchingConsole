using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using SharedLibrary.GlobalEnums;

namespace BlazorLibrary.Helpers
{
    public static class CheckConnectParam
    {
        public static bool IsValid(string connParam, BaseLineType connType)
        {
            if (string.IsNullOrEmpty(connParam) && connType != BaseLineType.LINE_TYPE_DEDICATED)
            {
                return false;
            }
            if (connType == BaseLineType.LINE_TYPE_SMTP)
            {
                Regex regexGSM = new(@"[\{\}\:\""<>\[\];/~!\$%\^&\*\(\)+=|\\]");
                return !regexGSM.IsMatch(connParam);
            }
            else if (connType == BaseLineType.LINE_TYPE_DCOM)
            {
                return true;
            }
            else if (connType == BaseLineType.LINE_TYPE_WSDL)
            {
                return true;
            }
            else if (connType == BaseLineType.LINE_TYPE_SNMP_GATE)
            {
                return true;
            }
            else if (connType == BaseLineType.LINE_TYPE_GSM_TERMINAL)
            {
                Regex regexGSM = new(@"^\+?\d*$");
                return regexGSM.IsMatch(connParam);
            }
            else if (connType != BaseLineType.LINE_TYPE_DEDICATED)
            {
                //проверка на корректность ввода
                Regex regexGSM = new(@"[^\d,ptw\*#v]");
                return !regexGSM.IsMatch(connParam);
            }
            else
            {
                return true;
            }
        }
        public static string DeleteInvalidChar(string connParam, BaseLineType connType)
        {
            if (connType == BaseLineType.LINE_TYPE_SMTP)
            {
                Regex regexGSM = new(@"[\{\}\:\""<>\[\];/~!\$%\^&\*\(\)+=|\\]");
                return regexGSM.Replace(connParam, "");
            }
            else if (connType == BaseLineType.LINE_TYPE_GSM_TERMINAL)
            {
                Regex regexGSM = new(@"[^\+\d]");
                return regexGSM.Replace(connParam, "");
            }
            else if (connType != BaseLineType.LINE_TYPE_DEDICATED)
            {
                //проверка на корректность ввода
                Regex regexGSM = new(@"[^\d,ptw\*#v]");
                return regexGSM.Replace(connParam, "");
            }
            else
            {
                return connParam.Replace(" ", "");
            }
        }
    }
}
