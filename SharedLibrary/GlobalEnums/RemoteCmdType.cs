namespace SharedLibrary.GlobalEnums
{
    public enum RemoteCmdType
    {
        CMD_ERR = 0,
        CMD_REGIST = 1,         //регистрация подчиненного/соседнего штаба
        CMD_DELREG = 2,         //удаление регистрации штаба
        CMD_UPREG = 3               //повторное согласование данных регистрации
    }
}
