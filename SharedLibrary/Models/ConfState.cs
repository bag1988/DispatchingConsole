using GateServiceProto.V1;

namespace SharedLibrary.Models
{
    public class ConfState
    {
        public List<ResponseCode> Errors { get; set; } = new();
        public List<ResponseCode> Warnings { get; set; } = new();
    }
}
