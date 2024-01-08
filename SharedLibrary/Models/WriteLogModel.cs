namespace SharedLibrary.Models
{
    public enum ErrorType
    {
        Error_Type = 1,
        Warning_Type,
        Information_Type
    };

    public enum GSOModules
    {
        AsoNL_Module = 1,
        SZSNotifyLogic_Module,
        SCSLu_Module,
        SCSChlService_Module,
        SMP16xNL_Module,
        StaffNL_Module,
        Security_Module,
        DeviceConsole_Module,
        StartUI_Module,
        AsoForms_Module,
        SzsForms_Module,
        GsoForms_Module,
        P16Forms_Module,
        RdmForms_Module,
        ReporterForms_Module,
        StaffForms_Module,
        AutoTasks_Module,
        ViewStates_Module,
        UZSMonitor_Module, // нету
        GlobalStartGSO_Module,
        SMGate_Module,
        SMDataService_Module,
        AsoNLservice_Module,
        CLAsoLUGSMT_Module,
        CLAsoLULPT_Module,
        CLAsoLUGSM_Module,
        CLAsoLU43_Module,
        CLAsoLUVoIP_Module,
        _Dummy_Module,
    };
}
