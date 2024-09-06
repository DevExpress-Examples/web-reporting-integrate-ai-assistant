using System;

namespace ReportingApp {
    public static class EnvSettings {
        public static string AzureOpenAIEndpoint { get { return Environment.GetEnvironmentVariable("OPENAI_ENDPOINT"); } }
        public static string AzureOpenAIKey { get { return Environment.GetEnvironmentVariable("OPENAI_APIKEY"); } }
        public static string DeploymentName { get { return "GPT4o"; } }
    }
}
