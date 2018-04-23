using System;
using System.Runtime.Serialization;

namespace DoNotRefactor
{
    [DataContract(Namespace = "DoNotRefactor")]
    public class UpdateDocumentStatusRequest
    {
        [DataMember(IsRequired = true)]
        public Guid SignTaskId { get; set; }

        [DataMember(IsRequired = true)]
        public string CustomerNumber { get; set; }

        [DataMember(IsRequired = true)]
        public string Reference { get; set; }

        [DataMember(IsRequired = true)]
        public bool IsSigned { get; set; }

        [DataMember(IsRequired = true)]
        public bool IsPresentedToUser { get; set; }
    }
}
