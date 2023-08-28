using Elements;
using Elements.Components;
using Elements.Geometry;
using LayoutFunctionCommon;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace SpaceType
{
  public static class SpaceType
  {
    /// <summary>
    /// The SpaceType function.
    /// </summary>
    /// <param name="model">The input model.</param>
    /// <param name="input">The arguments to the execution.</param>
    /// <returns>A SpaceTypeOutputs instance containing computed results and the model with any new elements.</returns>
    public static SpaceTypeOutputs Execute(Dictionary<string, Model> inputModels, SpaceTypeInputs input)
    {
      Elements.Serialization.glTF.GltfExtensions.UseReferencedContentExtension = true;
      var output = new SpaceTypeOutputs();
      var spaceTypeCatalogFolder = input.SpaceType;
      if (spaceTypeCatalogFolder == null)
      {
        output.Errors.Add("No space type specified.");
        return output;
      }
      if (String.IsNullOrEmpty(input.ProgramName))
      {
        output.Errors.Add("No program name specified.");
        return output;
      }
      Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(input.SpaceType));

      // Build up a dictionary of FileRefIds and FileNames.
      var fileRefDict = new Dictionary<string, string>();
      foreach (var file in input.SpaceType.Files)
      {
        fileRefDict.Add(file.FileName, $"hypar://folders/{file.FolderId}/files/{file.FileRefId}");
      }

      // var catalogFile = spaceTypeCatalogFolder.Files.FirstOrDefault(f => f.FileName.EndsWith("catalog.json"));
      // the catalog file is the first one which contains a GUID in the name. Use regex
      // to find it.
      var catalogFile = spaceTypeCatalogFolder.Files.FirstOrDefault(f => System.Text.RegularExpressions.Regex.IsMatch(f.FileName, @".*[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}\.json"));

      if (catalogFile == null || !File.Exists(catalogFile.LocalFilePath))
      {
        output.Errors.Add("No content catalog found for the specified space type.");
        return output;
      }
      var configFile = spaceTypeCatalogFolder.Files.FirstOrDefault(f => f.FileName.EndsWith("space-config.json"));
      if (configFile == null || !File.Exists(configFile.LocalFilePath))
      {
        output.Errors.Add("No Space Type configuration found for the specified space type.");
        return output;
      }
      var catalogPath = catalogFile.LocalFilePath;
      var configPath = configFile.LocalFilePath;
      Console.WriteLine(File.ReadAllText(catalogPath));
      Console.WriteLine(File.ReadAllText(configPath));
      var catalog = Model.FromJson(File.ReadAllText(catalogPath));
      if (catalog == null)
      {
        output.Errors.Add("Space type catalog was invalid.");
        return output;
      }
      var contentElements = catalog.AllElementsOfType<ContentElement>();
      var contentConfiguration = JsonConvert.DeserializeObject<SpaceConfiguration>(File.ReadAllText(configPath));
      if (contentConfiguration == null)
      {
        output.Errors.Add("Space type configuration was invalid.");
        return output;
      }
      foreach (var config in contentConfiguration!.Values)
      {
        foreach (var ci in config.ContentItems)
        {
          ci.Url = fileRefDict[ci.Url];
        }
      }
      // get a temp path for updated config 
      var tempConfigPath = Path.GetTempFileName();
      File.WriteAllText(tempConfigPath, JsonConvert.SerializeObject(contentConfiguration));
      Console.WriteLine(File.ReadAllText(tempConfigPath));
      LayoutStrategies.StandardLayoutOnAllLevels<LevelElements, LevelVolume, SpaceBoundary, CirculationSegment>(
          input.ProgramName,
          inputModels,
          input.Overrides,
          output.Model,
          true,
          tempConfigPath, catalogPath);
      return output;
    }
  }
}