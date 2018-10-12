using System.Runtime.Serialization;

namespace YAF.Types.Objects
{
    [DataContract]
    public class VokabularValidationResponse
    {
        [DataMember(Name = "active")]
        public bool Active { get; set; }
    }
}
