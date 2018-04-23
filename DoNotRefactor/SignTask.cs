using System;
using System.Collections.Generic;

namespace DoNotRefactor
{
    public class SignTask
    {
        public ApplicationData ApplicationData { get; set; }
        public string ExternalReference { get; set; }
        public SignTaskStatusCode StatusCode { get; set; }
        public Guid Id { get; set; }
        public List<Signer> Signers { get; set; }
        public string OrderId { get; set; }
        public List<SignTaskDocument> Documents { get; set; }
    }
}
