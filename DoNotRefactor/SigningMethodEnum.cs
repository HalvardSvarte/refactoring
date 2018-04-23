using System.Runtime.Serialization;

namespace DoNotRefactor
{
    [DataContract(Namespace = "DoNotRefactor")]
    public enum SigningMethodEnum
    {
        [EnumMember]
        NotSignedYet,

        [EnumMember]
        BankIdNorway,

        [EnumMember]
        DownloadedAndPrinted,

        [EnumMember]
        GotItInTheMail,

        [EnumMember]
        BankIdSweden,

        [EnumMember]
        BankIdMobileSweden,

        [EnumMember]
        Telia,

        [EnumMember]
        Nordea
    }
}
