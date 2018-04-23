namespace DoNotRefactor
{
    public enum SignTaskStatusCode
    {
        Created = 0,
        SigningCompleted = 1,
        Completed = 2,
        Deactivated = 3,
        Rejected = 4,
        Archived = 5,
        SentToElectronicSignatureProvider = 6,
        Opened = 7
    }
}
