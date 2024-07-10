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

    // Over time, we should remove these "exceptions" and make the standard
    // layout function capable of producing their layouts. We may also need a
    // generic mechanism for a function to "take over" layout for a given type
    // and say "Don't try to lay out a space type here!" (Maybe not specifying
    // "Layout Type" is sufficient.)
    private static readonly HashSet<string> ReservedSpaceTypes = new() {
       "Classroom",
       "Data Hall",
       "Lounge",
       "Meeting Room",
       "Open Collaboration",
       "Open Office",
       "Pantry",
       "Phone Booth",
       "Private Office",
       "Reception",
       "Unassigned Space Type",
       "unspecified"
      };

    private static readonly Dictionary<string, string> ReservedSpaceTypeFolderIds = new() {
        { "4c8701eb-c83f-4a12-841d-c1fb0a9d30cd", "Classroom" },
        { "c3fc7d7a-88c3-4f01-b481-21be8a5366d4", "Data Hall" },
        { "626f5100-d564-497d-877a-327c6a4a8d48", "Lounge" },
        { "e4d81fb8-7d30-4cf1-9622-211abfdc2c27", "Meeting Room" },
        { "3e91b498-57c1-4a8d-94dd-edaf954d1f2f", "Open Collaboration" },
        { "65640703-dfbf-485f-a291-8f1e3193c28c", "Open Office" },
        { "227f74ef-bc3d-4033-bd6f-81ff75a7577b", "Pantry" },
        { "4be92770-9e38-471a-b48f-d8ab09e120a9", "Phone Booth" },
        { "572692e8-fd80-4c7b-b9e8-2445acdbdf0d", "Private Office" },
        { "47bc8ed1-b92b-443e-9131-7a0fd0151649", "Reception" }
    };

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

      if (!inputModels.TryGetValue("Program Requirements", out var programReqModel))
      {
        output.Errors.Add("Missing input model: Program Requirements");
        return output;
      }

      var programReqs = programReqModel.AllElementsOfType<ProgramRequirement>();
      var programReqsBySpaceType = programReqs.GroupBy(pr =>
      {
        var name = pr.HyparSpaceType ?? pr.QualifiedProgramName;
        if (name == "unspecified")
        {
          name = pr.QualifiedProgramName;
        }
        return name;
      }, pr => pr);
      var alreadyHandled = new HashSet<string>();
      var handledSpaces = new HashSet<Guid>();
      foreach (var group in programReqsBySpaceType)
      {
        var programName = group.Key;
        var programReq = group.First();
        if (ReservedSpaceTypes.Contains(programName))
        {
          // Classic built-in space types, on explore (or old pringle)
          continue;
        }
        else if (ReservedSpaceTypeFolderIds.ContainsKey(programName))
        {
          // Classic built-in space types on new pringle 
          continue;
        }

        var (catalogPath, configPath) = programReq.WriteLayoutConfigs(programReqModel);
        if (catalogPath == null || configPath == null)
        {
          Console.WriteLine($"No Space information for {programName}.");
          continue;
        }

        Console.WriteLine($"instantiating {programName}");
        SpaceConfiguration configs = ContentManagement.GetSpaceConfiguration<ProgramRequirement>(inputModels, configPath, programName);
        var ids = LayoutStrategies.StandardLayoutOnAllLevels<LevelElements, LevelVolume, SpaceBoundary, CirculationSegment>(
            programName,
            inputModels,
            input.Overrides,
            output.Model,
            true,
            configs);
        handledSpaces.UnionWith(ids);
        alreadyHandled.Add(programReq.QualifiedProgramName);
      }
      var allSpaceBoundaries = inputModels["Space Planning Zones"]
        .AllElementsAssignableFromType<SpaceBoundary>()
        .Where(sb => !handledSpaces.Contains(sb.Id) && !ReservedSpaceTypes.Contains(sb.HyparSpaceType ?? sb.Name));
      // For any spaces which were not "required," we still want to respect their wall requirements.
      LayoutStrategies.GenerateWallsForAllSpaces<LevelElements, LevelVolume, SpaceBoundary, CirculationSegment>(allSpaceBoundaries, inputModels, output.Model);

      return output;
    }
  }
}