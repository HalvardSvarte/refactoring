using System;
using System.Collections.Generic;

namespace DoNotRefactor
{
    public interface IDocumentSignRepository
    {
        List<SignTask> GetSignTasksByStatusCode(SignTaskStatusCode statusCode);
        List<SignTask> GetSignTasksByStatusCode(SignTaskStatusCode statusCode, DateTime cutoffDate);
        List<SignTask> GetSignTasksByExternalReference(string externalReference);
    }
}
