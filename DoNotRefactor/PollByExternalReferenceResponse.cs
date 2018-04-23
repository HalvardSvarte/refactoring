using System.Runtime.Serialization;

namespace DoNotRefactor
{
    [DataContract(Namespace = "DoNotRefactor")]

    public class PollByExternalReferenceResponse
    {
        [DataMember]
        public bool IsUpdated { get; set; }
    }
}
