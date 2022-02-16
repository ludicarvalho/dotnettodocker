using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DOTNETTODOCKER.App
{
    internal class Program
    {
        static void Main()
        {
            try
            {
                var csProjFilePath = ObterString("Digite o caminho do csproj: ");

                if (!File.Exists(csProjFilePath))
                    throw new FileNotFoundException($"Arquivo não encontrado em '{csProjFilePath}'.");

                var linhas = File.ReadAllText(csProjFilePath);

                var linhasCsProj = linhas.Split('\n')
                    .Where((l) => l.Contains(".csproj") && !l.ToLower().Contains("test"))
                    .Select((l) => l.Split(',').FirstOrDefault((a) => a.Contains(".csproj")).Replace("\"", string.Empty).Trim())
                    .ToList();

                var dockeFilePath = csProjFilePath.Replace($"\\{csProjFilePath.Split('\\')[^1]}", string.Empty);

                var programFilePath = Directory.GetFiles(dockeFilePath, "Program.cs", SearchOption.AllDirectories);

                var projetoPrincipalNome = programFilePath
                    .FirstOrDefault()
                    .Replace("\\" + programFilePath.FirstOrDefault().Split('\\')[^1], string.Empty)
                    .Split('\\')[^1];

                var versaoSDK = File.ReadAllText($"{dockeFilePath}\\{projetoPrincipalNome}\\{projetoPrincipalNome}.csproj")
                    .Split('\n')
                    .Where((l) => l.Contains("TargetFramework"))
                    .Select((l) => l
                        .Replace("<TargetFramework>", string.Empty)
                        .Replace("</TargetFramework>", string.Empty)
                        .Replace("net", string.Empty)
                        .Replace("\r", string.Empty)
                        .Trim())
                    .FirstOrDefault();

                #region Dockerfile

                var streamWriterDockerFile = new StreamWriter(dockeFilePath + "\\Dockerfile", false);

                streamWriterDockerFile.WriteLine("#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.");
                streamWriterDockerFile.WriteLine($"FROM mcr.microsoft.com/dotnet/aspnet:{versaoSDK} AS base");
                streamWriterDockerFile.WriteLine("WORKDIR /app");
                streamWriterDockerFile.WriteLine("EXPOSE 80");

                streamWriterDockerFile.WriteLine("");

                streamWriterDockerFile.WriteLine($"FROM mcr.microsoft.com/dotnet/sdk:{versaoSDK} AS build");
                streamWriterDockerFile.WriteLine("WORKDIR /src");

                foreach (var item in linhasCsProj)
                {
                    streamWriterDockerFile.WriteLine($"COPY [\"{item.Replace('\\', '/')}\", \"src/{item.Replace("\\" + item.Split('\\')[^1], string.Empty).Replace('\\', '/')}/\"]");
                }

                streamWriterDockerFile.WriteLine("");
                streamWriterDockerFile.WriteLine($"RUN dotnet restore \"src/{projetoPrincipalNome}/{projetoPrincipalNome}.csproj\"");
                streamWriterDockerFile.WriteLine("");

                streamWriterDockerFile.WriteLine("COPY . .");
                streamWriterDockerFile.WriteLine($"WORKDIR \"/src/{projetoPrincipalNome}\"");
                streamWriterDockerFile.WriteLine($"RUN dotnet build \"{projetoPrincipalNome}.csproj\" -c Release -o /app/build -r linux-x64");
                streamWriterDockerFile.WriteLine("");

                streamWriterDockerFile.WriteLine("FROM build AS publish");
                streamWriterDockerFile.WriteLine($"RUN dotnet publish \"{projetoPrincipalNome}.csproj\" -c Release -o /app/publish -r linux-x64");
                streamWriterDockerFile.WriteLine("");

                streamWriterDockerFile.WriteLine("FROM base AS final");
                streamWriterDockerFile.WriteLine("WORKDIR /app");
                streamWriterDockerFile.WriteLine("COPY --from=publish /app/publish .");
                streamWriterDockerFile.WriteLine($"ENTRYPOINT [\"dotnet\", \"{projetoPrincipalNome}.dll\"]");

                streamWriterDockerFile.Close();
                streamWriterDockerFile.Dispose();

                #endregion

                #region docker-compose

                var streamWriterDockerCompose = new StreamWriter(dockeFilePath + "\\docker-compose.yml", false);

                streamWriterDockerCompose.WriteLine("version: '3.4'");

                streamWriterDockerCompose.WriteLine("");

                streamWriterDockerCompose.WriteLine("services:");
                streamWriterDockerCompose.WriteLine($"    {projetoPrincipalNome.ToLower()}:");
                streamWriterDockerCompose.WriteLine("        image: ${DOCKER_REGISTRY-}" + projetoPrincipalNome.ToLower().Replace(".", string.Empty));
                streamWriterDockerCompose.WriteLine("        restart: always");
                streamWriterDockerCompose.WriteLine("        ports:");
                streamWriterDockerCompose.WriteLine("            - 5031:80");
                streamWriterDockerCompose.WriteLine("        build:");
                streamWriterDockerCompose.WriteLine("            context: .");
                streamWriterDockerCompose.WriteLine("            dockerfile: Dockerfile");

                streamWriterDockerCompose.Close();
                streamWriterDockerCompose.Dispose();

                #endregion

                #region docker ignore

                var streamWriterDockerIgnore = new StreamWriter(dockeFilePath + "\\.dockerignore.yml", false);

                foreach (var item in ListaIgnore)
                {
                    streamWriterDockerIgnore.WriteLine(item);
                }

                streamWriterDockerIgnore.Close();
                streamWriterDockerIgnore.Dispose();

                #endregion
            }
            catch (Exception ex)
            {
                var msgErro = $"Erro: '{ex.Message.ToUpper()}'.";

                if (ex.InnerException != null)
                    msgErro += $"\n\nDetalhes do erro: '{ex.InnerException.Message.ToUpper()}'.";

                Escrever(msgErro);
            }
            finally
            {
                Escrever("\nAperte qualquer tecla para sair.");
                Console.ReadKey();
            }
        }

        static void Escrever(string msg, bool ask = false)
        {
            if (ask)
                Console.Write(msg);
            else
                Console.WriteLine(msg);
        }

        private static string ObterString(string msg)
        {
            Escrever(msg, true);

            var retorno = Console.ReadLine();

            while (string.IsNullOrWhiteSpace(retorno))
            {
                Escrever(msg, true);
                retorno = Console.ReadLine();
            }

            return retorno;
        }

        private static IEnumerable<string> ListaIgnore => new string[]
        {
            "**/.classpath",
            "**/.dockerignore",
            "**/.env",
            "**/.git",
            "**/.gitignore",
            "**/.project",
            "**/.settings",
            "**/.toolstarget",
            "**/.vs",
            "**/.vscode",
            "**/*.*proj.user",
            "**/*.dbmdl",
            "**/*.jfm",
            "**/azds.yaml",
            "**/bin",
            "**/charts",
            "**/docker-compose*",
            "**/Dockerfile*",
            "**/node_modules",
            "**/npm-debug.log",
            "**/obj",
            "**/secrets.dev.yaml",
            "**/values.dev.yaml",
            "LICENSE",
            "README.md"
        };
    }
}
