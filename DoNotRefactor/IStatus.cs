namespace DoNotRefactor
{
    public interface IStatus
    {
        void Poll();
        PollByExternalReferenceResponse PollByExternalReference(string externalReference);
    }
}
