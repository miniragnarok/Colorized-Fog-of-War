using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ColorizedFogOfWar
{
    class Program
    {
        private static readonly string currentDirectory = Path.Combine(Directory.GetCurrentDirectory(), @"..\..\");
        private static readonly string colorizedHistoricMomentsXlp = "ColorizedHistoricMoments.xlp";
        private static readonly string finishedImagesFolder = Path.Combine(currentDirectory, "Textures");
        private static readonly string colorizedHistoricMomentsModInfo = "ColorizedHistoricMoments.modinfo";
        private static readonly string textureTemplateFileName = "TextureTemplate.tex";
        private static readonly string imageFileNamePrepend = "CHM_";
        private static readonly string imageFileExtension = ".dds";
        private static readonly string textureFileExtension = ".tex";

        static void Main()
        {
            RenameImageAndTextureFiles();
            CreateTextureFiles();
            TransformXlp();
            TransformModInfo();
        }

        private static void TransformXlp()
        {
            string xlpFilepath = Path.Combine(currentDirectory, "XLPs", colorizedHistoricMomentsXlp);

            XElement xlp = XElement.Load(xlpFilepath);
            XElement entries = xlp.Descendants("m_Entries").SingleOrDefault();
            entries.Elements().Remove();

            List<string> imageNames = GetFinishedImageNames();
            foreach (string image in imageNames)
            {
                XElement newElement = new XElement("Element");

                newElement.Add(new XElement(
                    "m_EntryID",
                    new XAttribute("text", image)
                ));

                newElement.Add(new XElement(
                    "m_ObjectName",
                    new XAttribute("text", image)
                ));

                entries.Add(newElement);
            }

            xlp.Save(xlpFilepath);
        }

        private static List<string> GetFinishedImageNames()
        {
            List<string> imageNames = new List<string>();
            foreach (string file in Directory.EnumerateFiles(finishedImagesFolder, "*" + imageFileExtension))
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                if (!string.IsNullOrWhiteSpace(fileName))
                {
                    imageNames.Add(fileName);
                }
            }

            return imageNames;
        }

        private static void TransformModInfo()
        {
            string xlpFilepath = Path.Combine(currentDirectory, colorizedHistoricMomentsModInfo);

            XElement xlp = XElement.Load(xlpFilepath);
            XElement entries = xlp.Descendants("Files").SingleOrDefault();
            entries.Elements()
                .Where(m => !m.Value.EndsWith(".dep")
                    && !m.Value.EndsWith(".blp")
                ).Remove();

            char pathDelimiter = '/';
            List<string> fileDirectories = new List<string>
            {
                "ArtDefs",
                "SQL",
                "Textures",
                "XLPs"
            };

            foreach (string directoryName in fileDirectories)
            {
                foreach (string file in Directory.EnumerateFiles(Path.Combine(currentDirectory, directoryName)))
                {
                    XElement fileElement = new XElement("File");
                    fileElement.Add(directoryName + pathDelimiter + Path.GetFileName(file));
                    entries.Add(fileElement);
                }
            }

            xlp.Save(xlpFilepath);
        }

        private static void RenameImageAndTextureFiles()
        {
            foreach (string file in Directory.EnumerateFiles(finishedImagesFolder, "*" + imageFileExtension))
            {
                string currentFileName = Path.GetFileNameWithoutExtension(file);
                if (!currentFileName.StartsWith(imageFileNamePrepend))
                {
                    string newFileName = currentFileName;
                    newFileName = newFileName.Trim().Replace(' ', '_');

                    Regex regex = new Regex("[^a-zA-Z0-9_]");
                    newFileName = regex.Replace(newFileName, string.Empty);

                    newFileName = imageFileNamePrepend + newFileName;
                    File.Move(Path.Combine(finishedImagesFolder, currentFileName + imageFileExtension), Path.Combine(finishedImagesFolder, newFileName + imageFileExtension));

                    if (File.Exists(Path.Combine(finishedImagesFolder, currentFileName + textureFileExtension)))
                    {
                        File.Move(Path.Combine(finishedImagesFolder, currentFileName + textureFileExtension), Path.Combine(finishedImagesFolder, newFileName + textureFileExtension));
                    }
                }
            }
        }

        private static void CreateTextureFiles()
        {
            foreach (string file in Directory.EnumerateFiles(finishedImagesFolder, "*" + imageFileExtension))
            {
                string textureFileName = Path.GetFileNameWithoutExtension(file) + textureFileExtension;
                if (!File.Exists(Path.Combine(finishedImagesFolder, textureFileName)))
                {
                    string templateFilePath = Path.Combine(currentDirectory, textureTemplateFileName);
                    XElement template = XElement.Load(templateFilePath);

                    XAttribute sourceFilePathTextAttribute = template.Descendants("m_SourceFilePath")
                        .SingleOrDefault()
                        .Attribute("text");
                    sourceFilePathTextAttribute.Value = sourceFilePathTextAttribute.Value + Path.GetFileName(file);

                    XAttribute dataFilesRelativePathTextAttribute = template.Descendants("m_DataFiles")
                        .SingleOrDefault()
                        .Descendants("m_RelativePath")
                        .SingleOrDefault()
                        .Attribute("text");
                    dataFilesRelativePathTextAttribute.Value = Path.GetFileName(file);

                    XAttribute nameTextAttribute = template.Descendants("m_Name")
                        .SingleOrDefault()
                        .Attribute("text");
                    nameTextAttribute.Value = Path.GetFileNameWithoutExtension(file);

                    template.Save(Path.Combine(finishedImagesFolder, textureFileName));
                }
            }
        }
    }
}
