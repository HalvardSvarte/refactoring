using System;
using System.Collections.Generic;
using DoNotRefactor;

namespace Tests
{
    public class TestData
    {
        public static SignTask SignTaskSigningCompleted()
        {
            return new SignTask
            {
                ApplicationData = new ApplicationData(),
                StatusCode = SignTaskStatusCode.SigningCompleted,
                Documents = new List<SignTaskDocument>
                {
                    new SignTaskDocument
                    {
                        SignTaskDocumentType = SignTaskDocumentType.Document,
                        IsElectronicSignatureAllowed = true,
                        Reference = Guid.NewGuid().ToString()
                    }
                },
                ExternalReference = "123456",
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid().ToString(),
                Signers = new List<Signer>
                {
                    new Signer
                    {
                        CustomerNumber = "10101012345",
                        Id = Guid.NewGuid()
                    }
                }
            };
        }

        public static SignTask SignTaskSentToElectronicSignatureProvider()
        {
            return new SignTask
            {
                ApplicationData = new ApplicationData(),
                StatusCode = SignTaskStatusCode.SentToElectronicSignatureProvider,
                Documents = new List<SignTaskDocument>
                {
                    new SignTaskDocument
                    {
                        SignTaskDocumentType = SignTaskDocumentType.Document,
                        IsElectronicSignatureAllowed = true,
                        Reference = Guid.NewGuid().ToString()
                    }
                },
                ExternalReference = "123457",
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid().ToString(),
                Signers = new List<Signer>
                {
                    new Signer
                    {
                        CustomerNumber = "10101012346",
                        Id = Guid.NewGuid()
                    }
                }
            };
        }

        public static SignTask PartiallySignedSignTaskSentToElectronicSignatureProvider()
        {
            return new SignTask
            {
                ApplicationData = new ApplicationData(),
                StatusCode = SignTaskStatusCode.SentToElectronicSignatureProvider,
                Documents = new List<SignTaskDocument>
                {
                    new SignTaskDocument
                    {
                        SignTaskDocumentType = SignTaskDocumentType.Document,
                        IsElectronicSignatureAllowed = true,
                        Reference = Guid.NewGuid().ToString()
                    }
                },
                ExternalReference = "123457",
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid().ToString(),
                Signers = new List<Signer>
                {
                    new Signer
                    {
                        CustomerNumber = "10101012346",
                        Id = Guid.NewGuid()
                    },
                    new Signer
                    {
                        CustomerNumber = "10101012347",
                        Id = Guid.NewGuid()
                    }
                }
            };
        }
    }
}
