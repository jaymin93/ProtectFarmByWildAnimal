using System;
using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System.Net.Http;

namespace IdentifyManOrMonkeyCustomVision
{
    public static class ManOrMonkeyFunc
    {

        private static string trainingEndpoint = "https://eastus.api.cognitive.microsoft.com/";
        private static string trainingKey = "5a12edb8904048d6904dc8384af8730d";

        private static string predictionEndpoint = "https://eastus.api.cognitive.microsoft.com/";
        private static string predictionKey = "5a12edb8904048d6904dc8384af8730d";


        private static string publishedModelName = "Iteration1";

        private static string StorageAccountURIWithConatinerName = "https://deafaid.blob.core.windows.net/manormonkey/";

        private static string ProjectGUID = "0b122e5a-03c6-4f27-a21e-66f3e2572d3b";

        private static string ManTagname = "man";

        private static string MonkeyTagname = "monkey";

        private static string TableName = "helpfarmer";

        private static string FuncUrl = "helpfarmer";

        private static TimeZoneInfo INDIAN_ZONE = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");



        [FunctionName("ManOrMonkeyFunc")]
        public async static Task Run([BlobTrigger("manormonkey/{name}", Connection = "AzureWebJobsStorage")] Stream myBlob, string name, ILogger log)
        {

            CustomVisionTrainingClient trainingApi = AuthTraining(trainingEndpoint, trainingKey);
            CustomVisionPredictionClient predictionApi = AuthPrediction(predictionEndpoint, predictionKey);

            Project project = GetExistingProject(trainingApi);

            var response = await TestManORMonkeyPrediction(predictionApi, project, $"{StorageAccountURIWithConatinerName}{name}");

            if (response.monkey > response.man)
            {
                await InsertIncidentgDeatilsTOAzureTable($"Please review recent image looks like monkeys are entering into the farm probability is {response.monkey:P1}", $"{StorageAccountURIWithConatinerName}{name}", log);
            }

        }




        public static async Task<bool> InsertIncidentgDeatilsTOAzureTable(string message, string imageurl, ILogger log)
        {
            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse("DefaultEndpointsProtocol=https;AccountName=deafaid;AccountKey=ch8fbsZmxgSbFGfLw5lGNXgGlRUBN+Actts2M09bIUgomisMHySQv7xuiVDbj5k//BwpVF7V6TMGniOkhFn17Q==;EndpointSuffix=core.windows.net");

                CloudTableClient tableClient = storageAccount.CreateCloudTableClient();

                CloudTable table = tableClient.GetTableReference(TableName);

                //can be used in situation if you are not sure table exist , in my i created table so i do not need to check this
                //await table.CreateIfNotExistsAsync();

                ManorMonkeyDeatails details;


                details = new ManorMonkeyDeatails($"{TableName}", $"{TableName}{DateTime.Now:dd-MM-yyyy-HH-mm-ss}");

                details.IncidentTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, INDIAN_ZONE);

                details.Message = message;
                details.ImageURL = imageurl;

                TableOperation insertOperation = TableOperation.Insert(details);

                var insertoperationresult = await table.ExecuteAsync(insertOperation);

                var sts = insertoperationresult.HttpStatusCode;

                return true;
            }
            catch (Exception ex)
            {
                log.LogError(ex.ToString());
                return default;
            }

        }



        private static CustomVisionTrainingClient AuthTraining(string endpoint, string trainingKey)
        {

            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(trainingKey))
            {
                Endpoint = endpoint
            };
            return trainingApi;
        }
        private static CustomVisionPredictionClient AuthPrediction(string endpoint, string predictionKey)
        {

            CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(predictionKey))
            {
                Endpoint = endpoint
            };
            return predictionApi;
        }


        private static Project GetExistingProject(CustomVisionTrainingClient trainingApi)
        {
            return trainingApi.GetProject(Guid.Parse(ProjectGUID));
        }

        private async static Task<(double man, double monkey)> TestManORMonkeyPrediction(CustomVisionPredictionClient predictionApi, Project project, string bloburi)
        {

            Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl imageUrl = new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.Models.ImageUrl(bloburi);
            var result = await predictionApi.ClassifyImageUrlAsync(project.Id, publishedModelName, imageUrl);

            double manprob = result.Predictions.Where(x => x.TagName == ManTagname).FirstOrDefault().Probability;

            double monkeyprob = result.Predictions.Where(x => x.TagName == MonkeyTagname).FirstOrDefault().Probability;

            return (manprob, monkeyprob);

        }


        private async static void UpdateDataAsync(string funcurlx, ILogger log, bool playlionroar = true)
        {
            HttpClient httpClient = new HttpClient();

            var response = await httpClient.GetAsync($"{FuncUrl}&{nameof(playlionroar)}={playlionroar}");

            if (!response.IsSuccessStatusCode)
            {
                log.LogError(response.Content.ToString());
            }

        }

    }



    public class ManorMonkeyDeatails : TableEntity
    {
        public ManorMonkeyDeatails()
        {

        }
        public ManorMonkeyDeatails(string skey, string srow)
        {
            this.PartitionKey = skey;
            this.RowKey = srow;
        }
        public DateTime IncidentTime { get; set; }

        public string Message { get; set; }

        public string ImageURL { get; set; }

        public string SoundPlayingStatus { get; set; }

    }
}

