namespace DoNotRefactor
{
    public interface IElectronicSignatureProviderStatus
    {
        bool IsSignedByAll(SignTask signTask);
    }
}
