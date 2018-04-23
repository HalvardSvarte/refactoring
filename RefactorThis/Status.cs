using System;
using System.Linq;
using DoNotRefactor;
using log4net;

namespace RefactorThis
{
    public class Status : IStatus
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(Status));

        private readonly IDocumentSignRepository _repository;
        private readonly ISetSignTaskComplete _setSignTaskComplete;
        private readonly DocumentHubSettings _documentHubSettings;
        private readonly ISetSigningStepComplete _setSigningStepComplete;
        private readonly IElectronicSignatureProviderStatus _electronicSignatureProviderStatus;
        private readonly IUpdateDocumentStatus _updateDocumentStatus;
        private readonly IESignature _eSignature;
        private readonly IDocumentStatus _documentStatus;

        public Status(
            IDocumentSignRepository repository,
            ISetSignTaskComplete setSignTaskComplete,
            DocumentHubSettings documentHubSettings,
            ISetSigningStepComplete setSigningStepComplete,
            IElectronicSignatureProviderStatus electronicSignatureProviderStatus,
            IUpdateDocumentStatus updateDocumentStatus,
            IESignature eSignature,
            IDocumentStatus documentStatus)
        {
            _repository = repository;
            _setSignTaskComplete = setSignTaskComplete;
            _documentHubSettings = documentHubSettings;
            _setSigningStepComplete = setSigningStepComplete;
            _electronicSignatureProviderStatus = electronicSignatureProviderStatus;
            _updateDocumentStatus = updateDocumentStatus;
            _eSignature = eSignature;
            _documentStatus = documentStatus;
        }

        public void Poll()
        {
            Logger.Debug("Polling status started ...");
            try
            {
                Logger.Debug("Checking for sign tasks stuck in status SigningCompleted ...");
                var signTasks = _repository.GetSignTasksByStatusCode(SignTaskStatusCode.SigningCompleted);
                Logger.Debug($"Found {signTasks.Count} sign tasks that needs to be completed");
                foreach (var signTask in signTasks)
                {
                    try
                    {
                        SetSignTaskCompleteFromStatusSigningComplete(signTask);
                    }
                    catch (Exception exception)
                    {
                        Logger.Warn($"Error on setting sign task with id = {signTask.Id} to complete from status signing complete.  Exception was: {exception}");
                        // Ignoring exception to continue loop.
                    }

                }
                Logger.Debug("Finished checking and updating sign tasks stuck in status SigningCompleted");

                Logger.Debug("Checking for sign tasks stuck in status SentToElectronicSignatureProvider ...");
                var cutoffDate = DateTime.Now.AddDays(-_documentHubSettings.NumDaysBackForStatusPoller);
                signTasks = _repository.GetSignTasksByStatusCode(SignTaskStatusCode.SentToElectronicSignatureProvider, cutoffDate);
                Logger.Debug($"Found {signTasks.Count} sign tasks that needs to be completed");
                foreach (var signTask in signTasks)
                {
                    try
                    {
                        var isSignedByAll = _electronicSignatureProviderStatus.IsSignedByAll(signTask);
                        if (!isSignedByAll)
                        {
                            Logger.Debug(
                                $"Sign task with id = {signTask.Id} has not been signed by all signers.");
                            PossiblyPartiallyCompleteSignTask(signTask);
                            continue;
                        }

                        SetSignTaskCompleteFromStatusSentToElectronicSignatureProvider(signTask);
                    }
                    catch (Exception exception)
                    {
                        Logger.Warn($"Error on setting sign task with id = {signTask.Id} to complete from status sent to electronic signature provider.  Exception was: {exception}");
                        // Ignoring exception to continue loop.
                    }
                }
                Logger.Debug("Finished checking and updating sign tasks stuck in status SentToElectronicSignatureProvider");
            }
            catch (Exception exception)
            {
                Logger.Error($"Error on polling status, exception was: {exception}");
                throw;
            }
            finally
            {
                Logger.Debug("Polling status ended");
            }
        }

        public PollByExternalReferenceResponse PollByExternalReference(string externalReference)
        {
            Logger.Debug($"Polling sign task with external reference = {externalReference} ...");
            bool isUpdated = false;
            try
            {
                var signTasks = _repository.GetSignTasksByExternalReference(externalReference);
                signTasks = signTasks.Where(x => x.ApplicationData != null
                                                           && x.StatusCode != SignTaskStatusCode.Deactivated
                                                           && x.StatusCode != SignTaskStatusCode.Archived
                                                           && x.StatusCode != SignTaskStatusCode.Rejected).ToList();
                
                Logger.Debug($"Found {signTasks.Count} active sign tasks with external reference = {externalReference}");
                foreach (var signTask in signTasks)
                {
                    try
                    {
                        Logger.Debug(
                            $"Checking sign task with id = {signTask.Id} and external reference = {externalReference}, which is in status = {signTask.StatusCode}");
                        if (signTask.StatusCode == SignTaskStatusCode.SigningCompleted)
                        {
                            Logger.Debug(
                                $"Sign task with id = {signTask.Id} and external reference = {externalReference} is in status signing completed.");
                            SetSignTaskCompleteFromStatusSigningComplete(signTask);
                            isUpdated = true;
                        }
                        else if (signTask.StatusCode == SignTaskStatusCode.SentToElectronicSignatureProvider)
                        {
                            Logger.Debug(
                                $"Sign task with id = {signTask.Id} and external reference = {externalReference} is in status sent to electronic signature provider.");
                            var isSignedByAll = _electronicSignatureProviderStatus.IsSignedByAll(signTask);
                            if (!isSignedByAll)
                            {
                                Logger.Debug(
                                    $"Sign task with id = {signTask.Id} and external reference = {externalReference} has not been signed by all signers.");
                                var isChanged = PossiblyPartiallyCompleteSignTask(signTask);
                                if (isChanged)
                                    isUpdated = true;
                                continue;
                            }
                            SetSignTaskCompleteFromStatusSentToElectronicSignatureProvider(signTask);
                            isUpdated = true;
                        }
                        else
                        {
                            Logger.Debug(
                                $"Sign task with id = {signTask.Id} and external reference = {externalReference} was not changed because it was in status = {signTask.StatusCode}");
                        }
                    }
                    catch (Exception exception)
                    {
                        Logger.Warn($"Error on checking sign task with id = {signTask.Id} and external reference = {externalReference}, exception was: {exception}");
                        // Ignoring exception to continue loop.
                    }
                }
            }
            catch (Exception exception)
            {
                Logger.Error($"Error on polling sign task with external reference = {externalReference}, exception was: {exception}");
                throw;
            }
            return new PollByExternalReferenceResponse { IsUpdated = isUpdated };
        }

        private bool PossiblyPartiallyCompleteSignTask(SignTask signTask)
        {
            var isChanged = false;
            foreach (var signer in signTask.Signers)
            {
                var allSigningProcesses = _eSignature.GetAllSigningProcesses(new GetAllSigningProcessesRequest
                {
                    OrderId = signTask.OrderId,
                    CustomerNumber = signer.CustomerNumber
                });
                if (allSigningProcesses?.SigningProcessResults != null)
                {
                    foreach (var result in allSigningProcesses.SigningProcessResults)
                    {
                        if (result.DocumentDetails.DocumentStatus ==
                            GetOrderStatusResponseDocumentStatusesDocumentStatusStatusDTO.Complete)
                        {
                            _updateDocumentStatus.Execute(new UpdateDocumentStatusRequest
                            {
                                SignTaskId = signTask.Id,
                                Reference = result.DocumentDetails.Reference,
                                CustomerNumber = result.CustomerNumber,
                                IsSigned = true,
                                IsPresentedToUser = true
                            });
                            isChanged = true;
                        }
                    }
                }
                else
                {
                    Logger.Debug($"Unable to update document status on sign task with id = {signTask.Id} because no signing process information was retrieved");
                }

                var presentedCount = 0;
                var presentableDocuments =
                    signTask.Documents.Where(x => x.SignTaskDocumentType != SignTaskDocumentType.CombinedDocument).ToList();
                foreach (var document in presentableDocuments)
                {
                    var status = _documentStatus.Get(signTask.Id, document.Reference, signer.CustomerNumber);
                    if (status?.SignTaskDocumentStatus == null) continue;
                    if (status.SignTaskDocumentStatus.IsPresentedToUser)
                        presentedCount++;
                }
                if (presentedCount == presentableDocuments.Count)
                {
                    Logger.Debug($"Signer with id = {signer.Id} has read and signed all documents in sign task with id = {signTask.Id}");
                    _setSigningStepComplete.Execute(new SetSigningStepCompleteRequest
                    {
                        SignTaskId = signTask.Id,
                        CustomerNumber = signer.CustomerNumber,
                        SigningMethod = SigningMethodEnum.BankIdNorway
                    });
                    _setSignTaskComplete.Execute(new SetSignTaskCompleteRequest
                    {
                        SignTaskId = signTask.Id,
                        CustomerNumber = signer.CustomerNumber
                    });
                    isChanged = true;
                }
                else
                {
                    Logger.Debug($"Signer with id = {signer.Id} has not completed sign task with id = {signTask.Id}");
                }
            }
            return isChanged;
        }

        private void SetSignTaskCompleteFromStatusSigningComplete(SignTask signTask)
        {
            foreach (var signer in signTask.Signers)
            {
                Logger.DebugFormat("Completing sign task with id = {0} for customer with id = {1}", signTask.Id, signer.Id);
                foreach (var document in signTask.Documents)
                {
                    if (document.IsElectronicSignatureAllowed)
                    {
                        _updateDocumentStatus.Execute(new UpdateDocumentStatusRequest
                        {
                            SignTaskId = signTask.Id,
                            Reference = document.Reference,
                            CustomerNumber = signer.CustomerNumber,
                            IsSigned = true,
                            IsPresentedToUser = true
                        });
                    }
                }
                _setSignTaskComplete.Execute(new SetSignTaskCompleteRequest
                {
                    CustomerNumber = signer.CustomerNumber,
                    SignTaskId = signTask.Id
                });
                Logger.DebugFormat("Completed sign task with id = {0} for customer with id = {1}", signTask.Id, signer.Id);
            }
        }

        private void SetSignTaskCompleteFromStatusSentToElectronicSignatureProvider(SignTask signTask)
        {
            foreach (var signer in signTask.Signers)
            {
                Logger.DebugFormat("Completing sign task with id = {0} for customer with id = {1}", signTask.Id, signer.Id);
                foreach (var document in signTask.Documents)
                {
                    if (document.IsElectronicSignatureAllowed)
                    {
                        _updateDocumentStatus.Execute(new UpdateDocumentStatusRequest
                        {
                            SignTaskId = signTask.Id,
                            Reference = document.Reference,
                            CustomerNumber = signer.CustomerNumber,
                            IsSigned = true,
                            IsPresentedToUser = true
                        });
                    }
                }
                _setSigningStepComplete.Execute(new SetSigningStepCompleteRequest
                {
                    CustomerNumber = signer.CustomerNumber,
                    SignTaskId = signTask.Id,
                    SigningMethod = SigningMethodEnum.BankIdNorway
                });
                _setSignTaskComplete.Execute(new SetSignTaskCompleteRequest
                {
                    CustomerNumber = signer.CustomerNumber,
                    SignTaskId = signTask.Id
                });
                Logger.DebugFormat("Completed sign task with id = {0} for customer with id = {1}", signTask.Id, signer.Id);
            }
        }
    }
}
