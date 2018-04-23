namespace DoNotRefactor
{
    public class SignTaskDocument
    {
        public SignTaskDocumentType SignTaskDocumentType { get; set; }
        public string Reference { get; set; }
        public bool IsElectronicSignatureAllowed { get; set; }
    }
}
