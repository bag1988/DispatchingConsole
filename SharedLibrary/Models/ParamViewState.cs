namespace SharedLibrary.Models
{
    public class ParamViewState
    {
        public int IntervalUpdateState { get; set; } = 60;

        public bool IsCheckCu { get; set; } = true;

        public bool IsPOSIgnore { get; set; } = false;

        public bool IsHistoryCommand { get; set; } = false;
    }
}
