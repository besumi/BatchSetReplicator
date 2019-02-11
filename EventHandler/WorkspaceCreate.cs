using kCura.EventHandler;
using kCura.Relativity.Client;
using Relativity.API;
using System;
using System.Collections.Generic;
using DTOs = kCura.Relativity.Client.DTOs;

namespace EventHandler
{
    [kCura.EventHandler.CustomAttributes.Description("Batch Set Replicator Post-Workspace Create")]
    [kCura.EventHandler.CustomAttributes.RunOnce(false)]
    [System.Runtime.InteropServices.Guid("51E30A8F-937B-46E8-8EF6-C1D9A5567E24")]
    class WorkspaceCreate : PostWorkspaceCreateEventHandlerBase
    {
        private static IAPILog _logger;

        public override Response Execute()
        {
            Response retVal = new Response()
            {
                Success = true,
                Message = String.Empty
            };

            _logger = Helper.GetLoggerFactory().GetLogger().ForContext<WorkspaceCreate>();

            try
            {
                int currentWorkspaceID = Helper.GetActiveCaseID();

                IDBContext workspaceDBContext = Helper.GetDBContext(currentWorkspaceID);

                int templateWorkspaceID = GetTemplateCase(workspaceDBContext);

                using (IRSAPIClient proxy = Helper.GetServicesManager().CreateProxy<IRSAPIClient>(ExecutionIdentity.System))
                {
                    proxy.APIOptions.WorkspaceID = templateWorkspaceID;

                    List<DTOs.Result<DTOs.BatchSet>> source = GetSourceBatches(proxy);

                    if (source != null)
                    {
                        if (source.Count < 1)
                        {
                            _logger.LogInformation("Template workspace has no batch sets; exiting.");
                        }
                        else
                        {
                            _logger.LogInformation("Starting creation of {number} batches", source.Count);
                            proxy.APIOptions.WorkspaceID = currentWorkspaceID;

                            CreateBatches(source, proxy);
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                retVal.Success = false;
                retVal.Message = ex.ToString();
            }

            return retVal;
        }

        private static int GetTemplateCase(IDBContext context)
        {
            string getTemplate = @"
                SELECT TOP 1 [CaseTemplateID]
                FROM [CaseEventHandlerHistory]
                WHERE [CaseTemplateID] IS NOT NULL
            ";

            int result = context.ExecuteSqlStatementAsScalar<int>(getTemplate);

            return result;
        }

        private static List<DTOs.Result<DTOs.BatchSet>> GetSourceBatches(IRSAPIClient proxy)
        {
            DTOs.Query<DTOs.BatchSet> query = new DTOs.Query<DTOs.BatchSet>();
            query.Fields = DTOs.FieldValue.AllFields;

            DTOs.QueryResultSet<DTOs.BatchSet> resultSet = new DTOs.QueryResultSet<DTOs.BatchSet>();
            try
            {
                resultSet = proxy.Repositories.BatchSet.Query(query, 0);
            }
            catch (Exception ex)
            {
                _logger.LogError("Exception when querying for batches: {message}", ex);
                return null;
            }

            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    _logger.LogInformation("{0} batch sets found in {1}", resultSet.Results.Count, proxy.APIOptions.WorkspaceID);
                    return resultSet.Results;
                }
                else
                {
                    _logger.LogWarning("Query was successful but no batches exist.");
                    return null;
                }
            }
            else
            {
                _logger.LogError("Unsuccessful query for batches: {message}", resultSet.Results[0].Message);
                return null;
            }
        }

        private static void CreateBatches(List<DTOs.Result<DTOs.BatchSet>> batches, IRSAPIClient proxy)
        {
            foreach (var batch in batches)
            {
                // i have to create a new batchset from the batchset DTO; no idea why
                var newBatch = new DTOs.BatchSet()
                {
                    Name = batch.Artifact.Name,
                    BatchPrefix = batch.Artifact.BatchPrefix,
                    AutoBatch = batch.Artifact.AutoBatch,
                    BatchDataSource = batch.Artifact.BatchDataSource,
                    BatchUnitField = batch.Artifact.BatchUnitField,
                    FamilyField = batch.Artifact.FamilyField,
                    ReviewedField = batch.Artifact.ReviewedField,
                    MaximumBatchSize = batch.Artifact.MaximumBatchSize
                };

                if (batch.Artifact.AutoBatch == true)
                {
                    newBatch.AutoCreateRateMinutes = batch.Artifact.AutoCreateRateMinutes;
                    newBatch.MinimumBatchSize = batch.Artifact.MinimumBatchSize;
                }

                DTOs.WriteResultSet<DTOs.BatchSet> results = new DTOs.WriteResultSet<DTOs.BatchSet>();

                try
                {
                    results = proxy.Repositories.BatchSet.Create(newBatch);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Exception when querying for batches: {message}", ex);
                }

                if (results.Success)
                {
                    _logger.LogInformation("New batch set: {ArtifactID}", results.Results[0].Artifact.ArtifactID);
                }
                else
                {
                    _logger.LogError("Batch Set creation unsuccessful: {message}", results.Results[0].Message);
                }
            }
        }
    }
}
