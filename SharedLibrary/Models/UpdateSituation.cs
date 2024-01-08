using SMDataServiceProto.V1;

namespace SharedLibrary.Models
{
    public class UpdateSituation
    {
        public SituationInfo? Info { get; set; }
        public List<CGetSitItemInfo>? Items { get; set; }
    }

    public class UpdateSituationStaff
    {
        public SituationInfo? Info { get; set; }
        public List<SMControlSysProto.V1.SituationItem>? Items { get; set; }
        public int UserSessId { get; set; }
    }
}
