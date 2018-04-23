using System;
using System.Runtime.Serialization;

namespace DoNotRefactor
{
    [DataContract(Namespace = "DoNotRefactor")]
    public class SetSignTaskCompleteRequest
    {
        [DataMember(IsRequired = true)]
        public Guid SignTaskId { get; set; }

        [DataMember(IsRequired = true)]
        public string CustomerNumber { get; set; }
    }
}
