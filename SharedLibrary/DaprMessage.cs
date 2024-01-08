namespace SharedLibrary
{
    public struct DaprMessage
    {
        public const string PubSubName = "pubsub";
        public const string PublicPubSubName = "public-pubsub";
        public const string PubSubTopicName = "durable-deleted-when-unused-pko.topic";
        public const string PublicPubSubTopicName = "amq.topic";
        public const string SkudEntRoutingKey = "skud.*.*.*.*.ent";
        public const string RoutingKeyMetadata = "routingKey";
        public const string RawPayloadMetadata = "rawPayload";

        public const string Fire_UpdateSCUDresult = "start.ui.update.scud.result";//обновление статистики

        #region AsoNlService проверка линий связи
        public const string Fire_StopNl = "device.console.stop.nl";
        public const string Fire_OnMessageSignal = "device.console.on.message.signal";
        public const string Fire_OnErrorSignal = "device.console.on.error.signal";
        public const string Fire_OnAnswerSignal = "device.console.on.answer.signal";
        public const string Fire_OnStateLineSignal = "device.console.on.state.line.signal";
        #endregion

        public const string Fire_StartSessionSubCu = "start.ui.start.session.sub.cu";
        public const string Fire_KseonCP = "kseon.cp";

        #region Регистрация ПУ
        public const string Fire_CmdStatus = "device.console.cmd.status";
        public const string Fire_RemoteCuStaffID = "device.console.remote.cu.staff.id";
        #endregion

        #region не совсем понятные события
        public const string Fire_EventFromDialog = "event.from.dialog";
        public const string Fire_EventMsgBuffFromDialog = "event.msg.buff.from.dialog";
        public const string Fire_EventResultFromAwaitingRestartPC = "event.result.from.awaiting.restart.pc";
        public const string Fire_EventResultFromManualStartDialog = "event.result.fromm.anual.start.dialog";
        #endregion

        #region Запуск по расписанию
        public const string Fire_EndTask = "device.console.end.task";
        public const string Fire_InsertDeleteTask = "device.console.insert.delete.task";
        public const string Fire_StartTask = "device.console.start.task";
        public const string Fire_UpdateTask = "device.console.update.task";
        #endregion

        #region устройства, группы
        public const string Fire_UpdateTermDevice = "common.update.term.device";
        public const string Fire_InsertDeleteTermDevice = "common.insert.delete.term.device";
        public const string Fire_UpdateTermDevicesGroup = "common.update.term.devices.group";
        public const string Fire_InsertDeleteTermDevicesGroup = "common.insert.delete.term.devices.group";
        #endregion

        public const string Fire_ShowPushNotify = "common.show.push.notify";//системные уведомления(только для UI - service worker)
        public const string Fire_UpdateStatListCache = "start.ui.update.stat.list.cache";//обновление статистики
        public const string Fire_AddErrorState = "view.states.add.error.state";//Контроль состояния
        public const string Fire_AllUserLogout = "common.all.user.logout";//выход всех пользователей
        public const string Fire_RestartUi = "common.restart.ui";//обновление пользовательского интерфейса
        public const string Fire_AddLogs = "start.ui.add.logs";//журнал событий
        public const string Fire_StartSession = "start.ui.start.session";//обновление формы при запуске оповещения
        public const string Fire_EndSession = "start.ui.end.session";//завершение оповещения
        public const string Fire_ErrorCreateNewSituation = "start.ui.error.create.new.situation";//ошибка запуска сценария
        public const string Fire_Redraw_LV = "start.ui.redraw.lv";//ничего интересного, добавление ситуации
        public const string Fire_ErrorContinue = "start.ui.error.continue";//ошибка дооповещения сценария
        public const string Fire_NotifySessEvents = "start.ui.notify.sess.events";//запрос дооповещения
        public const string Fire_ShowManualStart = "start.ui.show.manual.start"; // запрос на запуск от SMP16x

        public const string Fire_CommandStop = "command.stop";//получение кол-во активных сценариев и т.д.

        public const string Fire_StartNewSituation = "start.ui.start.new.situation";//добавлен сценарий в запуск

        public const string Fire_NewAction = "new.action";//получаем инфо по сценарию

        public const string Fire_UpdateChannels = "start.ui.update.channels";//загрузка списка, обновление кэша

        public const string Fire_UnLoadModule = "unload.module";//закрытие окна

        public const string Fire_InsertDeleteControllingDevice = "common.insert.delete.controlling.device";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteAbonent = "common.insert.delete.abonent";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteData = "device.console.insert.delete.data";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteDepartment = "device.console.insert.delete.department";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteRegistration = "device.console.insert.delete.registration";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteShedule = "device.console.insert.delete.shedule";//загрузка списка, обновление кэша
        public const string Fire_AddEvent = "start.ui.add.event";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteExStatistics = "start.ui.insert.delete.ex.statistics";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteLine = "device.console.insert.delete.line";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteList = "common.insert.delete.list";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteLocation = "device.console.insert.delete.location";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteMessage = "common.insert.delete.message";//загрузка списка, обновление кэша
        public const string Fire_InsertDeleteSituation = "common.insert.delete.situation";//загрузка списка, обновление кэша
        public const string Fire_UpdateControllingDevice = "common.update.controlling.device";//обновление кэша
        public const string Fire_UpdateAbonent = "common.update.abonent";//обновление кэша
        public const string Fire_UpdateData = "device.console.update.data";//обновление кэша
        public const string Fire_UpdateDepartment = "device.console.update.department";//обновление кэша
        public const string Fire_UpdateExStatistics = "start.ui.update.ex.statistics";//обновление статистики ПУ
        public const string Fire_UpdateLine = "device.console.update.line";//обновление кэша
        public const string Fire_UpdateList = "common.update.list";//обновление кэша
        public const string Fire_UpdateLocation = "device.console.update.location";//обновление кэша
        public const string Fire_UpdateLabels = "device.console.update.labels";//обновление доп полей
        public const string Fire_UpdateMessage = "common.update.message";//обновление кэша
        public const string Fire_UpdateRegistration = "device.console.update.registration";//обновление кэша
        public const string Fire_UpdateShedule = "device.console.update.shedule";//обновление кэша
        public const string Fire_UpdateSndSetting = "device.console.update.snd.setting";   // обновление настроек звука
        public const string Fire_UpdateSituation = "common.update.situation";//обновление кэша

        public const string Fire_UpdateSubsystParam = "update.subsyst.param";//обновление параметров подсистемы

    }
}
