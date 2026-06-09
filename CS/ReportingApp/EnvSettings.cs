using System;

namespace ReportingApp {
    public static class EnvSettings {
        public static string AzureOpenAIEndpoint { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT"); } }
        public static string AzureOpenAIKey { get { return Environment.GetEnvironmentVariable("AZURE_OPENAI_APIKEY"); } }
        public static string DeploymentName { get { return "gpt-5.4"; } }
    }
}
