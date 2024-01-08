namespace SharedLibrary.GlobalEnums
{
    public enum LineChannelState
    {
        CHAN_OK = 0x0,                //Все в порядке
        LINE_OK = 0x0,                //Все в порядке
        CHAN_NEW = 0x1,               //Новый канал -- не настроен
        CHAN_MOVED_ON_PORT = 0x2,     //Устройство было переставлено в другой слот кросс-платы
        CHAN_MOVED_PORT = 0x3,        //Устройство было подключено к другому порту
        CHAN_OLD = 0x4,               //Устройство устарело -- есть в базе, но нет на порту
        CHAN_NEED_BINDING = -1,       //Канал нуждается в привязке к линии
        CHAN_OFF = -2,                //Канала физически нет
        CHAN_DISABLED = -3,           //Канал отключен
        CHAN_BUSY = 0x200,            //Канал занят клиентом
        CHAN_BUSY_UNPRIORITY = 0x201 //Канал занят клиентом, которого можно перехватить
    }
}
