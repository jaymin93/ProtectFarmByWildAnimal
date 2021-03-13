﻿// <snippet_imports>
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training;
using Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
// </snippet_imports>

/* NOTE After compiling this program, but before running it, do the following.
 * 1. Download the contents of https://github.com/Azure-Samples/cognitive-services-sample-data-files/tree/master/CustomVision/ImageClassification. The simplest way to do this is to visit https://github.com/Azure-Samples/cognitive-services-sample-data-files and download the repo as a .zip file, then extract the contents of the CustomVision/ImageClassification folder.
 * 2. Copy the Images folder to the output folder of your Visual Studio project. For example, if your project targets .NET Core 3.1, copy the Images folder to the /bin/Debug/netcoreapp3.1 folder.
 */

namespace ImageClassification
{
    class Program
    {
        // <snippet_creds>
        // You can obtain these values from the Keys and Endpoint page for your Custom Vision resource in the Azure Portal.
        private static string trainingEndpoint = "https://eastus.api.cognitive.microsoft.com/";
        private static string trainingKey = "5a12edb8904048d6904dc8384af8730d";
        // You can obtain these values from the Keys and Endpoint page for your Custom Vision Prediction resource in the Azure Portal.
        private static string predictionEndpoint = "https://eastus.api.cognitive.microsoft.com/";
        private static string predictionKey = "5a12edb8904048d6904dc8384af8730d";
        // You can obtain this value from the Properties page for your Custom Vision Prediction resource in the Azure Portal. See the "Resource ID" field. This typically has a value such as:
        // /subscriptions/<your subscription ID>/resourceGroups/<your resource group>/providers/Microsoft.CognitiveServices/accounts/<your Custom Vision prediction resource name>
        private static string predictionResourceId = "/subscriptions/c4777c65-857b-42dc-8cf9-7c2c89c33003/resourceGroups/comeyecv/providers/Microsoft.CognitiveServices/accounts/helpfarmers";

        private static List<string> man;
        private static List<string> monkey;
        private static Tag mantag;
        private static Tag monkeytag;
        private static Iteration iteration;
        private static string publishedModelName = "treeClassModel";
        private static MemoryStream testImage;
        // </snippet_creds>

        static void Main(string[] args)
        {
            // <snippet_maincalls>
            CustomVisionTrainingClient trainingApi = AuthenticateTraining(trainingEndpoint, trainingKey);
            CustomVisionPredictionClient predictionApi = AuthenticatePrediction(predictionEndpoint, predictionKey);

            Project project = CreateProject(trainingApi);
            //AddTags(trainingApi, project);
            //UploadImages(trainingApi, project);
            //TrainProject(trainingApi, project);
            //PublishIteration(trainingApi, project);
            //sTestIteration(predictionApi, project);
            //DeleteProject(trainingApi, project);
            // </snippet_maincalls>
        }

        // <snippet_auth>
        private static CustomVisionTrainingClient AuthenticateTraining(string endpoint, string trainingKey)
        {
            // Create the Api, passing in the training key
            CustomVisionTrainingClient trainingApi = new CustomVisionTrainingClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Training.ApiKeyServiceClientCredentials(trainingKey))
            {
                Endpoint = endpoint
            };
            return trainingApi;
        }
        private static CustomVisionPredictionClient AuthenticatePrediction(string endpoint, string predictionKey)
        {
            // Create a prediction endpoint, passing in the obtained prediction key
            CustomVisionPredictionClient predictionApi = new CustomVisionPredictionClient(new Microsoft.Azure.CognitiveServices.Vision.CustomVision.Prediction.ApiKeyServiceClientCredentials(predictionKey))
            {
                Endpoint = endpoint
            };
            return predictionApi;
        }
        // </snippet_auth>

        // <snippet_create>
        private static Project CreateProject(CustomVisionTrainingClient trainingApi)
        {
            // Create a new project
            Console.WriteLine("Creating new project:");
            return trainingApi.GetProject(Guid.Parse("0b122e5a-03c6-4f27-a21e-66f3e2572d3b"));
        }
        // </snippet_create>
        // <snippet_addtags>
        private static void AddTags(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Make two tags in the new project
            mantag = trainingApi.CreateTag(project.Id, "man");
            monkeytag = trainingApi.CreateTag(project.Id, "monkey");
        }
        // </snippet_addtags>

        // <snippet_upload>
        private static void UploadImages(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Add some images to the tags
            Console.WriteLine("\tUploading images");
            LoadImagesFromDisk();

            // Images can be uploaded one at a time
            foreach (var image in man)
            {
                using (var stream = new MemoryStream(File.ReadAllBytes(image)))
                {
                    trainingApi.CreateImagesFromData(project.Id, stream, new List<Guid>() { mantag.Id });
                }
            }

            // Or uploaded in a single batch 
            var imageFiles = monkey.Select(img => new ImageFileCreateEntry(Path.GetFileName(img), File.ReadAllBytes(img))).ToList();
            trainingApi.CreateImagesFromFiles(project.Id, new ImageFileCreateBatch(imageFiles, new List<Guid>() { monkeytag.Id }));

        }
        // </snippet_upload>

        // <snippet_train>
        private static void TrainProject(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Now there are images with tags start training the project
            Console.WriteLine("\tTraining");
            iteration = trainingApi.TrainProject(project.Id);

            // The returned iteration will be in progress, and can be queried periodically to see when it has completed
            while (iteration.Status == "Training")
            {
                Console.WriteLine("Waiting 10 seconds for training to complete...");
                Thread.Sleep(10000);

                // Re-query the iteration to get it's updated status
                iteration = trainingApi.GetIteration(project.Id, iteration.Id);
            }
        }
        // </snippet_train>

        // <snippet_publish>
        private static void PublishIteration(CustomVisionTrainingClient trainingApi, Project project)
        {
            trainingApi.PublishIteration(project.Id, iteration.Id, publishedModelName, predictionResourceId);
            Console.WriteLine("Done!\n");

            // Now there is a trained endpoint, it can be used to make a prediction
        }
        // </snippet_publish>

        // <snippet_test>
        private static void TestIteration(CustomVisionPredictionClient predictionApi, Project project)
        {

            LoadImagesFromDisk();
            // Make a prediction against the new project
            Console.WriteLine("Making a prediction:");
            var result = predictionApi.ClassifyImage(project.Id, publishedModelName, testImage);

            // Loop over each prediction and write out the results
            foreach (var c in result.Predictions)
            {
                Console.WriteLine($"\t{c.TagName}: {c.Probability:P1}");
            }
        }
        // </snippet_test>

        // <snippet_loadimages>
        private static void LoadImagesFromDisk()
        {
            // this loads the images to be uploaded from disk into memory
            man = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "man in farm")).ToList();
            monkey = Directory.GetFiles(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "monkey image")).ToList();
            testImage = new MemoryStream(File.ReadAllBytes(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "humaninfarm.jpeg")));
        }
        // </snippet_loadimages>
        // <snippet_delete>
        private static void DeleteProject(CustomVisionTrainingClient trainingApi, Project project)
        {
            // Delete project. Note you cannot delete a project with a published iteration; you must unpublish the iteration first.
            Console.WriteLine("Unpublishing iteration.");
            trainingApi.UnpublishIteration(project.Id, iteration.Id);
            Console.WriteLine("Deleting project.");
            trainingApi.DeleteProject(project.Id);
        }
        // </snippet_create>
    }
}
