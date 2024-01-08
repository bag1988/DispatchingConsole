namespace SharedLibrary.Models
{
    public enum PimpAnswerState
    {
        // нужный объект не был привязан к кнопке, рисовать пимпу не надо
        NotFound,

        // Тевкову документировать состояние
        Answered16,

        // Тевкову документировать состояние
        NotAnswered16,
    }

    public enum StopState
    {
        /// <summary>
        /// тест
        /// </summary>
        TEST,
        /// <summary>
        /// стоп
        /// </summary>
        STOP,
        /// <summary>
        /// прервать
        /// </summary>
        ABORT
    }

    public enum SourceMsgNotify
    {
        No,
        Microphone,
        Records
    }

    public class ButtonColor
    {
        public string BGColor { get; set; } = "outline-gray";
        public string Color { get; set; } = "dark";
    }
}
