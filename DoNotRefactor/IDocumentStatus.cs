using System;

namespace DoNotRefactor
{
    public interface IDocumentStatus
    {
        DocumentStatus Get(Guid signTaskId, string documentReference, string customerNumber);
    }
}
