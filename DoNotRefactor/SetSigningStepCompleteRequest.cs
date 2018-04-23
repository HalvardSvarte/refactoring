using System;
using System.Runtime.Serialization;

namespace DoNotRefactor
{
    [DataContract(Namespace = "DoNotRefactor")]
    public class SetSigningStepCompleteRequest
    {
        [DataMember(IsRequired = true)]
        public SigningMethodEnum SigningMethod { get; set; }

        [DataMember(IsRequired = true)]
        public Guid SignTaskId { get; set; }

        [DataMember(IsRequired = true)]
        public string CustomerNumber { get; set; }
    }
}
