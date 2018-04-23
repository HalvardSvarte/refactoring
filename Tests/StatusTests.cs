using System;
using System.Collections.Generic;
using DoNotRefactor;
using Moq;
using NUnit.Framework;
using RefactorThis;

namespace Tests
{
    [TestFixture]
    public class StatusTests
    {
        #region Construction / SetUp

        private Mock<IDocumentSignRepository> _documentSignRepositoryMock;
        private Mock<ISetSignTaskComplete> _setSignTaskCompleteMock;
        private Mock<ISetSigningStepComplete> _setSigningStepCompleteMock;
        private Mock<IElectronicSignatureProviderStatus> _electronicSignatureProviderStatusMock;
        private Mock<IUpdateDocumentStatus> _updateDocumentStatusMock;
        private Mock<IESignature> _eSignatureMock;
        private Mock<IDocumentStatus> _documentStatusMock;
        private DocumentHubSettings _documentHubSettings;

        [SetUp]
        public void Setup()
        {
            _documentSignRepositoryMock = new Mock<IDocumentSignRepository>();
            _setSignTaskCompleteMock = new Mock<ISetSignTaskComplete>();
            _setSigningStepCompleteMock = new Mock<ISetSigningStepComplete>();
            _electronicSignatureProviderStatusMock = new Mock<IElectronicSignatureProviderStatus>();
            _updateDocumentStatusMock = new Mock<IUpdateDocumentStatus>();
            _eSignatureMock = new Mock<IESignature>();
            _documentStatusMock = new Mock<IDocumentStatus>();
            _documentHubSettings = new DocumentHubSettings{NumDaysBackForStatusPoller = 5};
        }

        private Status InitStatus()
        {
            return new Status(
                _documentSignRepositoryMock.Object,
                _setSignTaskCompleteMock.Object,
                _documentHubSettings,
                _setSigningStepCompleteMock.Object,
                _electronicSignatureProviderStatusMock.Object,
                _updateDocumentStatusMock.Object,
                _eSignatureMock.Object,
                _documentStatusMock.Object);
        }

        #endregion

        [Test]
        public void Poll_OneSignTaskStuckInStatusSigningComplete()
        {
            var signTaskSigningCompleted = TestData.SignTaskSigningCompleted();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SigningCompleted)))
                .Returns(new List<SignTask> {signTaskSigningCompleted});
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SentToElectronicSignatureProvider),
                        It.IsAny<DateTime>()))
                .Returns(new List<SignTask>());

            var status = InitStatus();
            status.Poll();

            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.Is<UpdateDocumentStatusRequest>(
                    u => u.SignTaskId == signTaskSigningCompleted.Id
                         && u.CustomerNumber == signTaskSigningCompleted.Signers[0].CustomerNumber
                         && u.IsPresentedToUser
                         && u.IsSigned
                         && u.Reference == signTaskSigningCompleted.Documents[0].Reference)));
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.Is<SetSignTaskCompleteRequest>(
                    s => s.CustomerNumber == signTaskSigningCompleted.Signers[0].CustomerNumber 
                         && s.SignTaskId == signTaskSigningCompleted.Id)),
                Times.Once);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Never);
        }

        [Test]
        public void Poll_OneSignTaskStuckInStatusSentToElectronicSignatureProviderButIsSigned()
        {
            var signTaskSentToElectronicSignatureProvider = TestData.SignTaskSentToElectronicSignatureProvider();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SigningCompleted)))
                .Returns(new List<SignTask>());
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SentToElectronicSignatureProvider),
                        It.IsAny<DateTime>()))
                .Returns(new List<SignTask> {signTaskSentToElectronicSignatureProvider});
            _electronicSignatureProviderStatusMock.Setup(
                    x => x.IsSignedByAll(
                        It.Is<SignTask>(st => st.Id == signTaskSentToElectronicSignatureProvider.Id)))
                .Returns(true);

            var status = InitStatus();
            status.Poll();

            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.Is<UpdateDocumentStatusRequest>(
                    u => u.SignTaskId == signTaskSentToElectronicSignatureProvider.Id
                         && u.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && u.IsPresentedToUser
                         && u.IsSigned
                         && u.Reference == signTaskSentToElectronicSignatureProvider.Documents[0].Reference)),
                Times.Once);
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.Is<SetSignTaskCompleteRequest>(
                    s => s.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && s.SignTaskId == signTaskSentToElectronicSignatureProvider.Id)),
                Times.Once);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.Is<SetSigningStepCompleteRequest>(
                    s => s.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && s.SignTaskId == signTaskSentToElectronicSignatureProvider.Id
                         && s.SigningMethod == SigningMethodEnum.BankIdNorway)),
                Times.Once);
        }

        [Test]
        public void Poll_OneSignTaskStuckInStatusSentToElectronicSignatureProviderAndIsNotSigned()
        {
            var signTaskSentToElectronicSignatureProvider = TestData.SignTaskSentToElectronicSignatureProvider();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SigningCompleted)))
                .Returns(new List<SignTask>());
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SentToElectronicSignatureProvider),
                        It.IsAny<DateTime>()))
                .Returns(new List<SignTask> { signTaskSentToElectronicSignatureProvider });
            _electronicSignatureProviderStatusMock.Setup(
                    x => x.IsSignedByAll(
                        It.Is<SignTask>(st => st.Id == signTaskSentToElectronicSignatureProvider.Id)))
                .Returns(false);

            var status = InitStatus();
            status.Poll();

            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.IsAny<UpdateDocumentStatusRequest>()),
                Times.Never);
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSignTaskCompleteRequest>()),
                Times.Never);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Never);
        }

        [Test]
        public void Poll_ManySignTasks()
        {
            var firstSignTask = TestData.SignTaskSigningCompleted();
            var secondSignTask = TestData.SignTaskSigningCompleted();
            secondSignTask.Signers[0].CustomerNumber = "10101023456";
            var thirdSignTask = TestData.SignTaskSentToElectronicSignatureProvider();
            var fourthSignTask = TestData.PartiallySignedSignTaskSentToElectronicSignatureProvider();
            var fifthSignTask = TestData.SignTaskSentToElectronicSignatureProvider();
            fifthSignTask.Documents.Add(new SignTaskDocument
            {
                SignTaskDocumentType = SignTaskDocumentType.Document,
                Reference = Guid.NewGuid().ToString(),
                IsElectronicSignatureAllowed = true
            });
            var sixthSignTask = TestData.SignTaskSentToElectronicSignatureProvider();
            sixthSignTask.Signers.Add(new Signer
            {
                Id = Guid.NewGuid(),
                CustomerNumber = "10101012321"
            });

            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SigningCompleted)))
                .Returns(new List<SignTask> {firstSignTask, secondSignTask});
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByStatusCode(
                        It.Is<SignTaskStatusCode>(
                            c => c == SignTaskStatusCode.SentToElectronicSignatureProvider),
                        It.IsAny<DateTime>()))
                .Returns(new List<SignTask>{thirdSignTask, fourthSignTask, fifthSignTask, sixthSignTask});
            _electronicSignatureProviderStatusMock.SetupSequence(
                    x => x.IsSignedByAll(
                        It.IsAny<SignTask>()))
                .Returns(true)
                .Returns(false)
                .Returns(true)
                .Returns(true);

            var status = InitStatus();
            status.Poll();

            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.IsAny<UpdateDocumentStatusRequest>()),
                Times.Exactly(7));
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSignTaskCompleteRequest>()),
                Times.Exactly(6));
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Exactly(4));
        }

        [Test]
        public void PollByExternalReference_OneSignTaskStuckInStatusSigningComplete()
        {
            const string externalReference = "123456";
            var signTaskSigningCompleted = TestData.SignTaskSigningCompleted();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByExternalReference(
                        It.Is<string>(
                            s => s == externalReference)))
                .Returns(new List<SignTask> { signTaskSigningCompleted });

            var status = InitStatus();
            var response = status.PollByExternalReference(externalReference);

            Assert.IsTrue(response.IsUpdated);
            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.Is<UpdateDocumentStatusRequest>(
                    u => u.SignTaskId == signTaskSigningCompleted.Id
                         && u.CustomerNumber == signTaskSigningCompleted.Signers[0].CustomerNumber
                         && u.IsPresentedToUser
                         && u.IsSigned
                         && u.Reference == signTaskSigningCompleted.Documents[0].Reference)));
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.Is<SetSignTaskCompleteRequest>(
                    s => s.CustomerNumber == signTaskSigningCompleted.Signers[0].CustomerNumber
                         && s.SignTaskId == signTaskSigningCompleted.Id)),
                Times.Once);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Never);
        }

        [Test]
        public void PollByExternalReference_OneSignTaskStuckInStatusSentToElectronicSignatureProviderButIsSigned()
        {
            const string externalReference = "123456";
            var signTaskSentToElectronicSignatureProvider = TestData.SignTaskSentToElectronicSignatureProvider();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByExternalReference(
                        It.Is<string>(
                            s => s == externalReference)))
                .Returns(new List<SignTask> { signTaskSentToElectronicSignatureProvider });
            _electronicSignatureProviderStatusMock.Setup(
                    x => x.IsSignedByAll(
                        It.Is<SignTask>(st => st.Id == signTaskSentToElectronicSignatureProvider.Id)))
                .Returns(true);

            var status = InitStatus();
            var response = status.PollByExternalReference(externalReference);

            Assert.IsTrue(response.IsUpdated);
            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.Is<UpdateDocumentStatusRequest>(
                    u => u.SignTaskId == signTaskSentToElectronicSignatureProvider.Id
                         && u.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && u.IsPresentedToUser
                         && u.IsSigned
                         && u.Reference == signTaskSentToElectronicSignatureProvider.Documents[0].Reference)),
                Times.Once);
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.Is<SetSignTaskCompleteRequest>(
                    s => s.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && s.SignTaskId == signTaskSentToElectronicSignatureProvider.Id)),
                Times.Once);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.Is<SetSigningStepCompleteRequest>(
                    s => s.CustomerNumber == signTaskSentToElectronicSignatureProvider.Signers[0].CustomerNumber
                         && s.SignTaskId == signTaskSentToElectronicSignatureProvider.Id
                         && s.SigningMethod == SigningMethodEnum.BankIdNorway)),
                Times.Once);
        }

        [Test]
        public void PollByExternalReference_OneSignTaskStuckInStatusSentToElectronicSignatureProviderButIsNotSigned()
        {
            const string externalReference = "123456";
            var signTaskSentToElectronicSignatureProvider = TestData.SignTaskSentToElectronicSignatureProvider();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByExternalReference(
                        It.Is<string>(
                            s => s == externalReference)))
                .Returns(new List<SignTask> { signTaskSentToElectronicSignatureProvider });
            _electronicSignatureProviderStatusMock.Setup(
                    x => x.IsSignedByAll(
                        It.Is<SignTask>(st => st.Id == signTaskSentToElectronicSignatureProvider.Id)))
                .Returns(false);

            var status = InitStatus();
            var response = status.PollByExternalReference(externalReference);

            Assert.IsFalse(response.IsUpdated);
            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.IsAny<UpdateDocumentStatusRequest>()),
                Times.Never);
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSignTaskCompleteRequest>()),
                Times.Never);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Never);
        }

        [Test]
        public void PollByExternalReference_OneSignTaskStuckInStatusSentToElectronicSignatureProviderButIsPartiallySigned()
        {
            const string externalReference = "123456";
            var partiallySignedSignTaskSentToElectronicSignatureProvider = TestData.PartiallySignedSignTaskSentToElectronicSignatureProvider();
            _documentSignRepositoryMock.Setup(
                    x => x.GetSignTasksByExternalReference(
                        It.Is<string>(
                            s => s == externalReference)))
                .Returns(new List<SignTask> { partiallySignedSignTaskSentToElectronicSignatureProvider });
            _electronicSignatureProviderStatusMock.Setup(
                    x => x.IsSignedByAll(
                        It.Is<SignTask>(st => st.Id == partiallySignedSignTaskSentToElectronicSignatureProvider.Id)))
                .Returns(false);
            _eSignatureMock.SetupSequence(
                    x => x.GetAllSigningProcesses(It.IsAny<GetAllSigningProcessesRequest>()))
                .Returns(new GetAllSigningProcessesResponse
                {
                    SigningProcessResults = new List<SigningProcessResult>
                    {
                        new SigningProcessResult
                        {
                            CustomerNumber = partiallySignedSignTaskSentToElectronicSignatureProvider.Signers[0]
                                .CustomerNumber,
                            DocumentDetails = new DocumentDetails
                            {
                                DocumentStatus = GetOrderStatusResponseDocumentStatusesDocumentStatusStatusDTO.Complete,
                                Reference = partiallySignedSignTaskSentToElectronicSignatureProvider.Documents[0]
                                    .Reference
                            }
                        }
                    }
                })
                .Returns(new GetAllSigningProcessesResponse
                {
                    SigningProcessResults = new List<SigningProcessResult>
                    {
                        new SigningProcessResult
                        {
                            CustomerNumber = partiallySignedSignTaskSentToElectronicSignatureProvider.Signers[1]
                                .CustomerNumber,
                            DocumentDetails = new DocumentDetails
                            {
                                DocumentStatus = GetOrderStatusResponseDocumentStatusesDocumentStatusStatusDTO.Active,
                                Reference = partiallySignedSignTaskSentToElectronicSignatureProvider.Documents[0]
                                    .Reference
                            }
                        }
                    }
                });
            _documentStatusMock.SetupSequence(
                    x => x.Get(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new DocumentStatus
                {
                    SignTaskDocumentStatus = new SignTaskDocumentStatusLog
                    {
                        IsPresentedToUser = true
                    }
                })
                .Returns(new DocumentStatus
                {
                    SignTaskDocumentStatus = new SignTaskDocumentStatusLog
                    {
                        IsPresentedToUser = false
                    }
                });

            var status = InitStatus();
            var response = status.PollByExternalReference(externalReference);

            Assert.IsTrue(response.IsUpdated);
            _updateDocumentStatusMock.Verify(
                x => x.Execute(It.IsAny<UpdateDocumentStatusRequest>()),
                Times.Once);
            _setSignTaskCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSignTaskCompleteRequest>()),
                Times.Once);
            _setSigningStepCompleteMock.Verify(
                x => x.Execute(It.IsAny<SetSigningStepCompleteRequest>()),
                Times.Once);
        }
    }
}
