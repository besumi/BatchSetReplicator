using kCura.Relativity.Client;
using Relativity.Services.ServiceProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using DTOs = kCura.Relativity.Client.DTOs;

namespace ConsoleUI
{
    class Program
    {
        static string _rsapiUrl = "https://blah.hopper.relativity.com";
        static string _rsapiUsername = "a@b.c";
        static string _rsapiPassword = "passwordHere1234!";
        static string _templateCase = "Folder Template";
        static string _targetCase = "Folder Target";
        static int _templateArtifactId = 1016890;
        static int _targetArtifactId = 1025440;

        static void Main(string[] args)
        {

            Relativity.Services.ServiceProxy.ServiceFactorySettings settings = new Relativity.Services.ServiceProxy.ServiceFactorySettings(
                                                              new Uri(_rsapiUrl + "/relativity.services/"),
                                                              new Uri(_rsapiUrl + "/relativity.rest/api"),
                                                              new Relativity.Services.ServiceProxy.UsernamePasswordCredentials(_rsapiUsername, _rsapiPassword));

            DuplicateBatches(settings);


            // exit
            Console.WriteLine("\r\nDone.");
            Console.Read();
        }

        private static void DuplicateBatches(ServiceFactorySettings settings)
        {
            try
            {
                using (IRSAPIClient rsapiProxy = new Relativity.Services.ServiceProxy.ServiceFactory(settings).CreateProxy<IRSAPIClient>())
                {
                    rsapiProxy.APIOptions.WorkspaceID = _templateArtifactId;

                    List<DTOs.Result<DTOs.BatchSet>> source = GetSourceBatches(rsapiProxy);

                    if (source == null)
                        return;
                    else if (source.Count < 1)
                    {
                        Console.WriteLine("Template workspace has no folders; exiting.");
                        return;
                    }

                    rsapiProxy.APIOptions.WorkspaceID = _targetArtifactId;

                    CreateBatches(source, rsapiProxy);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception encountered:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
            }
        }

        private static void CreateBatches(List<DTOs.Result<DTOs.BatchSet>> batches, IRSAPIClient proxy)
        {
            foreach (var batch in batches)
            {
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
                    Console.WriteLine(string.Format("An error occurred: {0}", ex.Message));
                }

                if (results.Success)
                {
                    Console.WriteLine(string.Format("ID of the new Batch Set: {0}", results.Results[0].Artifact.ArtifactID));
                }
                else
                {
                    Console.WriteLine($"Batch Set creation was unsuccessful.");
                    Console.WriteLine($"{results.Results[0].Message}");
                }
            }
        }

        private static List<DTOs.Result<DTOs.BatchSet>> GetSourceBatches(IRSAPIClient proxy)
        {
            // build the query / condition
            DTOs.Query<DTOs.BatchSet> query = new DTOs.Query<DTOs.BatchSet>();
            query.Fields = DTOs.FieldValue.AllFields;

            // query for the folders
            DTOs.QueryResultSet<DTOs.BatchSet> resultSet = new DTOs.QueryResultSet<DTOs.BatchSet>();
            try
            {
                resultSet = proxy.Repositories.BatchSet.Query(query, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
                return null;
            }

            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    Console.WriteLine(String.Format("{0} batch sets found in {1}.\r\n", resultSet.Results.Count, _templateArtifactId));
                    return resultSet.Results;
                }
                else
                {
                    Console.WriteLine("Query was successful but no batches exist.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Query was not successful.");
                return null;
            }
        }

        static DTOs.Workspace FindWorkspace(string name, IRSAPIClient client)
        {
            client.APIOptions.WorkspaceID = -1;

            //build the query / condition
            DTOs.Query<DTOs.Workspace> query = new DTOs.Query<DTOs.Workspace>
            {
                Condition = new TextCondition(DTOs.WorkspaceFieldNames.Name, TextConditionEnum.EqualTo, name),
                Fields = DTOs.FieldValue.AllFields
            };

            // query for the workspace
            DTOs.QueryResultSet<DTOs.Workspace> resultSet = new DTOs.QueryResultSet<DTOs.Workspace>();
            try
            {
                resultSet = client.Repositories.Workspace.Query(query, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(String.Format("Exception:\r\n{0}\r\n{1}", ex.Message, ex.InnerException));
                return null;
            }

            // check for success
            if (resultSet.Success)
            {
                if (resultSet.Results.Count > 0)
                {
                    DTOs.Workspace firstWorkspace = resultSet.Results.FirstOrDefault().Artifact;
                    Console.WriteLine(String.Format("Workspace found with artifactID {0}.", firstWorkspace.ArtifactID));
                    return firstWorkspace;
                }
                else
                {
                    Console.WriteLine("Query was successful but workspace does not exist.");
                    return null;
                }
            }
            else
            {
                Console.WriteLine("Query was not successful.");
                return null;
            }
        }
    }
}
